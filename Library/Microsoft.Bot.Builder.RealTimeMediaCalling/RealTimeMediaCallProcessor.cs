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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Calling.Exceptions;
using Microsoft.Bot.Builder.Calling.ObjectModel.Contracts;
using Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts;
using Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Misc;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling
{
    /// <summary>
    /// Processes the incoming requests and invokes the appropriate handlers for the call
    /// </summary>
    public class RealTimeCallProcessor : IRealTimeCallProcessor
    {
        /// <summary>
        /// Container for the current active calls on this instance.
        /// </summary>
        private readonly ConcurrentDictionary<string, RealTimeMediaCallService> _activeCalls;
      
        /// <summary>
        /// Configuration settings
        /// </summary>
        IRealTimeMediaCallServiceSettings _settings;

        /// <summary>
        /// Function to create a bot to deliver events
        /// </summary>
        Func<IRealTimeMediaCallService, IRealTimeMediaCall> _makeBot;

        /// <summary>
        /// Instantiates the call processor
        /// </summary>
        /// <param name="settings">Configuration settings</param>
        /// <param name="makeBot">Function to create a bot</param>
        public RealTimeCallProcessor(IRealTimeMediaCallServiceSettings settings, Func<IRealTimeMediaCallService, IRealTimeMediaCall> makeBot)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            _settings = settings;
            _makeBot = makeBot;
            _activeCalls = new ConcurrentDictionary<string, RealTimeMediaCallService>();
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

                // todo put it back when needed
                // conversation.Validate();
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"Exception in conversation validate {ex}");
                return new ResponseResult(ResponseType.BadRequest);
            }            

            RealTimeMediaCallService service = new RealTimeMediaCallService(conversation.Id, skypeChainId, _makeBot, _settings);            
            var workflow = await service.HandleIncomingCall(conversation).ConfigureAwait(false);
            if (workflow == null)
            {
                throw new BotCallingServiceException("Incoming call not handled. No workflow produced for incoming call.");
            }
            workflow.Validate();

            RealTimeMediaCallService prevService;
            if (_activeCalls.TryRemove(conversation.Id, out prevService))
            {
                Trace.TraceWarning($"Another call with the same Id {conversation.Id} exists. ending the old call");
                await prevService.LocalCleanup().ConfigureAwait(false);
            }

            _activeCalls[conversation.Id] = service;

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

            RealTimeMediaCallService service;
            if (!_activeCalls.TryGetValue(conversationResult.Id, out service))
            {
                Trace.TraceWarning($"CallId {conversationResult.Id} not found");
                return new ResponseResult(ResponseType.NotFound);
            }
            return new ResponseResult(ResponseType.Accepted, await service.ProcessConversationResult(conversationResult).ConfigureAwait(false));
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

            RealTimeMediaCallService service;
            if (!_activeCalls.TryGetValue(notification.Id, out service))
            {
                return new ResponseResult(ResponseType.NotFound, $"Call {notification.Id} not found");
            }

            await service.ProcessNotificationResult(notification).ConfigureAwait(false);
            return new ResponseResult(ResponseType.Accepted);
        }

        /// <summary>
        /// Method responsible for processing the data sent with POST request to command URL
        /// </summary>
        /// <param name="content">The content of request</param>
        /// <returns>Returns the response that should be sent to the sender of POST request</returns>
        public async Task<ResponseResult> ProcessControlCommandAsync(string content)
        {
            NotificationBase notification;
            if (content == null)
            {
                return new ResponseResult(ResponseType.BadRequest);
            }
            try
            {
                Trace.TraceWarning($"Received command {content}");
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

            RealTimeMediaCallService service;
            if (!_activeCalls.TryGetValue(notification.Id, out service))
            {
                return new ResponseResult(ResponseType.NotFound, $"Call {notification.Id} not found");
            }

            await service.ProcessControlCommandResult(notification).ConfigureAwait(false);
            return new ResponseResult(ResponseType.Accepted);
        }
    }  
}
