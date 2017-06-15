/**************************************************
*                                                                                    *
*   © Microsoft Corporation. All rights reserved.  *
*                                                                                    *
**************************************************/

namespace FrontEnd.Http
{
    /// <summary>
    /// HTTP route constants for routing requests to CallController methods.
    /// </summary>
    public static class HttpRouteConstants
    {
        /// <summary>
        /// Route prefix for all incoming requests.
        /// </summary>
        public const string CallSignalingRoutePrefix = "api/calling";

        /// <summary>
        /// Route prefix for incoming requests
        /// </summary>
        public const string OnIncomingMessageRoute = "api/messages";

        /// <summary>
        /// Route for incoming calls.
        /// </summary>
        public const string OnIncomingCallRoute = "call";

        /// <summary>
        /// Route for existing call callbacks.
        /// </summary>
        public const string OnCallbackRoute = "callback";

        /// <summary>
        /// Route for existing call notifications.
        /// </summary>
        public const string OnNotificationRoute = "notification";
    }
}
