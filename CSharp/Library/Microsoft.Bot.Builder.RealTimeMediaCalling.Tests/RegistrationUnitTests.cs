using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.RealTimeMediaCalling.Events;
using Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts;
using Microsoft.Skype.Bots.Media;
using Moq;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling.Tests
{
    [TestFixture]
    public class RegistrationUnitTests
    {
        private class RealTimeMediaBot : IRealTimeMediaBot
        {
            public IRealTimeMediaBotService RealTimeMediaBotService { get; }

            public RealTimeMediaBot(IRealTimeMediaBotService service)
            {
                RealTimeMediaBotService = service;
            }
        }

        private class RealTimeMediaCall : IRealTimeMediaCall
        {
            public IRealTimeMediaCallService CallService { get; }

            /// <summary>
            /// CorrelationId that needs to be set in the media platform for correlating logs across services
            /// </summary>
            public string CorrelationId { get; }

            /// <summary>
            /// Id generated locally that is unique to each RealTimeMediaCall
            /// </summary>
            public string CallId { get; }

            public RealTimeMediaCall(IRealTimeMediaCallService service)
            {
                CallService = service;
                CallService.OnIncomingCallReceived += OnIncomingCallReceived;
                CallService.OnJoinCallRequested += OnJoinCallRequested;
                CallService.OnCallCleanup += OnCallCleanup;

                CorrelationId = service.CorrelationId;
                CallId = $"{service.CorrelationId}:{Guid.NewGuid()}";
            }

            private Task OnIncomingCallReceived(RealTimeMediaIncomingCallEvent realTimeMediaIncomingCallEvent)
            {
                var mediaSession = CallService.CreateMediaSession(NotificationType.CallStateChange);
                var socketSettings = new AudioSocketSettings
                {
                    StreamDirections = StreamDirection.Sendrecv,
                    SupportedAudioFormat = AudioFormat.Pcm16K
                };
                var audioSocket = mediaSession.SetAudioSocket(socketSettings);
                audioSocket.AudioMediaReceived += AudioSocket_AudioMediaReceived;
                realTimeMediaIncomingCallEvent.Answer(mediaSession);
                return Task.CompletedTask;
            }

            private Task OnJoinCallRequested(RealTimeMediaJoinCallEvent realTimeMediaJoinCallEvent)
            {
                var mediaSession = CallService.CreateMediaSession(NotificationType.CallStateChange);
                var audioSocket = mediaSession.SetAudioSocket(new AudioSocketSettings());
                audioSocket.AudioMediaReceived += AudioSocket_AudioMediaReceived;
                realTimeMediaJoinCallEvent.JoinCall(mediaSession);
                return Task.CompletedTask;
            }

            private void AudioSocket_AudioMediaReceived(object sender, AudioMediaReceivedEventArgs e)
            {
                throw new NotImplementedException();
            }

            private Task OnCallCleanup()
            {
                return Task.CompletedTask;
            }
        }

        [Test]
        public async Task CreatingBotWithIncomingCall()
        {
            var settings = new Mock<IRealTimeMediaCallServiceSettings>();
            settings.Setup(a => a.CallbackUrl).Returns(new Uri("https://someuri/callback"));
            settings.Setup(a => a.NotificationUrl).Returns(new Uri("https://someuri/notification"));

            RealTimeMediaCalling.RegisterRealTimeMediaCallingBot<MockRealTimeMediaBotService, MockRealTimeMediaCallService>(
                settings.Object,
                a => new RealTimeMediaBot(a),
                a => new RealTimeMediaCall(a));
            var bot = RealTimeMediaCalling.GetBot();

            Assert.NotNull(bot);
            Assert.NotNull(bot.RealTimeMediaBotService);
            Assert.AreSame(typeof(RealTimeMediaBot), bot.GetType());

            var incomingCallJson = @"
{
  ""id"": ""0b022b87-f255-4667-9335-2335f30ee8de"",
  ""participants"": [
    {
      ""identity"": ""29:1kMGSkuCPgD7ReaC5V2XN08CMOjOcs9MngtbzvvJ8sNU"",
      ""languageId"": ""en-US"",
      ""originator"": true
    },
    {
      ""identity"": ""28:c89e6f90-2b47-4eee-8e3b-22d0b3a6d495"",
      ""originator"": false
    }
  ],
  ""isMultiparty"": false,
  ""presentedModalityTypes"": [
    ""audio""
  ],
  ""callState"": ""incoming""
}";

            var service = bot.RealTimeMediaBotService as IInternalRealTimeMediaBotService;
            var result = await service.ProcessIncomingCallAsync(incomingCallJson, null);
            Assert.AreEqual(ResponseType.Accepted, result.ResponseType);
            Assert.AreEqual(1, service.Calls.Count);
            Assert.NotNull(service.GetCallForId("0b022b87-f255-4667-9335-2335f30ee8de"));
            Assert.Null(service.GetCallForId("0b022b88-f255-4667-9335-2335f30ee8de"));

            var call1 = service.GetCallForId("0b022b87-f255-4667-9335-2335f30ee8de") as RealTimeMediaCall;
            Assert.NotNull(call1);
            Assert.IsNotEmpty(call1.CorrelationId);
            Assert.IsNotEmpty(call1.CallId);
            Assert.AreEqual(call1.CorrelationId, call1.CallService.CorrelationId);
            Assert.IsTrue(call1.CallId.StartsWith(call1.CorrelationId));

            result = await service.ProcessIncomingCallAsync(incomingCallJson, Guid.Empty.ToString());
            Assert.AreEqual(ResponseType.Accepted, result.ResponseType);
            Assert.AreEqual(1, service.Calls.Count);
            Assert.NotNull(service.GetCallForId("0b022b87-f255-4667-9335-2335f30ee8de"));
            Assert.Null(service.GetCallForId("0b022b88-f255-4667-9335-2335f30ee8de"));

            var call2 = service.GetCallForId("0b022b87-f255-4667-9335-2335f30ee8de") as RealTimeMediaCall;
            Assert.NotNull(call2);
            Assert.IsNotEmpty(call2.CorrelationId);
            Assert.IsNotEmpty(call2.CallId);
            Assert.AreEqual(call2.CorrelationId, call2.CallService.CorrelationId);
            Assert.IsTrue(call2.CallId.StartsWith(call2.CorrelationId));
            Assert.AreNotEqual(call1, call2);

            incomingCallJson = incomingCallJson.Replace("0b022b87", "0b022b88");

            result = await service.ProcessIncomingCallAsync(incomingCallJson, null);
            Assert.AreEqual(ResponseType.Accepted, result.ResponseType);
            Assert.AreEqual(2, service.Calls.Count);
            Assert.NotNull(service.GetCallForId("0b022b87-f255-4667-9335-2335f30ee8de"));
            Assert.NotNull(service.GetCallForId("0b022b88-f255-4667-9335-2335f30ee8de"));

            var call3 = service.GetCallForId("0b022b88-f255-4667-9335-2335f30ee8de") as RealTimeMediaCall;
            Assert.NotNull(call3);
            Assert.IsNotEmpty(call3.CorrelationId);
            Assert.IsNotEmpty(call3.CallId);
            Assert.AreEqual(call3.CorrelationId, call3.CallService.CorrelationId);
            Assert.IsTrue(call3.CallId.StartsWith(call3.CorrelationId));
            Assert.AreNotEqual(call1, call3);
            Assert.AreNotEqual(call2, call3);

            var acceptCallbackJson = @"   
{
  ""id"": ""0b022b88-f255-4667-9335-2335f30ee8de"",
  ""operationOutcome"": {
    ""type"": ""answerAppHostedMediaOutcome"",
    ""id"": ""1a1a29f1-4102-4b6c-9b85-bf20d61c1756"",
    ""outcome"": ""success""
  },
  ""callState"": ""established"",
  ""appState"": ""ddc2d769-30e2-4418-8de5-5d846139add8"",
  ""links"": {
    ""call"": ""https://b-pma-uswe-01.plat.skype.com:6702/platform/v1/calls/faff9af3-5fef-443b-bc2c-7027d00e7cc9"",
    ""subscriptions"": ""https://b-pma-uswe-01.plat.skype.com:6702/platform/v1/calls/faff9af3-5fef-443b-bc2c-7027d00e7cc9/subscriptions"",
    ""mixer"": ""https://b-pma-uswe-01.plat.skype.com:6702/platform/v1/calls/faff9af3-5fef-443b-bc2c-7027d00e7cc9/mixer"",
    ""participantLegMetadata"": ""https://b-pma-uswe-01.plat.skype.com:6702/platform/v1/calls/faff9af3-5fef-443b-bc2c-7027d00e7cc9/participantlegmetadata""
  }
}";

            result = await service.ProcessCallbackAsync(acceptCallbackJson);
            Assert.AreEqual(ResponseType.Accepted, result.ResponseType);

            var notificationJson = @"
{
  ""id"": ""0b022b87-f255-4667-9335-2335f30ee8de"",
  ""currentState"": ""terminated"",
  ""stateChangeCode"": ""0"",
  ""type"": ""callStateChange"",
}";

            result = await service.ProcessNotificationAsync(notificationJson).ConfigureAwait(false);
            var call4 = service.GetCallForId("0b022b87-f255-4667-9335-2335f30ee8de");
            Assert.AreEqual(ResponseType.Accepted, result.ResponseType);
            Assert.Null(call4);
        }

        [Test]
        public async Task CreatingBotWithJoinCall()
        {
            var settings = new Mock<IRealTimeMediaCallServiceSettings>();
            settings.Setup(a => a.CallbackUrl).Returns(new Uri("https://someuri/callback"));
            settings.Setup(a => a.NotificationUrl).Returns(new Uri("https://someuri/notification"));

            RealTimeMediaCalling.RegisterRealTimeMediaCallingBot<MockRealTimeMediaBotService, MockRealTimeMediaCallService>(
                settings.Object,
                a => new RealTimeMediaBot(a),
                a => new RealTimeMediaCall(a));
            var bot = RealTimeMediaCalling.GetBot();

            Assert.NotNull(bot);
            Assert.NotNull(bot.RealTimeMediaBotService);
            Assert.AreSame(typeof(RealTimeMediaBot), bot.GetType());

            var service = bot.RealTimeMediaBotService as IInternalRealTimeMediaBotService;
            var joinCall1 = new JoinCallParameters("ABC", "123", "456");
            var joinCall2 = new JoinCallParameters("ABC", "123", "456");

            await service.JoinCall(joinCall1, null);
            Assert.AreEqual(1, service.Calls.Count);
            Assert.NotNull(service.GetCallForId(joinCall1.ConversationId));
            Assert.Null(service.GetCallForId(joinCall2.ConversationId));

            var call1 = service.GetCallForId(joinCall1.ConversationId) as RealTimeMediaCall;
            Assert.NotNull(call1);
            Assert.IsNotEmpty(call1.CorrelationId);
            Assert.IsNotEmpty(call1.CallId);
            Assert.AreEqual(call1.CorrelationId, call1.CallService.CorrelationId);
            Assert.IsTrue(call1.CallId.StartsWith(call1.CorrelationId));

            await service.JoinCall(joinCall1, null);
            Assert.AreEqual(1, service.Calls.Count);
            Assert.NotNull(service.GetCallForId(joinCall1.ConversationId));
            Assert.Null(service.GetCallForId(joinCall2.ConversationId));

            var call2 = service.GetCallForId(joinCall1.ConversationId) as RealTimeMediaCall;
            Assert.NotNull(call2);
            Assert.IsNotEmpty(call2.CorrelationId);
            Assert.IsNotEmpty(call2.CallId);
            Assert.AreEqual(call2.CorrelationId, call2.CallService.CorrelationId);
            Assert.IsTrue(call2.CallId.StartsWith(call2.CorrelationId));
            Assert.AreNotEqual(call1, call2);

            await service.JoinCall(joinCall2, null);
            Assert.AreEqual(2, service.Calls.Count);
            Assert.NotNull(service.GetCallForId(joinCall1.ConversationId));
            Assert.NotNull(service.GetCallForId(joinCall2.ConversationId));

            var call3 = service.GetCallForId(joinCall2.ConversationId) as RealTimeMediaCall;
            Assert.NotNull(call3);
            Assert.IsNotEmpty(call3.CorrelationId);
            Assert.IsNotEmpty(call3.CallId);
            Assert.AreEqual(call3.CorrelationId, call3.CallService.CorrelationId);
            Assert.IsTrue(call3.CallId.StartsWith(call3.CorrelationId));
            Assert.AreNotEqual(call1, call3);
            Assert.AreNotEqual(call2, call3);

            // Accept doesn't make sense here.
            // We're just testing to make sure that the callback is going to the right call.
            // TODO: replace with a different callback.
            var acceptCallbackJson = @"   
{
  ""id"": ""0b022b88-f255-4667-9335-2335f30ee8de"",
  ""operationOutcome"": {
    ""type"": ""answerAppHostedMediaOutcome"",
    ""id"": ""1a1a29f1-4102-4b6c-9b85-bf20d61c1756"",
    ""outcome"": ""success""
  },
  ""callState"": ""established"",
  ""appState"": ""ddc2d769-30e2-4418-8de5-5d846139add8"",
  ""links"": {
    ""call"": ""https://b-pma-uswe-01.plat.skype.com:6702/platform/v1/calls/faff9af3-5fef-443b-bc2c-7027d00e7cc9"",
    ""subscriptions"": ""https://b-pma-uswe-01.plat.skype.com:6702/platform/v1/calls/faff9af3-5fef-443b-bc2c-7027d00e7cc9/subscriptions"",
    ""mixer"": ""https://b-pma-uswe-01.plat.skype.com:6702/platform/v1/calls/faff9af3-5fef-443b-bc2c-7027d00e7cc9/mixer"",
    ""participantLegMetadata"": ""https://b-pma-uswe-01.plat.skype.com:6702/platform/v1/calls/faff9af3-5fef-443b-bc2c-7027d00e7cc9/participantlegmetadata""
  }
}";

            acceptCallbackJson = acceptCallbackJson.Replace("0b022b88-f255-4667-9335-2335f30ee8de", joinCall2.ConversationId);
            var result = await service.ProcessCallbackAsync(acceptCallbackJson);
            Assert.AreEqual(ResponseType.Accepted, result.ResponseType);

            var notificationJson = @"
{
  ""id"": ""0b022b87-f255-4667-9335-2335f30ee8de"",
  ""currentState"": ""terminated"",
  ""stateChangeCode"": ""0"",
  ""type"": ""callStateChange"",
}";

            notificationJson = notificationJson.Replace("0b022b87-f255-4667-9335-2335f30ee8de", joinCall2.ConversationId);
            result = await service.ProcessNotificationAsync(notificationJson).ConfigureAwait(false);
            var call4 = service.GetCallForId(joinCall2.ConversationId);
            Assert.AreEqual(ResponseType.Accepted, result.ResponseType);
            Assert.Null(call4);
        }
    }
}
