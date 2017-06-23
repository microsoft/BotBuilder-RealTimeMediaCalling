using Autofac;
using Microsoft.Bot.Builder.Calling.ObjectModel.Contracts;
using Microsoft.Bot.Builder.RealTimeMediaCalling.Events;
using Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

            public RealTimeMediaCall(IRealTimeMediaCallService service)
            {
                CallService = service;
                CallService.OnIncomingCallReceived += OnIncomingCallReceived;
                CallService.OnCallCleanup += OnCallCleanup;
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
            var iBot = RealTimeMediaCalling.Container.Resolve<IRealTimeMediaBot>();

            Assert.NotNull(iBot);
            Assert.NotNull(iBot.RealTimeMediaBotService);
            Assert.AreSame(typeof(RealTimeMediaBot), iBot.GetType());

            var requestJson = @"
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

            var service = iBot.RealTimeMediaBotService;
            var result = await service.ProcessIncomingCallAsync(requestJson, null);
            Assert.AreEqual(ResponseType.Accepted, result.ResponseType);
            Assert.AreEqual(1, service.Calls.Count);
            Assert.NotNull(service.GetCallForId("0b022b87-f255-4667-9335-2335f30ee8de"));
            Assert.Null(service.GetCallForId("0b022b88-f255-4667-9335-2335f30ee8de"));

            result = await service.ProcessIncomingCallAsync(requestJson, Guid.Empty.ToString());
            Assert.AreEqual(ResponseType.Accepted, result.ResponseType);
            Assert.AreEqual(1, service.Calls.Count);
            Assert.NotNull(service.GetCallForId("0b022b87-f255-4667-9335-2335f30ee8de"));
            Assert.Null(service.GetCallForId("0b022b88-f255-4667-9335-2335f30ee8de"));

            requestJson = requestJson.Replace("0b022b87", "0b022b88");

            result = await service.ProcessIncomingCallAsync(requestJson, null);
            Assert.AreEqual(ResponseType.Accepted, result.ResponseType);
            Assert.AreEqual(2, service.Calls.Count);
            Assert.NotNull(service.GetCallForId("0b022b87-f255-4667-9335-2335f30ee8de"));
            Assert.NotNull(service.GetCallForId("0b022b88-f255-4667-9335-2335f30ee8de"));

            // TODO: There is no cleanup task, as far as I can tell.
        }

    }
}
