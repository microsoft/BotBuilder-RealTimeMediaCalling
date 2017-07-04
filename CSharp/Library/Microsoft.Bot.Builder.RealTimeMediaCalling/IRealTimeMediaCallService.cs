﻿// 
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
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Calling.ObjectModel.Contracts;
using Microsoft.Bot.Builder.RealTimeMediaCalling.Events;
using Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling
{
    /// <summary>
    /// Service interface that invokes the appropriate events on an incoming real-time media call and provides functions to make outgoing requests for that call.
    /// </summary>
    public interface IRealTimeMediaCallService 
    {
        /// <summary>
        /// Id used for correlating logs across different services
        /// </summary>
        string CorrelationId { get; }

        /// <summary>
        /// Event raised when specified workflow fails to be validated by Bot platform
        /// </summary>
        event Func<RealTimeMediaWorkflowValidationOutcomeEvent, Task> OnWorkflowValidationFailed;

        /// <summary>
        /// Event raised when bot receives incoming call
        /// </summary>
        event Func<RealTimeMediaIncomingCallEvent, Task> OnIncomingCallReceived;

        /// <summary>
        /// Event raised when the bot gets the outcome of AnswerAppHostedMedia action and the call is established.
        /// </summary>
        event Func<AnswerAppHostedMediaOutcomeEvent, Task> OnAnswerSucceeded;

        /// <summary>
        /// Event raised when the bot gets the outcome of AnswerAppHostedMedia action but the call failed.
        /// </summary>
        event Func<AnswerAppHostedMediaOutcomeEvent, Task> OnAnswerFailed;

        /// <summary>
        /// Event raised when the bot requests to join a call
        /// </summary>
        event Func<RealTimeMediaJoinCallEvent, Task> OnJoinCallRequested;

        /// <summary>
        /// Event raised when the bot gets the outcome of JoinCallAppHostedMedia action and the call is established.
        /// </summary>
        event Func<JoinCallAppHostedMediaOutcomeEvent, Task> OnJoinCallSucceeded;

        /// <summary>
        /// Event raised when the bot gets the outcome of JoinCallAppHostedMedia action but the call failed.
        /// </summary>
        event Func<JoinCallAppHostedMediaOutcomeEvent, Task> OnJoinCallFailed;

        /// <summary>
        /// Event raised when the bot gets call state change notification
        /// </summary>
        event Func<CallStateChangeNotification, Task> OnCallStateChangeNotification;

        /// <summary>
        /// Event raised when bot gets roster update notification
        /// </summary>
        event Func<RosterUpdateNotification, Task> OnRosterUpdateNotification;

        /// <summary>
        /// Event raised when bot needs to cleanup the call
        /// </summary>
        event Func<Task> OnCallCleanup;

        /// <summary>
        /// Create a media session for this call.
        /// </summary>
        IRealTimeMediaSession CreateMediaSession(params NotificationType[] subscriptions);

        /// <summary>
        /// The current media session for this call.
        /// </summary>
        IReadOnlyMediaSession CurrentMediaSession { get; }

        /// <summary>
        /// Subscribe to video or video-based screen sharing channel
        /// </summary>
        /// <param name="videoSubscription">Details regarding the subscription like the source to subscribe, socket on which subscription needs to be done, etc</param>
        /// <returns></returns>
        Task Subscribe(VideoSubscription videoSubscription);

        /// <summary>
        /// Terminate the call
        /// </summary>
        /// <returns></returns>
        Task EndCall();
    }

    internal interface IInternalRealTimeMediaCallService : IRealTimeMediaCallService
    {
        string CallLegId { get; }

        Task LocalCleanup();

        Task<Workflow> HandleIncomingCall(Conversation conversation);

        Task<Workflow> HandleJoinCall(JoinCallAppHostedMedia call);

        Task<string> ProcessConversationResult(ConversationResult conversationResult);

        Task ProcessNotificationResult(NotificationBase notification);
    }
}
