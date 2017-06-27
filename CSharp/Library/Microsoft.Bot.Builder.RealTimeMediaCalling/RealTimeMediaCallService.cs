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
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Calling.Events;
using Microsoft.Bot.Builder.Calling.Exceptions;
using Microsoft.Bot.Builder.Calling.ObjectModel.Contracts;
using Microsoft.Bot.Builder.Calling.ObjectModel.Misc;
using Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Misc;
using Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts;
using Microsoft.Bot.Builder.RealTimeMediaCalling.Events;
using System.Net.Http.Headers;
using System.Reflection;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling
{
    /// <summary>
    /// Service that handles per call requests
    /// </summary>            
    public class RealTimeMediaCallService : IInternalRealTimeMediaCallService
    {
        private readonly Uri _callbackUrl;
        private readonly Uri _notificationUrl;

        private Uri _subscriptionLink;
        private Uri _callLink;
        
        private Timer _timer;
        private const int CallExpiredTimerInterval = 1000 * 60 * 10; //10 minutes

        /// <summary>
        /// Id for this call
        /// </summary>
        public string CallLegId { get; set; }

        /// <summary>
        /// CorrelationId for this call.
        /// </summary>
        public string CorrelationId
        {
            get => this.MediaSession.CorrelationId;
            set => ((IInternalRealTimeMediaSession) this.MediaSession).CorrelationId = value;
        }

        /// <summary>
        /// Event raised when bot receives incoming call
        /// </summary>
        public event Func<RealTimeMediaIncomingCallEvent, Task> OnIncomingCallReceived;

        /// <summary>
        /// Event raised when the bot gets the outcome of Answer action. If the operation was successful the call is established
        /// </summary>
        public event Func<AnswerAppHostedMediaOutcomeEvent, Task> OnAnswerAppHostedMediaCompleted;

        /// <summary>
        /// Event raised when specified workflow fails to be validated by Bot platform
        /// </summary>
        public event Func<RealTimeMediaWorkflowValidationOutcomeEvent, Task> OnWorkflowValidationFailed;

        /// <summary>
        /// Event raised when bot receives call state change notification
        /// </summary>
        public event Func<CallStateChangeNotification, Task> OnCallStateChangeNotification;

        /// <summary>
        /// Event raised when bot receives roster update notification
        /// </summary>
        public event Func<RosterUpdateNotification, Task> OnRosterUpdateNotification;

        /// <summary>
        /// Event raised when bot needs to cleanup an existing call
        /// </summary>
        public event Func<Task> OnCallCleanup;

        public IRealTimeMediaSession MediaSession { get; }

        /// <summary>
        /// Instantiates the service with settings to handle a call
        /// </summary>
        /// <param name="settings">The settings for the RTM call service.</param>
        /// <param name="mediaSession">The media session for this call.</param>
        public RealTimeMediaCallService(IRealTimeMediaCallServiceSettings settings, IRealTimeMediaSession mediaSession)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (settings.CallbackUrl == null || settings.NotificationUrl == null)
            {
                throw new ArgumentNullException("callback settings");
            }

            if (mediaSession == null)
            {
                throw new ArgumentNullException(nameof(mediaSession));
            }

            MediaSession = mediaSession;
            _callbackUrl = settings.CallbackUrl;
            _notificationUrl = settings.NotificationUrl;
            _timer = new Timer(CallExpiredTimerCallback, null, CallExpiredTimerInterval, Timeout.Infinite);
        }        

        /// <summary>
        /// Keeps track of receiving AnswerAppHostedMediaOutcome. If the answer does not come back, bot can start leaking sockets.
        /// </summary>
        /// <param name="state"></param>
        private void CallExpiredTimerCallback(object state)
        {
            Trace.TraceInformation(
            $"RealTimeMediaCallService [{CallLegId}]: CallExpiredTimerCallback called.. cleaning up the call");

            Task.Run(async () =>
            {
                try
                {
                    await LocalCleanup().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Trace.TraceError(
                        $"RealTimeMediaCallService [{CallLegId}]: Error in LocalCleanup {ex}");
                }
            });
        }

        /// <summary>
        /// Invokes notifications on the bot
        /// </summary>
        /// <param name="notification">Notification to be sent</param>
        /// <returns></returns>
        public Task ProcessNotificationResult(NotificationBase notification)
        {
            Trace.TraceInformation(
                $"RealTimeMediaCallService [{CallLegId}]: Received the notification for {notification.Type} operation, callId: {notification.Id}");

            switch (notification.Type)
            {
                case NotificationType.CallStateChange:
                    return HandleCallStateChangeNotification(notification as CallStateChangeNotification);

                case NotificationType.RosterUpdate:
                    return HandleRosterUpdateNotification(notification as RosterUpdateNotification);
            }
            throw new BotCallingServiceException($"[{CallLegId}]: Unknown notification type {notification.Type}");
        }

        private async Task HandleCallStateChangeNotification(CallStateChangeNotification notification)
        {
            Trace.TraceInformation($"RealTimeMediaCallService [{CallLegId}]: Received CallStateChangeNotification.. ");
            notification.Validate();

            var eventHandler = OnCallStateChangeNotification;
            if (eventHandler != null)
                await eventHandler.Invoke(notification).ConfigureAwait(false);

            return;
        }

        private async Task HandleRosterUpdateNotification(RosterUpdateNotification notification)
        {
            Trace.TraceInformation($"RealTimeMediaCallService [{CallLegId}]: Received RosterUpdateNotification");
            notification.Validate();

            var eventHandler = OnRosterUpdateNotification;
            if (eventHandler != null)
                await eventHandler.Invoke(notification).ConfigureAwait(false);

            return;
        }

        /// <summary>
        /// Invokes handlers for callback on the bot
        /// </summary>
        /// <param name="conversationResult">ConversationResult that has the details of the callback</param>
        /// <returns></returns>
        public async Task<string> ProcessConversationResult(ConversationResult conversationResult)
        {
            conversationResult.Validate();
            var newWorkflowResult = await PassActionResultToHandler(conversationResult).ConfigureAwait(false);
            if (newWorkflowResult == null)
            {
                throw new BotCallingServiceException($"[{CallLegId}]: No workflow returned for AnswerAppHostedMediaOutcome");
            }

            bool expectEmptyActions = false;
            if(conversationResult.OperationOutcome.Type == RealTimeMediaValidOutcomes.AnswerAppHostedMediaOutcome && conversationResult.OperationOutcome.Outcome == Outcome.Success)
            {
                Uri link;
                if (conversationResult.Links.TryGetValue("subscriptions", out link))
                {
                    _subscriptionLink = link;
                    Trace.TraceInformation($"RealTimeMediaCallService [{CallLegId}]: Caching subscription link {link}");
                }

                if (conversationResult.Links.TryGetValue("call", out link))
                {
                    _callLink = link;
                    Trace.TraceInformation($"RealTimeMediaCallService [{CallLegId}]: Caching call link {link}");
                }
                expectEmptyActions = true;

                Trace.TraceInformation($"RealTimeMediaCallService [{CallLegId}]: Disposing call expiry timer");
                _timer.Dispose();
            }

            newWorkflowResult.Validate(expectEmptyActions);
            return RealTimeMediaSerializer.SerializeToJson(newWorkflowResult);
        }

        private Task<RealTimeMediaWorkflow> PassActionResultToHandler(ConversationResult receivedConversationResult)
        {
            Trace.TraceInformation(
                $"RealTimeMediaCallService [{CallLegId}]: Received the outcome for {receivedConversationResult.OperationOutcome.Type} operation, callId: {receivedConversationResult.OperationOutcome.Id}");

            switch (receivedConversationResult.OperationOutcome.Type)
            {
                case RealTimeMediaValidOutcomes.AnswerAppHostedMediaOutcome:
                    return HandleAnswerAppHostedMediaOutcome(receivedConversationResult, receivedConversationResult.OperationOutcome as AnswerAppHostedMediaOutcome);
                
                case ValidOutcomes.WorkflowValidationOutcome:
                    return HandleWorkflowValidationOutcome(receivedConversationResult, receivedConversationResult.OperationOutcome as WorkflowValidationOutcome);
            }

            throw new BotCallingServiceException($"[{CallLegId}]: Unknown conversation result type {receivedConversationResult.OperationOutcome.Type}");
        }

        /// <summary>
        /// Invokes handler for incoming call
        /// </summary>
        /// <param name="conversation">Conversation corresponding to the incoming call</param>
        /// <returns>WorkFlow to be executed for the call</returns>
        public async Task<RealTimeMediaWorkflow> HandleIncomingCall(Conversation conversation)
        {
            Trace.TraceInformation($"RealTimeMediaCallService [{CallLegId}]: Received incoming call");
            var incomingCall = new RealTimeMediaIncomingCallEvent(conversation, CreateInitialWorkflow());
            var eventHandler = OnIncomingCallReceived;
            if (eventHandler != null)
                await eventHandler.Invoke(incomingCall).ConfigureAwait(false);
            else
            {
                Trace.TraceInformation($"RealTimeMediaCallService [{CallLegId}]: No handler specified for incoming call");
                return null;
            }

            return incomingCall.RealTimeMediaWorkflow;
        }

        private Task<RealTimeMediaWorkflow> HandleAnswerAppHostedMediaOutcome(ConversationResult conversationResult, AnswerAppHostedMediaOutcome answerAppHostedMediaOutcome)
        {            
            var outcomeEvent = new AnswerAppHostedMediaOutcomeEvent(conversationResult, CreateInitialWorkflow(), answerAppHostedMediaOutcome);
            var eventHandler = OnAnswerAppHostedMediaCompleted;
            return InvokeHandlerIfSet(eventHandler, outcomeEvent);
        }

        private Task<RealTimeMediaWorkflow> HandleWorkflowValidationOutcome(
            ConversationResult conversationResult,
            WorkflowValidationOutcome workflowValidationOutcome)
        {
            var outcomeEvent = new RealTimeMediaWorkflowValidationOutcomeEvent(conversationResult, CreateInitialWorkflow(), workflowValidationOutcome);
            var eventHandler = OnWorkflowValidationFailed;
            return InvokeHandlerIfSet(eventHandler, outcomeEvent);
        }

        public Task LocalCleanup()
        {
            var eventHandler = OnCallCleanup;
            return InvokeHandlerIfSet(eventHandler, "Cleanup");
        }

        /// <summary>
        /// Subscribe to a video or video based screen sharing channel
        /// </summary>
        /// <param name="videoSubscription"></param>
        /// <returns></returns>
        public async Task Subscribe(VideoSubscription videoSubscription)
        {
            if (_subscriptionLink == null)
            {
                throw new InvalidOperationException($"[{CallLegId}]: No subscription link was present in the AnswerAppHostedMediaOutcome");
            }
            
            videoSubscription.Validate();
            HttpContent content = new StringContent(RealTimeMediaSerializer.SerializeToJson(videoSubscription), Encoding.UTF8, JSONConstants.ContentType);

            //Subscribe
            try
            {
                Trace.TraceInformation(
                        $"RealTimeMediaCallService [{CallLegId}]: Sending subscribe request for " +
                        $"user: {videoSubscription.ParticipantIdentity}" +
                        $"subscriptionLink: {_subscriptionLink}");

                //TODO: add retries & logging
                using (var request = new HttpRequestMessage(HttpMethod.Put, _subscriptionLink) { Content = content })
                {
                    request.Headers.Add("X-Microsoft-Skype-Chain-ID", CorrelationId);
                    request.Headers.Add("X-Microsoft-Skype-Message-ID", Guid.NewGuid().ToString());

                    var client = GetHttpClient();
                    var response = await client.SendAsync(request).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();

                    Trace.TraceInformation($"RealTimeMediaCallService [{CallLegId}]: Response to subscribe: {response}");
                }
            }
            catch (Exception exception)
            {
                Trace.TraceError($"RealTimeMediaCallService [{CallLegId}]: Received error while sending request to subscribe participant. Message: {exception}");
                throw;
            }
        }

        private static HttpClient GetHttpClient()
        {
            var clientHandler = new HttpClientHandler { AllowAutoRedirect = false };
            DelegatingHandler[] handlers = new DelegatingHandler[] { new RetryMessageHandler(), new LoggingMessageHandler() };
            HttpClient client = HttpClientFactory.Create(clientHandler, handlers);
            var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Microsoft-BotFramework-RealTimeMedia", assemblyVersion));
            return client;
        }

        /// <summary>
        /// Ends the call. Local cleanup will not be done
        /// </summary>
        /// <returns></returns>
        public async Task EndCall()
        {
            if (_callLink == null)
            {
                throw new InvalidOperationException($"[{CallLegId}]: No call link was present in the AnswerAppHostedMediaOutcome");
            }

            using (var request = new HttpRequestMessage(HttpMethod.Delete, _callLink))
            {                
                request.Headers.Add("X-Microsoft-Skype-Chain-ID", CorrelationId);
                request.Headers.Add("X-Microsoft-Skype-Message-ID", Guid.NewGuid().ToString());

                var client = GetHttpClient();
                var response = await client.SendAsync(request).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                Trace.TraceInformation($"RealTimeMediaCallService [{CallLegId}]: Response to Delete: {response}");
            }
        }

        private async Task<RealTimeMediaWorkflow> InvokeHandlerIfSet<T>(Func<T, Task> action, T outcomeEventBase) where T : OutcomeEventBase
        {
            if (action != null)
            {
                await action.Invoke(outcomeEventBase).ConfigureAwait(false);
                return outcomeEventBase.ResultingWorkflow as RealTimeMediaWorkflow;
            }
            throw new BotCallingServiceException($"[{CallLegId}]: No event handler set for {outcomeEventBase.ConversationResult.OperationOutcome.Type} outcome");
        }

        private async Task InvokeHandlerIfSet(Func<Task> action, string type) 
        {
            if (action == null)
            {
                throw new BotCallingServiceException($"[{CallLegId}]: No event handler set for {type}");
            }

            await action.Invoke().ConfigureAwait(false);
        }

        private RealTimeMediaWorkflow CreateInitialWorkflow()
        {
            var workflow = new RealTimeMediaWorkflow();
            workflow.Links = GetCallbackLink();
            workflow.Actions = new List<ActionBase>();
            workflow.AppState = CallLegId;
            workflow.NotificationSubscriptions = new List<NotificationType>() { NotificationType.CallStateChange };
            return workflow;
        }

        private CallbackLink GetCallbackLink()
        {
            return new CallbackLink() { Callback = _callbackUrl, Notification = _notificationUrl };
        }
    }
}
