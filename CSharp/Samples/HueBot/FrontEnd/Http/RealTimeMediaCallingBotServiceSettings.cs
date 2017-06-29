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

        public RealTimeMediaCallingBotServiceSettings()
        {
            CallbackUrl = Service.Instance.Configuration.CallControlCallbackUrl;
            NotificationUrl = Service.Instance.Configuration.NotificationCallbackUrl;
        }
    }
}
