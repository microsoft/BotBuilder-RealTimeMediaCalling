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

using Autofac;
using Microsoft.Bot.Builder.Calling.Exceptions;
using Microsoft.Bot.Builder.Calling.ObjectModel.Contracts;
using Microsoft.Bot.Builder.RealTimeMediaCalling.Events;
using Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts;
using Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Misc;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling
{
    /// <summary>
    /// Processes the incoming requests and invokes the appropriate handlers for the call
    /// </summary>
    public class RealTimeMediaBotService : IRealTimeMediaBotService
    {
        /// <summary>
        /// The autofac component context.
        /// </summary>
        private readonly IComponentContext _context;

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
        private ConcurrentDictionary<string, IRealTimeMediaCall> ActiveCalls { get; }

        /// <summary>
        /// Instantiates the call processor
        /// </summary>
        /// <param name="context">The autofac component context</param>
        public RealTimeMediaBotService(IComponentContext context)
        {
            if (null == context)
                throw new ArgumentNullException(nameof(context));

            _context = context;
            ActiveCalls = new ConcurrentDictionary<string, IRealTimeMediaCall>();
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

            if (string.IsNullOrEmpty(conversation.Id))
            {
                throw new InvalidOperationException("No conversation ID found.");
            }

            var call = _context.Resolve<IRealTimeMediaCall>();

            var callService = call.CallService as IInternalRealTimeMediaCallService;
            if (null == callService)
            {
                throw new InvalidOperationException("Could not create RealTimeMediaCallService.");
            }

            callService.CallLegId = conversation.Id;

            if (string.IsNullOrEmpty(skypeChainId))
            {
                callService.CorrelationId = Guid.NewGuid().ToString();
                Trace.TraceInformation(
                    $"RealTimeMediaCallService No SkypeChainId found. Generating {callService.CorrelationId}");
            }
            else
            {
                callService.CorrelationId = skypeChainId;
            }

            var workflow = await callService.HandleIncomingCall(conversation).ConfigureAwait(false);
            if (workflow == null)
            {
                throw new BotCallingServiceException("Incoming call not handled. No workflow produced for incoming call.");
            }
            workflow.Validate();

            var callEvent = new RealTimeMediaCallEvent(conversation.Id, call);
            await InvokeCallEvent(OnCallCreated, callEvent).ConfigureAwait(false);

            IRealTimeMediaCall prevCall;
            if (ActiveCalls.TryRemove(conversation.Id, out prevCall))
            {
                Trace.TraceWarning($"Another call with the same Id {conversation.Id} exists. ending the old call");
                var prevService = (IInternalRealTimeMediaCallService)prevCall.CallService;
                await prevService.LocalCleanup().ConfigureAwait(false);
            }

            var serializedResponse = RealTimeMediaSerializer.SerializeToJson(workflow);
            return new ResponseResult(ResponseType.Accepted, serializedResponse);
        }

        /// <summary>
        /// Method responsible for processing the data sent with POST request to callback URL
        /// </summary>
        /// <param name="content">The content of request</param>
        /// <returns>Returns the response that should be sent to the sender of POST request</returns>
        public async Task<ResponseResult> ProcessCallbackAsync(string content)
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

            IRealTimeMediaCall call;
            if (!ActiveCalls.TryGetValue(conversationResult.Id, out call))
            {
                Trace.TraceWarning($"CallId {conversationResult.Id} not found");
                return new ResponseResult(ResponseType.NotFound);
            }

            var service = call.CallService as IInternalRealTimeMediaCallService;
            if (null == service)
            {
                Trace.TraceWarning($"Service for CallId {conversationResult.Id} not found");
                return new ResponseResult(ResponseType.NotFound);
            }
            var result = await service.ProcessConversationResult(conversationResult).ConfigureAwait(false);
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

            IRealTimeMediaCall call;
            if (!ActiveCalls.TryGetValue(notification.Id, out call))
            {
                Trace.TraceWarning($"CallId {notification.Id} not found");
                return new ResponseResult(ResponseType.NotFound);
            }

            var service = call.CallService as IInternalRealTimeMediaCallService;
            if (null == service)
            {
                Trace.TraceWarning($"Service for CallId {notification.Id} not found");
                return new ResponseResult(ResponseType.NotFound);
            }

            await service.ProcessNotificationResult(notification).ConfigureAwait(false);
            return new ResponseResult(ResponseType.Accepted);
        }
    }  
}
