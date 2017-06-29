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
    /// MessagesController handles the incoming messages for this bot.
    /// </summary>    
    public class MessagesController : ApiController
    {
        /// <summary>
        /// Handle an incoming message.
        /// </summary>
        /// <param name="request"></param>
        /// <returns>HttpResponseMessage with StatusCode as Accepted</returns>
        [HttpPost]
        [Route(HttpRouteConstants.OnIncomingMessageRoute)]
        [BotAuthentication]
        public HttpResponseMessage Post(HttpRequestMessage request)
        {
            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"Received HTTP {request.Method}, {request.RequestUri}"); 
            return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
        }
    }
}
