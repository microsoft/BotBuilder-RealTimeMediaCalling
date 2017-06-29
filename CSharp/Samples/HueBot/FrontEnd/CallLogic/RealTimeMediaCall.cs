/**************************************************
*                                                                                    *
*   © Microsoft Corporation. All rights reserved.  *
*                                                                                    *
**************************************************/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FrontEnd.Logging;
using Microsoft.Bot.Builder.Calling.ObjectModel.Contracts;
using Microsoft.Bot.Builder.Calling.ObjectModel.Misc;
using Microsoft.Bot.Builder.RealTimeMediaCalling;
using Microsoft.Bot.Builder.RealTimeMediaCalling.Events;
using Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts;
using Microsoft.Bot.Connector;
using FrontEnd.Http;

namespace FrontEnd.CallLogic
{
    /// <summary>
    /// This class does all the signaling needed to handle a call.
    /// </summary>
    internal class RealTimeMediaCall : IRealTimeMediaCall
    {
        /// <summary>
        /// MediaSession that handles media related details
        /// </summary>
        public MediaSession MediaSession { get; private set; }
        
        /// <summary>
        /// Service that helps parse incoming requests and provide corresponding events
        /// </summary>
        public IRealTimeMediaCallService CallService { get; private set; }

        /// <summary>
        /// Id generated locally that is unique to each RealTimeMediaCall
        /// </summary>
        public string CallId => CallService.CurrentMediaSession.Id;

        /// <summary>
        /// CorrelationId that needs to be set in the media platform for correlating logs across services
        /// </summary>
        public string CorrelationId => CallService.CorrelationId;

        public RealTimeMediaCall(IRealTimeMediaCallService callService)
        {
            if (callService == null)
                throw new ArgumentNullException(nameof(callService));

            CallService = callService;

            //Register for the events 
            CallService.OnIncomingCallReceived += OnIncomingCallReceived;
            CallService.OnAnswerSucceeded += OnAnswerSucceeded;
            CallService.OnAnswerFailed += OnAnswerFailed;
            CallService.OnCallStateChangeNotification += OnCallStateChangeNotification;
            CallService.OnCallCleanup += OnCallCleanup;
        }

        private Task OnIncomingCallReceived(RealTimeMediaIncomingCallEvent incomingCallEvent)
        {
            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"[{CallId}] OnIncomingCallReceived");

            var mediaSession = CallService.CreateMediaSession(NotificationType.CallStateChange);
            MediaSession = new MediaSession(mediaSession);
            incomingCallEvent.Answer(mediaSession);

            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"[{CallId}] Answering the call");

            return Task.CompletedTask;
        }

        private Task OnAnswerSucceeded()
        {
            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"[{CallId}] OnAnswerSucceded");
            return Task.CompletedTask;
        }

        private Task OnAnswerFailed(AnswerAppHostedMediaOutcomeEvent answerFailedEvent)
        {
            var outcome = answerFailedEvent.AnswerAppHostedMediaOutcome;
            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"[{CallId}] OnAnswerFailed failed with reason: {outcome.FailureReason}");
            //cleanup internal resources
            MediaSession.Dispose();
            return Task.CompletedTask;
        }

        /// <summary>
        /// This gets called when the user hangs up the call to the bot
        /// </summary>
        /// <param name="callStateChangeNotification"></param>
        /// <returns></returns>
        private Task OnCallStateChangeNotification(CallStateChangeNotification callStateChangeNotification)
        {
            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"[{CallId}] Received CallStateChangeNotification with AudioVideoCallStateType={callStateChangeNotification.CurrentState.ToString()}");

            if (callStateChangeNotification.CurrentState == CallState.Terminated)
            {
                MediaSession.Dispose();
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// When the IRealTimeMediaCallService detects an error and cleans up the call locally
        /// </summary>
        /// <returns></returns>
        private Task OnCallCleanup()
        {
            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"[{CallId}] Received OnCallCleanup");
            if (MediaSession != null)
            {
                MediaSession.Dispose();
            }

            return Task.CompletedTask;
        }
    }
}