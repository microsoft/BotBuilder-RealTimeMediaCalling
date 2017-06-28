/**************************************************
*                                                                                    *
*   © Microsoft Corporation. All rights reserved.  *
*                                                                                    *
**************************************************/

using System;
using Microsoft.Bot.Builder.RealTimeMediaCalling;

namespace FrontEnd.Http
{
    /// <summary>
    /// Service settings to configure the RealTimeMediaCalling
    /// </summary>
    public class RealTimeMediaCallingBotServiceSettings : IRealTimeMediaCallServiceSettings
    {
        /// <summary>
        /// The url where the callbacks for the calls to this bot needs to be sent. 
        /// For example "https://testservice.azurewebsites.net/api/calling/callback"   
        /// </summary>
        public Uri CallbackUrl { get; private set; }

        /// <summary>
        /// The url where the notifications for the calls to this bot needs to be sent. 
        /// For example "https://testservice.azurewebsites.net/api/calling/notification"   
        /// </summary>
        public Uri NotificationUrl { get; private set; }

        /// <summary>
        /// Url that the bot uses to make an outbound call request, used for both outgoing join or place call
        /// For example "https://pma-dev-uswe-01.plat-dev.skype.net:6448/platform/v1/calls" 
        /// </summary>
        public Uri PlaceCallEndpointUrl { get; private set; }

        /// <summary>
        /// BotId used to authenticate outgoing call requests to PMA
        /// </summary>
        public string BotId { get; }

        /// <summary>
        /// BotSecret used to authenticate outgoing call requests to PMA
        /// </summary>
        public string BotSecret { get; }

        public RealTimeMediaCallingBotServiceSettings()
        {
            CallbackUrl = Service.Instance.Configuration.CallControlCallbackUrl;
            NotificationUrl = Service.Instance.Configuration.NotificationCallbackUrl;
            PlaceCallEndpointUrl = Service.Instance.Configuration.PlaceCallEndpointUrl;
        }
    }
}
