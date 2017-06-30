// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
// 
// Microsoft Bot Framework: http://botframework.com
// 
// Bot Builder SDK GitHub:
// https://github.com/Microsoft/BotBuilder
// 
// Copyright (c) Microsoft Corporation
// All rights reserved.
// 
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Bot.Builder.Calling.Exceptions;
using Microsoft.Bot.Builder.Calling.ObjectModel.Contracts;
using Microsoft.Bot.Builder.Calling.ObjectModel.Misc;
using Microsoft.Bot.Builder.RealTimeMediaCalling.Events;
using Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts;
using Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Misc;
using Microsoft.Skype.Calling.ServiceAgents.MSA;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling
{
    /// <summary>
    /// Processes the incoming requests and invokes the appropriate handlers for the call
    /// </summary>
    internal class RealTimeMediaBotService : IInternalRealTimeMediaBotService
    {
        /// <summary>
        /// The autofac lifetime scope.
        /// </summary>
        private readonly ILifetimeScope _scope;

        /// <summary>
        /// The global settings for the RTM SDK.
        /// </summary>
        private readonly IRealTimeMediaCallServiceSettings _settings;

        private readonly Uri _defaultProdPlaceCallEndpointUrl = new Uri("https://pma.plat.skype.com:6448/platform/v1/calls");
        private readonly Uri _defaultDevPlaceCallEndpointUrl = new Uri("https://pma-dev-uswe-01.plat-dev.skype.net:6448/platform/v1/calls");

        /// <summary>
        /// Event raised when a new call is created.
        /// </summary>
        public event Func<RealTimeMediaCallEvent, Task> OnCallCreated;

        /// <summary>
        /// Event raised when an existing call is ended.
        /// </summary>
        public event Func<RealTimeMediaCallEvent, Task> OnCallEnded;

        /// <summary>
        /// Container for the current active calls on this instance.
        /// </summary>
        private ConcurrentDictionary<string, Tuple<IInternalRealTimeMediaCallService, IRealTimeMediaCall>> ActiveCalls { get; }

        /// <summary>
        /// Returns the list of all active call ids.
        /// </summary>
        public IList<string> CallIds => ActiveCalls.Keys.ToList();

        /// <summary>
        /// Returns the list of all active calls.
        /// </summary>
        public IList<IRealTimeMediaCall> Calls => ActiveCalls.Values.Select(c => c.Item2).ToList();

        /// <summary>
        /// Fetches the call for the given id.
        /// </summary>
        /// <param name="id">The ID of the call.</param>
        /// <returns>The real time media call, or null.</returns>
        public IRealTimeMediaCall GetCallForId(string id)
        {
            Tuple<IInternalRealTimeMediaCallService, IRealTimeMediaCall> tuple;
            ActiveCalls.TryGetValue(id, out tuple);
            return tuple?.Item2;
        }

        /// <summary>
        /// Instantiates the call processor
        /// </summary>
        /// <param name="scope">The autofac lifetime scope</param>
        /// <param name="settings">Settings for the call service</param>
        public RealTimeMediaBotService(ILifetimeScope scope, IRealTimeMediaCallServiceSettings settings)
        {
            if (null == scope)
                throw new ArgumentNullException(nameof(scope));

            if (null == settings)
                throw new ArgumentNullException(nameof(settings));

            _scope = scope;
            _settings = settings;
            ActiveCalls = new ConcurrentDictionary<string, Tuple<IInternalRealTimeMediaCallService, IRealTimeMediaCall>>();
        }

        private async Task InvokeCallEvent(Func<RealTimeMediaCallEvent, Task> eventHandler, RealTimeMediaCallEvent callEvent)
        {
            if (eventHandler != null)
            {
                await eventHandler.Invoke(callEvent).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// method for processing an outgoing join call request
        /// </summary>
        /// <param name="joinCallAppHostedMedia">Object contains</param>
        /// <param name="callId">call id of the call to be joined</param>
        public async Task JoinCall(JoinCallAppHostedMedia joinCallAppHostedMedia, string callId)
        {
            ///TODO remove callId in the parameter after mediaSession has been migrated into SDK
            if (joinCallAppHostedMedia != null && joinCallAppHostedMedia.JoinToken == null)
            {
                throw new InvalidOperationException("No meeting link was present in the joinCallAppHostedMedia");
            }

            var callLegId = Guid.NewGuid().ToString();
            var correlationId = callId;
            var currentCall = CreateCall(callLegId, correlationId);
            joinCallAppHostedMedia.OperationId = Guid.NewGuid().ToString();

            var workFlow = new RealTimeMediaWorkflow();
            workFlow.Links = new CallbackLink() { Callback = _settings.CallbackUrl, Notification = _settings.NotificationUrl };
            workFlow.AppState = callLegId;
            workFlow.NotificationSubscriptions = new List<NotificationType>() { NotificationType.CallStateChange };

            workFlow.Actions = new ActionBase[]
            {
                joinCallAppHostedMedia
            };
            await currentCall.Item1.HandleJoinCall(workFlow).ConfigureAwait(false);

            await AddCall(correlationId, currentCall).ConfigureAwait(false);

            HttpContent content = new StringContent(RealTimeMediaSerializer.SerializeToJson(workFlow), Encoding.UTF8, "application/json");

            await PlaceCall(content, correlationId).ConfigureAwait(false);
        }

        protected virtual async Task PlaceCall(HttpContent content, string correlationId)
        {
            var placeCallEndpointUrl = _settings.PlaceCallEndpointUrl;
            if (null == placeCallEndpointUrl)
            {
                placeCallEndpointUrl = _defaultDevPlaceCallEndpointUrl;
            }

            //place the call
            try
            {
                Trace.TraceInformation(
                    "RealTimeMediaBotService :Sending place call request");

                //TODO: add retries & logging
                using (var request = new HttpRequestMessage(HttpMethod.Post, placeCallEndpointUrl) {Content = content})
                {
                    var token = await GetBotToken(_settings.BotId, _settings.BotSecret).ConfigureAwait(false);

                    request.Headers.Add("X-Microsoft-Skype-Chain-ID", correlationId);
                    request.Headers.Add("X-Microsoft-Skype-Message-ID", Guid.NewGuid().ToString());
                    //TODO make this an http factory and inject it to the call service and bot service
                    var client = RealTimeMediaCallService.GetHttpClient();

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    var response = await client.SendAsync(request).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    Trace.TraceInformation($"RealTimeMediaBotService [{correlationId}]: Response to join call: {response}");
                }
            }
            catch (Exception exception)
            {
                Trace.TraceError(
                    $"RealTimeMediaBotService [{correlationId}]: Received error while sending request to subscribe participant. Message: {exception}");
                throw;
            }
        }

        /// <summary>
        /// Method to obtain bot token from AAD
        /// </summary>
        /// <param name="botId"></param>
        /// <param name="botSecret"></param>
        /// <returns></returns>
        internal async Task<string> GetBotToken(string botId, string botSecret)
        {
            if (null == botId)
            {
                throw new ArgumentNullException(nameof(botId));
            }

            if (null == botSecret)
            {
                throw new ArgumentNullException(nameof(botSecret));
            }

            using (var tokenClient = new MsaAuthTokenService(
                new Uri(@"https://login.microsoftonline.com/common/oauth2/v2.0/token"),
                botId,
                botSecret,
                @"https://api.botframework.com/.default",
                null))
            {
                await tokenClient.Init().ConfigureAwait(false);
                var accessToken = tokenClient.Token;
                return accessToken;
            }
        }


        /// <summary>
        /// Method responsible for processing the data sent with POST request to incoming call URL
        /// </summary>
        /// <param name="content">The content of request</param>
        /// <param name="skypeChainId">X-Microsoft-Skype-Chain-Id header value used to associate calls across different services</param>
        /// <returns>Returns the response that should be sent to the sender of POST request</returns>
        public async Task<ResponseResult> ProcessIncomingCallAsync(string content, string skypeChainId)
        {
            if (content == null)
            {
                return new ResponseResult(ResponseType.BadRequest);
            }

            Conversation conversation;
            try
            {
                conversation = RealTimeMediaSerializer.DeserializeFromJson<Conversation>(content);
                if (conversation == null)
                {
                    Trace.TraceWarning($"Could not deserialize the incoming request.. returning badrequest");
                    return new ResponseResult(ResponseType.BadRequest);
                }

                conversation.Validate();
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"Exception in conversation validate {ex}");
                return new ResponseResult(ResponseType.BadRequest);
            }

            var callLegId = conversation.Id;
            string correlationId;
            if (string.IsNullOrEmpty(skypeChainId))
            {
                correlationId = Guid.NewGuid().ToString();
                Trace.TraceWarning(
                    $"RealTimeMediaCallService No SkypeChainId found. Generating {correlationId}");
            }
            else
            {
                correlationId = skypeChainId;
            }

            var currentCall = CreateCall(callLegId, correlationId);

            //TODO store jointoken, if present, to ActiveJoinTokens

            var workflow = await currentCall.Item1.HandleIncomingCall(conversation).ConfigureAwait(false);
            if (workflow == null)
            {
                throw new BotCallingServiceException("Incoming call not handled. No workflow produced for incoming call.");
            }
            workflow.Validate();

            await AddCall(conversation.Id, currentCall).ConfigureAwait(false);

            var serializedResponse = RealTimeMediaSerializer.SerializeToJson(workflow);
            return new ResponseResult(ResponseType.Accepted, serializedResponse);
        }

        private Tuple<IInternalRealTimeMediaCallService, IRealTimeMediaCall> CreateCall(string callLegId, string correlationId)
        {
            IRealTimeMediaCall call;
            IInternalRealTimeMediaCallService callService;
            using (var scope = _scope.BeginLifetimeScope(RealTimeMediaCallingScope.LifetimeScopeTag))
            {
                var parameters = new RealTimeMediaCallServiceParameters(callLegId, correlationId);
                scope.Resolve<RealTimeMediaCallServiceParameters>(TypedParameter.From(parameters));
                callService = scope.Resolve<IInternalRealTimeMediaCallService>();
                call = scope.Resolve<IRealTimeMediaCall>();
            }

            if (callService == null)
            {
                throw new InvalidOperationException("The call service was not resolved correctly.");
            }

            if (call == null)
            {
                throw new InvalidOperationException("The call was not resolved correctly.");
            }

            return new Tuple<IInternalRealTimeMediaCallService, IRealTimeMediaCall>(callService, call);
        }

        private async Task AddCall(string conversationId, Tuple<IInternalRealTimeMediaCallService, IRealTimeMediaCall> currentCall)
        {
            var callEvent = new RealTimeMediaCallEvent(conversationId, currentCall.Item2);
            await InvokeCallEvent(OnCallCreated, callEvent).ConfigureAwait(false);

            Tuple<IInternalRealTimeMediaCallService, IRealTimeMediaCall> prevCall = null;
            ActiveCalls.AddOrUpdate(conversationId, currentCall, (key, oldCall) =>
            {
                prevCall = oldCall;
                return currentCall;
            });

            if (prevCall?.Item1 != null)
            {
                Trace.TraceWarning($"Another call with the same Id {conversationId} exists. ending the old call");
                var prevService = prevCall.Item1;
                await prevService.LocalCleanup().ConfigureAwait(false);
            }
        }

        private async Task EndCall(string conversationId, Tuple<IInternalRealTimeMediaCallService, IRealTimeMediaCall> currentCall)
        {
            var callEvent = new RealTimeMediaCallEvent(conversationId, currentCall.Item2);
            await InvokeCallEvent(OnCallEnded, callEvent).ConfigureAwait(false);

            Tuple<IInternalRealTimeMediaCallService, IRealTimeMediaCall> callToRemove = null;
            if(!ActiveCalls.TryRemove(conversationId, out callToRemove))
            {
                Trace.TraceWarning($"CallId {conversationId} not found");
            }
        }

        /// <summary>
        /// Method responsible for processing the data sent with POST request to callback URL
        /// </summary>
        /// <param name="content">The content of request</param>
        /// <param name="skypeChainId">THe ID to associate a call across multiple processes</param>
        /// <returns>Returns the response that should be sent to the sender of POST request</returns>
        public async Task<ResponseResult> ProcessCallbackAsync(string content, string skypeChainId)
        {
            ConversationResult conversationResult;
            if (content == null)
            {
                return new ResponseResult(ResponseType.BadRequest);
            }
            try
            {
                conversationResult = RealTimeMediaSerializer.DeserializeFromJson<ConversationResult>(content);
                if (conversationResult == null)
                {
                    Trace.TraceWarning($"Could not deserialize the callback.. returning badrequest");
                    return new ResponseResult(ResponseType.BadRequest);
                }
          
                conversationResult.Validate();
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"Exception in conversationResult validate {ex}");
                return new ResponseResult(ResponseType.BadRequest);
            }

            Tuple<IInternalRealTimeMediaCallService, IRealTimeMediaCall> currentCall =null;
            if (!ActiveCalls.TryGetValue(conversationResult.Id, out currentCall))
            {
                Trace.TraceWarning($"CallId {conversationResult.Id} not found");
                return new ResponseResult(ResponseType.NotFound);
            }

            var result = await currentCall.Item1.ProcessConversationResult(conversationResult).ConfigureAwait(false);
            return new ResponseResult(ResponseType.Accepted, result);
        }

        /// <summary>
        /// Method responsible for processing the data sent with POST request to notification URL
        /// </summary>
        /// <param name="content">The content of request</param>
        /// <returns>Returns the response that should be sent to the sender of POST request</returns>
        public async Task<ResponseResult> ProcessNotificationAsync(string content)
        {
            NotificationBase notification;
            if (content == null)
            {
                return new ResponseResult(ResponseType.BadRequest);
            }
            try
            {
                Trace.TraceWarning($"Received notification {content}");
                notification = RealTimeMediaSerializer.DeserializeFromJson<NotificationBase>(content);
                if (notification == null)
                {
                    Trace.TraceWarning($"Could not deserialize the notification.. returning badrequest");
                    return new ResponseResult(ResponseType.BadRequest);
                }

                notification.Validate();
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"Exception in notification validate {ex}");
                return new ResponseResult(ResponseType.BadRequest);
            }

            Tuple<IInternalRealTimeMediaCallService, IRealTimeMediaCall> currentCall;
            if (!ActiveCalls.TryGetValue(notification.Id, out currentCall))
            {
                Trace.TraceWarning($"CallId {notification.Id} not found");
                return new ResponseResult(ResponseType.NotFound);
            }
            if (notification.Type == NotificationType.CallStateChange && (notification as CallStateChangeNotification)?.CurrentState == CallState.Terminated)
            {
                await EndCall(notification.Id, currentCall).ConfigureAwait(false);
            }
            await currentCall.Item1.ProcessNotificationResult(notification).ConfigureAwait(false);
            return new ResponseResult(ResponseType.Accepted);
        }
    }  
}
