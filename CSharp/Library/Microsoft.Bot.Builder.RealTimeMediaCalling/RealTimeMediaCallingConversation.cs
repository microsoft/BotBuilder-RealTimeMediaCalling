using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Bot.Builder.Calling.ObjectModel.Misc;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling
{
    /// <summary>
    /// The top level class that is used to register the bot for enabling real-time media communication
    /// </summary>
    public static partial class RealTimeMediaCallingConversation
    {
        public static readonly IContainer Container;

        static RealTimeMediaCallingConversation()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new RealTimeMediaCallingModule_MakeBot());
            Container = builder.Build();
        }
        
        /// <summary>
        /// Register the function to be called to create a bot along with configuration settings. 
        /// </summary>
        /// <param name="MakeCallingBot"> The factory method to make the calling bot.</param>
        public static void RegisterRealTimeMediaCallingBot(Func<IRealTimeMediaCallService, IRealTimeMediaCall> MakeCallingBot, IRealTimeMediaCallServiceSettings realTimeBotServiceSettings)
        {
            Trace.TraceInformation($"Registering real-time media calling bot");
            if(realTimeBotServiceSettings.CallbackUrl == null)
            {
                throw new ArgumentNullException("callbackUrl");
            }

            if (realTimeBotServiceSettings.NotificationUrl == null)
            {
                throw new ArgumentNullException("notificationUrl");
            }

            RealTimeMediaCallingModule_MakeBot.Register(Container, MakeCallingBot, realTimeBotServiceSettings);
        }

        /// <summary>
        /// Process an incoming request
        /// </summary>
        /// <param name="toBot"> The calling request sent to the bot.</param>
        /// <param name="callRequestType"> The type of calling request.</param>
        /// <returns> The response from the bot.</returns>
        public static async Task<HttpResponseMessage> SendAsync(HttpRequestMessage toBot, RealTimeMediaCallRequestType callRequestType)
        {
            using (var scope = RealTimeMediaCallingModule.BeginLifetimeScope(Container, toBot))
            {                
                var context = scope.Resolve<RealTimeMediaCallingContext>();
                var parsedRequest = await context.ProcessRequest(callRequestType).ConfigureAwait(false);
               
                if (parsedRequest.Faulted())
                {
                    return Utils.GetResponseMessage(parsedRequest.ParseStatusCode, parsedRequest.Content);
                }
                else
                {
                    try
                    {
                        ResponseResult result;
                        var callingBotService = scope.Resolve<IRealTimeCallProcessor>();
                        switch (callRequestType)
                        {
                            case RealTimeMediaCallRequestType.IncomingCall:
                                result = await callingBotService.ProcessIncomingCallAsync(parsedRequest.Content, parsedRequest.SkypeChaindId).ConfigureAwait(false);
                                break;

                            case RealTimeMediaCallRequestType.CallingEvent:
                                result = await callingBotService.ProcessCallbackAsync(parsedRequest.Content).ConfigureAwait(false);
                                break;

                            case RealTimeMediaCallRequestType.NotificationEvent:
                                result = await callingBotService.ProcessNotificationAsync(parsedRequest.Content).ConfigureAwait(false);
                                break;

                            default:
                                result = new ResponseResult(ResponseType.BadRequest, $"Unsupported call request type: {callRequestType}");
                                break;
                        }

                        return GetHttpResponseForResult(result);
                    }
                    catch (Exception e)
                    {
                        Trace.TraceError($"RealTimeMediaCallingConversation: {e}");
                        return Utils.GetResponseMessage(HttpStatusCode.InternalServerError, e.ToString());
                    }
                }
            }
        }

        private static HttpResponseMessage GetHttpResponseForResult(ResponseResult result)
        {
            HttpResponseMessage responseMessage;
            switch(result.ResponseType)
            {
                case ResponseType.Accepted:
                    responseMessage = new HttpResponseMessage(HttpStatusCode.Accepted);
                    break;
                case ResponseType.BadRequest:
                    responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
                    break;
                case ResponseType.NotFound:
                    responseMessage = new HttpResponseMessage(HttpStatusCode.NotFound);
                    break;
                default:
                    responseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                    break;
            }

            if (!string.IsNullOrEmpty(result.Content))
            {
                responseMessage.Content = new StringContent(result.Content);
            }
            return responseMessage;
        }
    }   
}
