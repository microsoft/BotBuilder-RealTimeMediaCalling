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
using FrontEnd.Http;
using FrontEnd.Logging;
using Microsoft.Bot.Builder.Calling.ObjectModel.Contracts;
using Microsoft.Bot.Builder.Calling.ObjectModel.Misc;
using Microsoft.Bot.Builder.RealTimeMediaCalling;
using Microsoft.Bot.Builder.RealTimeMediaCalling.Events;
using Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts;

namespace FrontEnd.CallLogic
{
    /// <summary>
    /// This class does all the signaling needed to handle a call.
    /// </summary>
    internal class RealTimeMediaCall : IRealTimeMediaCall
    {
        /// <summary>
        /// Container for the current active calls.
        /// </summary>
        private static ConcurrentDictionary<string, RealTimeMediaCall> ActiveMediaCalls;

        /// <summary>
        /// The MediaStreamId of the participant to which the video channel is currently subscribed to
        /// </summary>
        private RosterParticipant _subscribedToParticipant;

        /// <summary>
        /// Roster to get the video msi of the dominant speaker
        /// </summary>
        private IEnumerable<RosterParticipant> _participants;

        /// <summary>
        /// MediaSession that handles media related details
        /// </summary>
        public MediaSession MediaSession { get; private set; }

        /// <summary>
        /// ThreadId corresponding to this call to tie with chat messages for this conversation
        /// </summary>
        public string ThreadId;

        /// <summary>
        /// Service that helps parse incoming requests and provide corresponding events
        /// </summary>
        public IRealTimeMediaCallService CallService { get; private set; }

        private readonly string _callGuid = Guid.NewGuid().ToString();
        private string _callId;

        /// <summary>
        /// Id generated locally that is unique to each RealTimeMediaCall
        /// </summary>
        public string CallId
        {
            get
            {
                if (null == _callId)
                {
                    _callId = $"{CallService.CorrelationId}:{_callGuid}";
                }
                return _callId;
            }
        }

        /// <summary>
        /// CorrelationId that needs to be set in the media platform for correlating logs across services
        /// </summary>
        public string CorrelationId => CallService.CorrelationId;

        static RealTimeMediaCall()
        {
            ActiveMediaCalls = new ConcurrentDictionary<string, RealTimeMediaCall>();
        }

        public RealTimeMediaCall(IRealTimeMediaCallService callService)
        {
            if (callService == null)
                throw new ArgumentNullException(nameof(callService));

            CallService = callService;

            //Register for the events 
            CallService.OnIncomingCallReceived += OnIncomingCallReceived;
            CallService.OnAnswerAppHostedMediaCompleted += OnAnswerAppHostedMediaCompleted;
            CallService.OnCallStateChangeNotification += OnCallStateChangeNotification;
            CallService.OnRosterUpdateNotification += OnRosterUpdateNotification;
            CallService.OnCallCleanup += OnCallCleanup;
        }

        private Task OnIncomingCallReceived(RealTimeMediaIncomingCallEvent incomingCallEvent)
        {
            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"[{CallId}] OnIncomingCallReceived");

            //handles the media for this call like creating sockets, registering for events on the socket and sending/receiving media
            MediaSession = new MediaSession(CallId, CorrelationId, this);

            //subscribe for roster changes and call state changes
            incomingCallEvent.Answer(MediaSession.MediaConfiguration, null, NotificationType.RosterUpdate, NotificationType.CallStateChange);
            ThreadId = incomingCallEvent.IncomingCall.ThreadId;
            
            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"[{CallId}] Answering the call");

            ActiveMediaCalls.AddOrUpdate(CallId, this, (callId, oldcall) => this);

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
                    answerAppHostedMediaOutcomeEvent.RealTimeMediaWorkflow.NotificationSubscriptions = new NotificationType[] { NotificationType.CallStateChange, NotificationType.RosterUpdate };
                }
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Log.Info(new CallerInfo(), LogContext.FrontEnd, $"[{CallId}] threw {ex.ToString()}");
                throw;
            }
        }

        private async Task OnRosterUpdateNotification(RosterUpdateNotification rosterUpdateNotification)
        {
            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"[{CallId}] OnRosterUpdateNotification");
            _participants = rosterUpdateNotification.Participants;

            uint prevSubscribedMsi = (_subscribedToParticipant == null) ? MediaSession.DominantSpeaker_None 
                                                                         : Convert.ToUInt32(_subscribedToParticipant.MediaStreamId);
            await Subscribe(prevSubscribedMsi, false).ConfigureAwait(false);
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
                //cleanup the media session that disposes sockets, etc
                MediaSession.Dispose();
                RealTimeMediaCall temp;
                ActiveMediaCalls.TryRemove(CallId, out temp);
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

            RealTimeMediaCall temp;
            ActiveMediaCalls.TryRemove(CallId, out temp);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Subscribe to the video stream of a participant. This function is called when dominant speaker notification is received and when roster changes
        /// When invoked on dominant speaker change, look if the participant is sharing their video. If yes then subscribe else choose the first participant in the list sharing their video
        /// When invoked on roster change, verify if the previously subscribed-to participant is still in the roster and sending video
        /// </summary>
        /// <param name="msi">Msi of dominant speaker or previously subscribed to msi depending on where it is invoked</param>
        /// <param name="msiOfDominantSpeaker">Gives more detail on the above msi.. Whether it is of dominant speaker or previously subscribed to video msi</param>
        /// <returns></returns>
        internal async Task Subscribe(uint msi, bool msiOfDominantSpeaker)
        {
            try
            {
                RosterParticipant participant;
                Log.Info(new CallerInfo(), LogContext.FrontEnd, $"[{CallId}] Received subscribe request for Msi {msi} msiOfDominantSpeaker {msiOfDominantSpeaker}");
                if(msiOfDominantSpeaker)
                {
                    participant = GetParticipantWithDominantSpeakerMsi(msi);
                }
                else
                {
                    participant = GetParticipantForRosterChange(msi);
                }
                 
                if (participant == null)
                {
                    _subscribedToParticipant = null;
                    return;
                }

                //if we have already subscribed earlier, skip the subscription
                if (_subscribedToParticipant != null && _subscribedToParticipant.MediaStreamId == participant.MediaStreamId)
                {
                    Log.Info(new CallerInfo(), LogContext.FrontEnd, $"[{CallId}] Already subscribed to {participant.Identity}. So skipping subscription");
                    return;
                }

                Log.Info(new CallerInfo(), LogContext.FrontEnd, $"[{CallId}] Subscribing to {participant.Identity} with msi {participant.MediaStreamId}");

                //Get subscription details
                var videoSubscription = new VideoSubscription
                {
                    ParticipantIdentity = participant.Identity,
                    OperationId = Guid.NewGuid().ToString(),
                    SocketId = 0,                   //index of the VideoSocket in MediaConfiguration which receives the incoming video stream
                    VideoModality = ModalityType.Video,
                    VideoResolution = ResolutionFormat.Hd1080p
                };

                await CallService.Subscribe(videoSubscription).ConfigureAwait(false);
                _subscribedToParticipant = participant;
            }
            catch(Exception ex)
            {
                Log.Error(new CallerInfo(), LogContext.FrontEnd, $"[{CallId}] Subscribe threw exception {ex.ToString()}");
                _subscribedToParticipant = null;
            }
        }

        private RosterParticipant GetParticipantWithDominantSpeakerMsi(uint dominantSpeakerMsi)
        {
            RosterParticipant firstParticipant = null;
            if (_participants == null)
            {
                Log.Warning(new CallerInfo(), LogContext.FrontEnd, $"[{CallId}] Did not receive rosterupdate notification yet");
                return null;
            }

            string dominantSpeakerIdentity = string.Empty;
            foreach (RosterParticipant participant in _participants)
            {
                if (participant.MediaStreamId == dominantSpeakerMsi.ToString())
                {                    
                    //identify
                    if (participant.MediaType == ModalityType.Audio)
                    {
                        dominantSpeakerIdentity = participant.Identity;
                        continue;
                    }
                }

                if (participant.MediaType != ModalityType.Video
                    || !(participant.MediaStreamDirection == "sendonly" || participant.MediaStreamDirection == "sendrecv")
                   )
                {
                    continue;
                }

                //cache the first participant.. just incase dominant speaker is not sending video
                if (firstParticipant == null)
                {
                    firstParticipant = participant;
                    if (dominantSpeakerMsi == MediaSession.DominantSpeaker_None)
                    {
                        return firstParticipant;
                    }
                }

                //The dominant speaker is sending video.. 
                if (participant.Identity == dominantSpeakerIdentity)
                {
                    return participant;
                }
            }

            //If dominant speaker is not sending video or if dominant speaker has exited the conference, choose the first participant sending video
            return firstParticipant;
        }

        private RosterParticipant GetParticipantForRosterChange(uint msi)
        {
            RosterParticipant firstParticipant = null;
            if (_participants == null)
            {
                Log.Warning(new CallerInfo(), LogContext.FrontEnd, $"[{CallId}] Did not receive rosterupdate notification yet");
                return null;
            }

            string dominantSpeakerIdentity = string.Empty;
            foreach (RosterParticipant participant in _participants)
            {
                if (participant.MediaStreamId == msi.ToString())
                {
                    if (participant.MediaType == ModalityType.Video && (participant.MediaStreamDirection == "sendonly" || participant.MediaStreamDirection == "sendrecv"))
                    {
                        return participant;
                    }
                }

                if (participant.MediaType != ModalityType.Video
                    || !(participant.MediaStreamDirection == "sendonly" || participant.MediaStreamDirection == "sendrecv")
                   )
                {
                    continue;
                }

                if (firstParticipant == null)
                {
                    firstParticipant = participant;
                }
            }

            //No dominant speaker or dominant speaker is not sending video or if old dominant speaker has exited the conference, choose a new oe
            return firstParticipant;
        }

        /// <summary>
        /// Debug: Get a list of the active mediasessions
        /// </summary>
        /// <returns>List of current call identifiers.</returns>
        public static IList<string> GetActiveRealTimeMediaCalls()
        {
            return ActiveMediaCalls.Values.Select(x => x.CallId).ToList();
        }

        internal static RealTimeMediaCall GetCallForCallId(string callId)
        {
            return ActiveMediaCalls.Values.FirstOrDefault(x => (x.CallId == callId));
         }

        internal static async Task SendUrlForConversationId(string threadId)
        {
            RealTimeMediaCall mediaCall = ActiveMediaCalls.Values.FirstOrDefault(x => (x.ThreadId == threadId));
            if (mediaCall == null)
            {
                Log.Warning(new CallerInfo(), LogContext.FrontEnd, $"No active mediacall for {threadId}");
                return;
            }
            await SendMessageForCall(mediaCall);
        }

        internal static async Task SendMessageForCall(RealTimeMediaCall mediaCall)
        { 
            string url = $"{Service.Instance.Configuration.AzureInstanceBaseUrl}/{HttpRouteConstants.CallSignalingRoutePrefix}/{HttpRouteConstants.Image}";
            url = url.Replace("{callid}", mediaCall.CallId);
            try
            {
                await MessageSender.SendMessage(mediaCall.ThreadId, url);
            }
            catch(Exception ex)
            {
                Log.Error(new CallerInfo(), LogContext.FrontEnd, $"[{mediaCall.CallId}] Exception in sending chat {ex}");
            }
        }
    }
}