using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Bot.Builder.Calling.ObjectModel.Contracts;
using Microsoft.Bot.Builder.Calling.ObjectModel.Misc;
using Microsoft.Bot.Builder.RealTimeMediaCalling.Events;
using Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts;
using Moq;
using Newtonsoft.Json.Linq;
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
                CallService.OnAnswerAppHostedMediaCompleted += OnAnswerAppHostedMediaCompleted;
                CallService.OnCallCleanup += OnCallCleanup;

                CorrelationId = service.CorrelationId;
                CallId = $"{service.CorrelationId}:{Guid.NewGuid()}";
            }

            private Task OnIncomingCallReceived(RealTimeMediaIncomingCallEvent realTimeMediaIncomingCallEvent)
            {
                JObject mediaConfiguration;
                using (var writer = new JTokenWriter())
                {
                    writer.WriteRaw("MediaConfiguration");
                    mediaConfiguration = new JObject { { "Token", writer.Token } };
                }

                realTimeMediaIncomingCallEvent.RealTimeMediaWorkflow.Actions = new ActionBase[]
                {
                    new AnswerAppHostedMedia
                    {
                        MediaConfiguration = mediaConfiguration,
                        OperationId = Guid.NewGuid().ToString()
                    }
                };

                realTimeMediaIncomingCallEvent.RealTimeMediaWorkflow.NotificationSubscriptions = new[] { NotificationType.CallStateChange };

                return Task.CompletedTask;
            }

            private Task OnAnswerAppHostedMediaCompleted(AnswerAppHostedMediaOutcomeEvent answerAppHostedMediaOutcomeEvent)
            {
                AnswerAppHostedMediaOutcome answerAppHostedMediaOutcome = answerAppHostedMediaOutcomeEvent.AnswerAppHostedMediaOutcome;
                if (answerAppHostedMediaOutcome.Outcome != Outcome.Success)
                {
                    throw new InvalidOperationException("Answer app hosted media failed.");
                }
                answerAppHostedMediaOutcomeEvent.RealTimeMediaWorkflow.NotificationSubscriptions = new[] { NotificationType.CallStateChange, NotificationType.RosterUpdate };
                return Task.CompletedTask;
            }

            private Task OnCallCleanup()
            {
                return Task.CompletedTask;
            }
        }

        [Test]
        public async Task CreatingBotWithIRealTimeMediaServices()
        {
            var settings = new Mock<IRealTimeMediaCallServiceSettings>();
            settings.Setup(a => a.CallbackUrl).Returns(new Uri("https://someuri/callback"));
            settings.Setup(a => a.NotificationUrl).Returns(new Uri("https://someuri/notification"));

            RealTimeMediaCalling.RegisterRealTimeMediaCallingBot(
                settings.Object,
                a => new RealTimeMediaBot(a),
                a => new RealTimeMediaCall(a));
            var bot = RealTimeMediaCalling.Container.Resolve<IRealTimeMediaBot>();

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

            var call = service.Calls.First() as RealTimeMediaCall;
            Assert.NotNull(call);
            Assert.IsNotEmpty(call.CorrelationId);
            Assert.IsNotEmpty(call.CallId);
            Assert.AreEqual(call.CorrelationId, call.CallService.CorrelationId);
            Assert.IsTrue(call.CallId.StartsWith(call.CorrelationId));

            result = await service.ProcessIncomingCallAsync(incomingCallJson, Guid.Empty.ToString());
            Assert.AreEqual(ResponseType.Accepted, result.ResponseType);
            Assert.AreEqual(1, service.Calls.Count);
            Assert.NotNull(service.GetCallForId("0b022b87-f255-4667-9335-2335f30ee8de"));
            Assert.Null(service.GetCallForId("0b022b88-f255-4667-9335-2335f30ee8de"));

            incomingCallJson = incomingCallJson.Replace("0b022b87", "0b022b88");

            result = await service.ProcessIncomingCallAsync(incomingCallJson, null);
            Assert.AreEqual(ResponseType.Accepted, result.ResponseType);
            Assert.AreEqual(2, service.Calls.Count);
            Assert.NotNull(service.GetCallForId("0b022b87-f255-4667-9335-2335f30ee8de"));
            Assert.NotNull(service.GetCallForId("0b022b88-f255-4667-9335-2335f30ee8de"));

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

            result = await service.ProcessCallbackAsync(acceptCallbackJson, null);
            Assert.AreEqual(ResponseType.Accepted, result.ResponseType);

            // TODO: There is no cleanup task, as far as I can tell.
        }
    }
}
