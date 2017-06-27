using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using CorrelationId = FrontEnd.Logging.CorrelationId;
using FrontEnd.Logging;
using Microsoft.Skype.Bots.Media;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace FrontEnd.CallLogic
{
    /// <summary>
    /// This class handles media related logic for a call.
    /// </summary>
    internal class MediaSession : IDisposable
    {
        #region Fields
        /// <summary>
        /// Msi when there is no dominant speaker
        /// </summary>
        public const uint DominantSpeaker_None = DominantSpeakerChangedEventArgs.None;

        /// <summary>
        /// Unique correlationID of this particular call.
        /// </summary>
        private readonly string _correlationId;

        /// <summary>
        /// The audio socket created for this particular call.
        /// </summary>
        private readonly AudioSocket _audioSocket;

        /// <summary>
        /// The video based screen sharing socket created for this particular call.
        /// </summary>
        private readonly VideoSocket _videoSocket;

        /// <summary>
        /// Indicates if the call has been disposed
        /// </summary>
        private int _disposed;

        /// <summary>
        /// The time stamp when video image was updated last
        /// </summary>
        private DateTime _lastVideoCapturedTimeUtc = DateTime.MinValue;
        
        /// <summary>
        /// The time between each video frame capturing 
        /// </summary>
        private TimeSpan VideoCaptureFrequency = TimeSpan.FromMilliseconds(1000);
        
        #endregion

        #region Properties
        /// <summary>
        /// The current Video image        
        /// </summary>
        public Bitmap CurrentVideoImage { get; private set; }
       
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
        /// Create a new instance of the MediaSession.
        /// </summary>        
        public MediaSession(string id, string correlationId, RealTimeMediaCall call)
        {
            _correlationId = correlationId;
            this.Id = id;
            this.RealTimeMediaCall = call;

            Log.Info(new CallerInfo(), LogContext.FrontEnd, $"[{this.Id}]: Call created");

            try
            {
                _audioSocket = new AudioSocket(new AudioSocketSettings
                {
                    StreamDirections = StreamDirection.Recvonly,
                    SupportedAudioFormat = AudioFormat.Pcm16K, // audio format is currently fixed at PCM 16 KHz.
                    CallId = correlationId
                });

                Log.Info(new CallerInfo(), LogContext.FrontEnd, $"[{this.Id}]: Created AudioSocket");

                // video socket
                _videoSocket = new VideoSocket(new VideoSocketSettings
                {
                    StreamDirections = StreamDirection.Recvonly,
                    ReceiveColorFormat = VideoColorFormat.NV12,
                    CallId = correlationId
                });

                Log.Info(new CallerInfo(), LogContext.FrontEnd, $"[{this.Id}]: Created VideoSocket");

                //audio socket events
                _audioSocket.DominantSpeakerChanged += OnDominantSpeakerChanged;

                //Video socket events
                _videoSocket.VideoMediaReceived += OnVideoMediaReceived;

                this.MediaConfiguration = MediaPlatform.CreateMediaConfiguration(_audioSocket, _videoSocket);

                Log.Info(new CallerInfo(), LogContext.FrontEnd, $"[{this.Id}]: MediaConfiguration={MediaConfiguration.ToString(Formatting.Indented)}");
            }
            catch (Exception ex)
            {
                Log.Error(new CallerInfo(), LogContext.FrontEnd, "Error in MediaSession creation" + ex.ToString());
                Dispose();
                throw;
            }
        }

        /// <summary>
        /// Unsubscribes from all audio/video send/receive-related events, cancels tasks and disposes sockets
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
                {
                    Log.Info(new CallerInfo(), LogContext.FrontEnd, $"[{this.Id}]: Disposing Call");

                    if (_audioSocket != null)
                    {
                        _audioSocket.DominantSpeakerChanged -= OnDominantSpeakerChanged;
                        _audioSocket.Dispose();
                    }

                    if (_videoSocket != null)
                    {
                        _videoSocket.VideoMediaReceived -= OnVideoMediaReceived;
                        _videoSocket.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(new CallerInfo(), LogContext.FrontEnd, $"[{this.Id}]: Ignoring exception in dispose {ex}");
            }
        }
        #endregion

        #region Audio
        /// <summary>
        /// Listen for dominant speaker changes in the conference
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDominantSpeakerChanged(object sender, DominantSpeakerChangedEventArgs e)
        {
            CorrelationId.SetCurrentId(_correlationId);
            Log.Info(
                new CallerInfo(),
                LogContext.Media,
                $"[{this.Id}:OnDominantSpeakerChanged(DominantSpeaker={e.CurrentDominantSpeaker})]"
                );

            Task.Run(async () =>
            {
                try
                {
                    await RealTimeMediaCall.Subscribe(e.CurrentDominantSpeaker, true).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.Warning(new CallerInfo(), LogContext.FrontEnd, $"[{this.Id}]: Ignoring exception in subscribe {ex}");
                }
            });
        }    
        #endregion

        #region Video        
        private void OnVideoMediaReceived(object sender, VideoMediaReceivedEventArgs e)
        {
            try
            {                                
                CorrelationId.SetCurrentId(_correlationId);

                if (DateTime.Now > this._lastVideoCapturedTimeUtc + this.VideoCaptureFrequency)
                {
                    // Update the last capture timestamp
                    this._lastVideoCapturedTimeUtc = DateTime.Now;

                    Log.Info(
                        new CallerInfo(),
                        LogContext.Media,
                        "[{0}]: Capturing image: [VideoMediaReceivedEventArgs(Data=<{1}>, Length={2}, Timestamp={3}, Width={4}, Height={5}, ColorFormat={6}, FrameRate={7})]",
                        this.Id,
                        e.Buffer.Data.ToString(),
                        e.Buffer.Length,
                        e.Buffer.Timestamp,
                        e.Buffer.VideoFormat.Width,
                        e.Buffer.VideoFormat.Height,
                        e.Buffer.VideoFormat.VideoColorFormat,
                        e.Buffer.VideoFormat.FrameRate);

                    // Make a copy of the media buffer
                    Stopwatch watch = new Stopwatch();
                    watch.Start();

                    byte[] buffer = new byte[e.Buffer.Length];
                    Marshal.Copy(e.Buffer.Data, buffer, 0, (int)e.Buffer.Length);

                    VideoMediaBuffer videoRenderMediaBuffer = e.Buffer as VideoMediaBuffer;

                    IntPtr ptrToBuffer = Marshal.AllocHGlobal(buffer.Length);
                    Marshal.Copy(buffer, 0, ptrToBuffer, buffer.Length);

                    watch.Stop();
                    Log.Info(new CallerInfo(), LogContext.Media, $"{this.Id} Took {watch.ElapsedMilliseconds} ms to copy buffer");

                    // Transform to bitmap object
                    Bitmap bmpObject = MediaUtils.TransformNV12ToBmpFaster(buffer, e.Buffer.VideoFormat.Width, e.Buffer.VideoFormat.Height);

                    bool sendChatMessage = (CurrentVideoImage == null);
                    Log.Info(new CallerInfo(), LogContext.Media, $"{this.Id} send chat message {sendChatMessage}");

                    // 3. Update the bitmap cache
                    CurrentVideoImage = bmpObject;

                    if (sendChatMessage)
                    {
                        Task.Run(async () => {
                            try
                            {
                                await RealTimeMediaCall.SendMessageForCall(RealTimeMediaCall);
                            }catch(Exception ex)
                            {
                                Log.Info(new CallerInfo(), LogContext.FrontEnd, $"Exception in SendingChatMessage {ex}");
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(new CallerInfo(), LogContext.Media, $"{this.Id} Exception in VideoMediaReceived {ex.ToString()}");
            }

            e.Buffer.Dispose();
        }       
        #endregion
    }
}