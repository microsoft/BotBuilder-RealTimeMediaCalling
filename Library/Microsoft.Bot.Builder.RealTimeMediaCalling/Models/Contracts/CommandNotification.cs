using Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling.Models.Contracts
{
    /// <summary>
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class CommandNotification : NotificationBase
    {
        /// <summary>
        /// 
        /// </summary>
        public CommandNotification()
        {
            this.Type = NotificationType.CommandReceived;
        }
    }
}
