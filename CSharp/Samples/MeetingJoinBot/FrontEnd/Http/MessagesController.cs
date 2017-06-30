/**************************************************
*                                                                                    *
*   © Microsoft Corporation. All rights reserved.  *
*                                                                                    *
**************************************************/

using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using FrontEnd.CallLogic;
using FrontEnd.Logging;
using Microsoft.Bot.Builder.RealTimeMediaCalling;
using Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts;
using Microsoft.Bot.Connector;

namespace FrontEnd.Http
{
    [BotAuthentication]
    [RoutePrefix("api/messages")]
    public class MessagesController : ApiController
    {
        public static string MessagesServiceUrl;
        public static ChannelAccount BotAccount;

        /// <summary>
        /// POST: api/Messages
        /// receive a message from a user and send replies
        /// </summary>
        /// <param name="activity"></param>
        [HttpPost]
        [Route(HttpRouteConstants.OnIncomingMessageRoute)]
        public virtual async Task<HttpResponseMessage> Post([FromBody] Activity activity)
        {
            //cache the serviceUrl and bot account 
            MessagesServiceUrl = activity.ServiceUrl;
            BotAccount = activity.Recipient;
            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"Received chat message.. checking if there is an active media call for this thread");
            var bot = RealTimeMediaCalling.GetBot();
            await RealTimeMediaCall.SendUrlForConversationId(activity.Conversation.Id);
            
            return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
        }
    }
}
