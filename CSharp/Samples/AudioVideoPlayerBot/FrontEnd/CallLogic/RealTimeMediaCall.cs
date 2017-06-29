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

namespace FrontEnd.Call
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
        public readonly string CallId;

        /// <summary>
        /// CorrelationId that needs to be set in the media platform for correlating logs across services
        /// </summary>
        public readonly string CorrelationId;
        
        public RealTimeMediaCall(IRealTimeMediaCallService callService)
        {
            if (callService == null)
                throw new ArgumentNullException(nameof(callService));

            CallService = callService;
            CorrelationId = callService.CorrelationId;
            CallId = CorrelationId + ":" + Guid.NewGuid().ToString();

            //Register for the events 
            CallService.OnIncomingCallReceived += OnIncomingCallReceived;
            CallService.OnAnswerAppHostedMediaCompleted += OnAnswerAppHostedMediaCompleted;
            CallService.OnCallStateChangeNotification += OnCallStateChangeNotification;
            CallService.OnCallCleanup += OnCallCleanup;
        }

        private Task OnIncomingCallReceived(RealTimeMediaIncomingCallEvent incomingCallEvent)
        {
            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"[{CallId}] OnIncomingCallReceived");

            MediaSession = new MediaSession(CallId, CorrelationId, this);
            incomingCallEvent.RealTimeMediaWorkflow.Actions = new ActionBase[]
                {
                    new AnswerAppHostedMedia
                    {
                        MediaConfiguration = MediaSession.MediaConfiguration,
                        OperationId = Guid.NewGuid().ToString()
                    }
                };

            incomingCallEvent.RealTimeMediaWorkflow.NotificationSubscriptions = new NotificationType[] { NotificationType.CallStateChange};

            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"[{CallId}] Answering the call");

            return Task.CompletedTask;
        }

        private Task OnAnswerAppHostedMediaCompleted(AnswerAppHostedMediaOutcomeEvent answerAppHostedMediaOutcomeEvent)
        {
            try
            {
                Log.Info(new CallerInfo(), LogContext.FrontEnd, $"[{CallId}] OnAnswerAppHostedMediaCompleted");
                AnswerAppHostedMediaOutcome answerAppHostedMediaOutcome = answerAppHostedMediaOutcomeEvent.AnswerAppHostedMediaOutcome;
                if (answerAppHostedMediaOutcome.Outcome == Outcome.Failure)
                {
                    Log.Info(new CallerInfo(), LogContext.FrontEnd, $"[{CallId}] AnswerAppHostedMedia failed with reason: {answerAppHostedMediaOutcome.FailureReason}");
                    //cleanup internal resources
                    MediaSession.Dispose();
                }
                else
                {
                    answerAppHostedMediaOutcomeEvent.RealTimeMediaWorkflow.NotificationSubscriptions = new NotificationType[] { NotificationType.CallStateChange};
                }
                return Task.CompletedTask;
            } catch (Exception ex)
            {
                Log.Info(new CallerInfo(), LogContext.FrontEnd, $"[{CallId}] threw {ex.ToString()}");
                throw;
            }
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