/**************************************************
*                                                                                    *
*   © Microsoft Corporation. All rights reserved.  *
*                                                                                    *
**************************************************/

using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using FrontEnd.Call;
using FrontEnd.Logging;
using Microsoft.Bot.Builder.RealTimeMediaCalling;
using Microsoft.Bot.Connector;

namespace FrontEnd.Http
{
    /// <summary>
    /// CallContoller is the enty point for handling incoming call signaling HTTP requests from Skype platform.
    /// </summary>    
    [RoutePrefix(HttpRouteConstants.CallSignalingRoutePrefix)]
    public class CallController : ApiController
    {        
        /// <summary>
        /// Instantiate a CallController with a specific ICallProcessor (e.g. for testing).
        /// </summary>
        /// <param name="callProcessor"></param>
        static CallController()
        {            
            RealTimeMediaCalling.RegisterRealTimeMediaCallingBot(c => { return new RealTimeMediaCall(c); },
                                                                 new RealTimeMediaCallingBotServiceSettings());
        }
        
        /// <summary>
        /// Handle an incoming call.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route(HttpRouteConstants.OnIncomingCallRoute)]
        [BotAuthentication]
        public async Task<HttpResponseMessage> OnIncomingCall(HttpRequestMessage request)
        {
            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"Received HTTP {request.Method}, {request.RequestUri}"); 

            var response = await RealTimeMediaCalling.SendAsync(request, RealTimeMediaCallRequestType.IncomingCall).ConfigureAwait(false);            

            // Enforce the connection close to ensure that requests are evenly load balanced so
            // calls do no stick to one instance of the worker role.
            response.Headers.ConnectionClose = true;
            return response;
        }

        /// <summary>
        /// Handle a callback for an existing call.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route(HttpRouteConstants.OnCallbackRoute)]
        [BotAuthentication]
        public async Task<HttpResponseMessage> OnCallback(HttpRequestMessage request)
        {           
            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"Received HTTP {request.Method}, {request.RequestUri}");

            var response = await RealTimeMediaCalling.SendAsync(request, RealTimeMediaCallRequestType.CallingEvent).ConfigureAwait(false);

            // Enforce the connection close to ensure that requests are evenly load balanced so
            // calls do no stick to one instance of the worker role.
            response.Headers.ConnectionClose = true;
            return response;
        }

        /// <summary>
        /// Handle a notification for an existing call.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route(HttpRouteConstants.OnNotificationRoute)]
        [BotAuthentication]
        public async Task<HttpResponseMessage> OnNotification(HttpRequestMessage request)
        {
            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"Notification Received HTTP {request.Method} on {request.RequestUri}");

            var response = await RealTimeMediaCalling.SendAsync(request, RealTimeMediaCallRequestType.NotificationEvent).ConfigureAwait(false);

            // Enforce the connection close to ensure that requests are evenly load balanced so
            // calls do no stick to one instance of the worker role.
            response.Headers.ConnectionClose = true;
            return response;
        }
    }
}
