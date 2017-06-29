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
using System.Threading.Tasks;
using Autofac;
using Microsoft.Bot.Builder.Calling.Exceptions;
using Microsoft.Bot.Builder.Calling.ObjectModel.Contracts;
using Microsoft.Bot.Builder.RealTimeMediaCalling.Events;
using Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts;
using Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Misc;

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
        /// Container for the joinTokens(null if call can't be joined) of all current active calls on this instance.
        /// </summary>
        private ConcurrentDictionary<string, string> ActiveJoinTokens { get; }

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
        /// Create a media session for an adhoc call.
        /// </summary>
        public IRealTimeMediaSession CreateMediaSession(string correlationId, params NotificationType[] subscriptions)
        {
            return new RealTimeMediaSession(correlationId, subscriptions);
        }

        /// <summary>
        /// Instantiates the call processor
        /// </summary>
        /// <param name="scope">The autofac lifetime scope</param>
        public RealTimeMediaBotService(ILifetimeScope scope)
        {
            if (null == scope)
                throw new ArgumentNullException(nameof(scope));

            _scope = scope;
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
            using (var scope = _scope.BeginLifetimeScope(RealTimeMediaCallingModule.LifetimeScopeTag))
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

            var currentCall = new Tuple<IInternalRealTimeMediaCallService, IRealTimeMediaCall>(callService, call);
            return currentCall;
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

        /// <summary>
        /// Method responsible for processing the data sent with POST request to callback URL
        /// </summary>
        /// <param name="content">The content of request</param>
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

            Tuple<IInternalRealTimeMediaCallService, IRealTimeMediaCall> currentCall;
            //we need to extract the ID here from the ConversationResult and cache it since the ID was not available when we were sending the JoinCall request
            if (conversationResult.OperationOutcome.Type == RealTimeMediaValidOutcomes.JoinCallAppHostedMediaOutcome)
            {
                var callLegId = conversationResult.Id;
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

                currentCall = CreateCall(callLegId, correlationId);
                await AddCall(conversationResult.Id, currentCall).ConfigureAwait(false);
            }
            else if (!ActiveCalls.TryGetValue(conversationResult.Id, out currentCall))
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

            await currentCall.Item1.ProcessNotificationResult(notification).ConfigureAwait(false);
            return new ResponseResult(ResponseType.Accepted);
        }
    }  
}
