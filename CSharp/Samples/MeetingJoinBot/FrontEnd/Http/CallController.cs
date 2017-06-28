/**************************************************
*                                                                                    *
*   © Microsoft Corporation. All rights reserved.  *
*                                                                                    *
**************************************************/

using FrontEnd.CallLogic;
using FrontEnd.Logging;
using Microsoft.Bot.Builder.RealTimeMediaCalling;
using Microsoft.Bot.Connector;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

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
            var settings = new RealTimeMediaCallingBotServiceSettings();
            RealTimeMediaCalling.RegisterRealTimeMediaCallingBot(
                settings,
                b => new RealTimeMediaBot(b),
                c => new RealTimeMediaCall(c));
        }

        /// <summary>
        /// Handle an incoming call.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route(HttpRouteConstants.OnIncomingCallRoute)]
        [BotAuthentication]
        public async Task<HttpResponseMessage> OnIncomingCallAsync()
        {
            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"Received HTTP {Request.Method}, {Request.RequestUri}"); 

            var response = await RealTimeMediaCalling.SendAsync(Request, RealTimeMediaCallRequestType.IncomingCall).ConfigureAwait(false);

            // Enforce the connection close to ensure that requests are evenly load balanced so
            // calls do no stick to one instance of the worker role.
            response.Headers.ConnectionClose = true;
            return response;
        }

        /// <summary>
        /// Handle a callback for an existing call.
        /// </summary>        
        /// <returns></returns>
        [HttpPost]
        [Route(HttpRouteConstants.OnCallbackRoute)]
        [BotAuthentication]
        public async Task<HttpResponseMessage> OnCallbackAsync()
        {           
            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"Received HTTP {Request.Method}, {Request.RequestUri}");

            //let the RealTimeMediaCalling SDK know of the incoming callback. The SDK deserializes the callback, validates it and calls the appropriate  
            //events on the IRealTimeMediaCall for this request
            var response = await RealTimeMediaCalling.SendAsync(Request, RealTimeMediaCallRequestType.CallingEvent).ConfigureAwait(false);

            // Enforce the connection close to ensure that requests are evenly load balanced so
            // calls do no stick to one instance of the worker role.
            response.Headers.ConnectionClose = true;
            return response;              
        }

        /// <summary>
        /// Handle a notification for an existing call.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route(HttpRouteConstants.OnNotificationRoute)]
        [BotAuthentication]
        public async Task<HttpResponseMessage> OnNotificationAsync()
        {
            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"Notification Received HTTP {Request.Method} on {Request.RequestUri}");

            //let the RealTimeMediaCalling SDK know of the incoming notification. The SDK deserializes the callback, validates it and calls the appropriate  
            //events on the IRealTimeMediaCall for this request
            var response = await RealTimeMediaCalling.SendAsync(Request, RealTimeMediaCallRequestType.NotificationEvent).ConfigureAwait(false);

            // Enforce the connection close to ensure that requests are evenly load balanced so
            // calls do no stick to one instance of the worker role.
            response.Headers.ConnectionClose = true;
            return response;
        }

        /// <summary>
        /// Get the image of screen sharing.
        /// </summary>
        /// <param name="callid">Id of the call to retrieve image</param>
        /// <returns></returns>
        [HttpGet]
        [Route(HttpRouteConstants.Image)]
        public HttpResponseMessage OnGetImage(string callid)
        {
            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"Retrieving image for call id {callid}");

            try
            {
                return VideoImageViewer.GetVideoImageResponse(callid);
            }
            catch (Exception e)
            {
                Log.Warning(new CallerInfo(), LogContext.FrontEnd, $"[OnGetImage] Exception {e.ToString()}");
                var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                response.Content = new StringContent(e.ToString());
                return response;
            }
        }
    }
}
