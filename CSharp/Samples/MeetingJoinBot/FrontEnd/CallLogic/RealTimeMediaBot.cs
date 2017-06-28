using Microsoft.Bot.Builder.RealTimeMediaCalling;

namespace FrontEnd.CallLogic
{
    public class RealTimeMediaBot : IRealTimeMediaBot
    {
        public IRealTimeMediaBotService RealTimeMediaBotService { get; }

        public RealTimeMediaBot(IRealTimeMediaBotService service)
        {
            RealTimeMediaBotService = service;
        }
    }
}
