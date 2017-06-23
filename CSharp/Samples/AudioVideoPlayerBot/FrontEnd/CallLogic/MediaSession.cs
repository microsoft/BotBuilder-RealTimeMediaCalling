/**************************************************
*                                                                                    *
*   © Microsoft Corporation. All rights reserved.  *
*                                                                                    *
**************************************************/

using FrontEnd.Logging;
using Microsoft.Skype.Bots.Media;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CorrelationId = FrontEnd.Logging.CorrelationId;

namespace FrontEnd.CallLogic
{
    /// <summary>
    /// This class handles media related logic for a call.
    /// </summary>
    internal class MediaSession : IDisposable
    {
        #region Fields

        private readonly TaskCompletionSource<bool> _audioSendStatusActive;
        private readonly TaskCompletionSource<bool> _videoSendStatusActive;
        private long _mediaTick;
        private int _disposed;
        private AudioVideoFramePlayer _audioVideoFramePlayer;
        private const int _startPlayerTimeOut = 4000;
        private List<AudioMediaBuffer> _audioMediaBuffers;
        private List<VideoMediaBuffer> _videoMediaBuffers;
        private readonly ManualResetEvent _startVideoPlayerCompleted;

        /// <summary>
        /// Unique correlationID of this particular call.
        /// </summary>
        private readonly string _correlationId;

        /// <summary>
        /// The audio socket created for this particular call.
        /// </summary>
        private readonly AudioSocket _audioSocket;

        /// <summary>
        /// The video socket created for this particular call.
        /// </summary>
        private readonly VideoSocket _videoSocket;

        #endregion

        #region Properties
        /// <summary>
        /// The Id of this call.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// The opaque media configuration object sent back to the Skype platform when answering a call.
        /// </summary>
        public JObject MediaConfiguration { get; private set; }

        /// <summary>
        /// Implementation of IRealTimeMediaCall that handles incoming and outgoing requests
        /// </summary>
        public readonly RealTimeMediaCall RealTimeMediaCall;
        #endregion

        #region Public Methods

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id"></param>
        /// <param name="correlationId"></param>
        /// <param name="call"></param>
        public MediaSession(string id, string correlationId, RealTimeMediaCall call)
        {
            _correlationId = CorrelationId.GetCurrentId();
            this.Id = id;
            RealTimeMediaCall = call;
            _audioSendStatusActive = new TaskCompletionSource<bool>();
            _videoSendStatusActive = new TaskCompletionSource<bool>();
            _videoMediaBuffers = new List<VideoMediaBuffer>();
            _audioMediaBuffers = new List<AudioMediaBuffer>();
            _startVideoPlayerCompleted = new ManualResetEvent(false);

            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"[{this.Id}]: Call created");

            try
            {
                _audioSocket = new AudioSocket(new AudioSocketSettings
                {
                    StreamDirections = StreamDirection.Sendrecv,
                    SupportedAudioFormat = AudioFormat.Pcm16K, // audio format is currently fixed at PCM 16 KHz.
                    CallId = correlationId
                });

                Log.Info(new CallerInfo(), LogContext.FrontEnd, $"[{this.Id}]:Created AudioSocket");

                _videoSocket = new VideoSocket(new VideoSocketSettings
                {
                    StreamDirections = StreamDirection.Sendrecv,
                    ReceiveColorFormat = VideoColorFormat.NV12,

                    //We loop back the video in this sample. The MediaPlatform always sends only NV12 frames. So include only NV12 video in supportedSendVideoFormats
                    SupportedSendVideoFormats = new List<VideoFormat>() {
                        VideoFormat.NV12_1280x720_30Fps,
                        VideoFormat.NV12_270x480_15Fps,
                        VideoFormat.NV12_320x180_15Fps,
                        VideoFormat.NV12_360x640_15Fps,
                        VideoFormat.NV12_424x240_15Fps,
                        VideoFormat.NV12_480x270_15Fps,
                        VideoFormat.NV12_480x848_30Fps,
                        VideoFormat.NV12_640x360_15Fps,
                        VideoFormat.NV12_720x1280_30Fps,
                        VideoFormat.NV12_848x480_30Fps,
                        VideoFormat.NV12_960x540_30Fps,
                        VideoFormat.NV12_424x240_15Fps
                    },
                    CallId = correlationId
                });

                Log.Info(new CallerInfo(), LogContext.FrontEnd, $"[{this.Id}]: Created VideoSocket");

                //audio socket events
                _audioSocket.AudioSendStatusChanged += OnAudioSendStatusChanged;

                //Video socket events
                _videoSocket.VideoSendStatusChanged += OnVideoSendStatusChanged;

                MediaConfiguration = MediaPlatform.CreateMediaConfiguration(_audioSocket, _videoSocket);

                Log.Info(new CallerInfo(), LogContext.FrontEnd, $"[{this.Id}]: MediaConfiguration={MediaConfiguration.ToString(Formatting.Indented)}");
                StartAudioVideoFramePlayer().ForgetAndLogException("Failed to start the player");
            }
            catch (Exception ex)
            {
                Log.Error(new CallerInfo(), LogContext.FrontEnd, "Error in MediaSession creation" + ex.ToString());
                Dispose();
                throw;
            }
        }

        /// <summary>
        /// Unsubscribes all audio/video send/receive-related events, cancels tasks and disposes sockets
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1)
            {
                return;
            }

            try
            {
                Log.Info(new CallerInfo(), LogContext.FrontEnd, "Disposing Call with Id={0}.", Id);

                if (_audioVideoFramePlayer != null)
                {
                    _audioVideoFramePlayer.LowOnFrames -= OnLowOnFrames;
                    Log.Verbose(new CallerInfo(), LogContext.FrontEnd, "shutting down the player LocalId={0}.", Id);
                    _audioVideoFramePlayer.ShutdownAsync().GetAwaiter().GetResult();
                    Log.Info(new CallerInfo(), LogContext.FrontEnd, "player shutdown LocalId={0}.", Id);
                    if (!_startVideoPlayerCompleted.WaitOne(_startPlayerTimeOut))
                    {
                        Log.Error(new CallerInfo(), LogContext.FrontEnd, "StartFramePlayerOperation timed out, LocalId={0}.", Id);
                    }

                    Log.Info(new CallerInfo(), LogContext.FrontEnd, "StartFramePlayerOperation Completed, LocalId={0}.", Id);
                }
                if (_audioSocket != null)
                {
                    _audioSocket.AudioSendStatusChanged -= OnAudioSendStatusChanged;
                    _audioSocket.Dispose();
                }

                if (_videoSocket != null)
                {
                    _videoSocket.VideoSendStatusChanged -= OnVideoSendStatusChanged;
                    _videoSocket.Dispose();
                }

                // make sure all the audio and video buffers are disposed, it can happen that,
                // the buffers were not enqueued but the call was disposed if the caller hangs up quickly
                foreach (var audioMediaBuffer in _audioMediaBuffers)
                {
                    audioMediaBuffer.Dispose();
                }

                Log.Info(new CallerInfo(), LogContext.FrontEnd, "disposed audioMediaBUffers Id={0}.", Id);
                foreach (var videoMediaBuffer in _videoMediaBuffers)
                {
                    videoMediaBuffer.Dispose();
                }

                Log.Info(new CallerInfo(), LogContext.FrontEnd, "disposed videoMediaBuffers Id={0}.", Id);
                _audioMediaBuffers.Clear();
                _videoMediaBuffers.Clear();
            }
            catch (Exception ex)
            {
                Log.Warning(new CallerInfo(), LogContext.FrontEnd, "Ignoring exception in dispose" + ex);
            }
        }
        #endregion

        #region Audio    
        /// <summary>
        /// Callback for informational updates from the media plaform about audio status changes.
        /// Once the status becomes active, audio can be loopbacked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAudioSendStatusChanged(object sender, AudioSendStatusChangedEventArgs e)
        {
            CorrelationId.SetCurrentId(_correlationId);
            Log.Info(
                new CallerInfo(),
                LogContext.Media,
                "[AudioSendStatusChangedEventArgs(MediaSendStatus={0})]",
                e.MediaSendStatus);

            if (e.MediaSendStatus == MediaSendStatus.Active)
            {
                _audioSendStatusActive.SetResult(true);
            }
        }

        #endregion

        #region Video

        /// <summary>
        /// Callback for informational updates from the media plaform about video status changes. 
        /// Once the Status becomes active, then video can be sent.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnVideoSendStatusChanged(object sender, VideoSendStatusChangedEventArgs e)
        {
            CorrelationId.SetCurrentId(_correlationId);

            Log.Info(new CallerInfo(), LogContext.Media, "OnVideoSendStatusChanged start");

            Log.Info(
                new CallerInfo(),
                LogContext.Media,
                "[VideoSendStatusChangedEventArgs(MediaSendStatus=<{0}>;PreferredVideoSourceFormat=<{1}>]",
                e.MediaSendStatus,
                e.PreferredVideoSourceFormat.VideoColorFormat);

            if (e.MediaSendStatus == MediaSendStatus.Active)
            {
                _videoSendStatusActive.SetResult(true);
            }
        }

        #endregion

        #region AudioVideoFramePlayer

        private void OnLowOnFrames(object sender, LowOnFramesEventArgs e)
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 1) == 1)
            {
                return;
            }

            Log.Info(
                new CallerInfo(),
                LogContext.Media,
                "Low in frames event raised]");
            _videoMediaBuffers = Utilities.CreateVideoMediaBuffers(_mediaTick);
            _audioMediaBuffers = Utilities.CreateAudioMediaBuffers(_mediaTick);
            _mediaTick = Math.Max(_audioMediaBuffers.Last().Timestamp, _videoMediaBuffers.Last().Timestamp);
            _audioVideoFramePlayer.EnqueueBuffersAsync(_audioMediaBuffers, _videoMediaBuffers).ForgetAndLogException();
            Log.Info(
                new CallerInfo(),
                LogContext.Media,
                "enqueued more frames in frames event raised");
        }

        private async Task StartAudioVideoFramePlayer()
        {
            try
            {
                await Task.WhenAll(_audioSendStatusActive.Task, _videoSendStatusActive.Task);
                Log.Info(
                    new CallerInfo(),
                    LogContext.Media,
                    "Send status active for audio and video Creating the audio video player");

                _audioVideoFramePlayer = new AudioVideoFramePlayer(_audioSocket, _videoSocket,
                    new AudioVideoFramePlayerSettings(new AudioSettings(20), new VideoSettings(), 1000));

                Log.Info(
                    new CallerInfo(),
                    LogContext.Media,
                    "created the audio video player");

                _audioVideoFramePlayer.LowOnFrames += OnLowOnFrames;
                var currentTick = DateTime.Now.Ticks;
                _videoMediaBuffers = Utilities.CreateVideoMediaBuffers(currentTick);
                _audioMediaBuffers = Utilities.CreateAudioMediaBuffers(currentTick);
                //update the tick for next iteration
                _mediaTick = Math.Max(_audioMediaBuffers.Last().Timestamp, _videoMediaBuffers.Last().Timestamp);
                await _audioVideoFramePlayer.EnqueueBuffersAsync(_audioMediaBuffers, _videoMediaBuffers);
            }
            finally
            {
                _startVideoPlayerCompleted.Set();
            }
        }

        #endregion AudioVideoFramePlayer
    }
}
