/**************************************************
*                                                                                    *
*   © Microsoft Corporation. All rights reserved.  *
*                                                                                    *
**************************************************/

using System;
using System.Collections.Generic;
using Microsoft.Skype.Bots.Media;

namespace FrontEnd
{
    /// <summary>
    /// IConfiguration contains the static configuration information the application needs
    /// to run such as the urls it needs to listen on, credentials to communicate with
    /// Bing translator, settings for media.platform, etc.
    /// 
    /// The concrete implemenation AzureConfiguration gets the configuration from Azure.  However,
    /// other concrete classes could be created to allow the application to run outside of Azure
    /// for testing.
    /// </summary>
    public interface IConfiguration : IDisposable
    {
        /// <summary>
        /// Dns name for this service
        /// </summary>
        string ServiceDnsName { get; }

        /// <summary>
        /// List of HTTP urls the app should listen on for incoming call
        /// signaling requests from Skype Platform.
        /// </summary>
        IEnumerable<Uri> CallControlListeningUrls { get; }

        /// <summary>
        /// The base callback URL for this instance.  To ensure that all requests
        /// for a given call go to the same instance, this Url is unique to each
        /// instance by way of its instance input endpoint port.
        /// </summary>
        Uri CallControlCallbackUrl { get; }

        /// <summary>
        /// The template for call notifications like call state change notifications.
        /// To ensure that all requests for a given call go to the same instance, this Url 
        /// is unique to each instance by way of its instance input endpoint port.
        /// </summary>
        Uri NotificationCallbackUrl { get; }

        /// <summary>
        /// The remote endpoint that any outgoing call targets
        /// </summary>
        Uri PlaceCallEndpointUrl { get; }

        /// <summary>
        /// Base url for this particular azure instance
        /// </summary>
        Uri AzureInstanceBaseUrl { get; }

        /// <summary>
        /// MicrosoftAppId generated at the time of registration of the bot
        /// </summary>
        string MicrosoftAppId { get; }

        /// <summary>
        /// Settings for the bot media platform
        /// </summary>
        MediaPlatformSettings MediaPlatformSettings { get; }
    }
}
