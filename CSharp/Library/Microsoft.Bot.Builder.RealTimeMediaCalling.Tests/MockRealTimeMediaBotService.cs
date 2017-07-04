using System.Net.Http;
using System.Threading.Tasks;
using Autofac;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling.Tests
{
    internal class MockRealTimeMediaBotService : RealTimeMediaBotService
    {
        public MockRealTimeMediaBotService(ILifetimeScope scope, IRealTimeMediaCallServiceSettings settings)
            : base(scope, settings)
        {
        }

        protected override Task PlaceCall(HttpContent content, string correlationId)
        {
            return Task.CompletedTask;
        }
    }

}
