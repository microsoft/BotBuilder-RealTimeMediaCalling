using Microsoft.Skype.Bots.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling
{
    class RealTimeMediaSession : IInternalRealTimeMediaSession
    {
        private class RealTimeAudioSocket : IRealTimeAudioSocket, IDisposable
        {
            public AudioSocket Socket { get; }

            public event EventHandler<AudioMediaReceivedEventArgs> AudioMediaReceived;

            public event EventHandler<AudioSendStatusChangedEventArgs> AudioSendStatusChanged;

            public event EventHandler<DominantSpeakerChangedEventArgs> DominantSpeakerChanged;

            public event EventHandler<ToneReceivedEventArgs> ToneReceived;

            public RealTimeAudioSocket(AudioSocketSettings settings)
            {
                throw new NotImplementedException();
                Socket = new AudioSocket(settings);
            }

            public void Send(AudioMediaBuffer buffer)
            {
                Socket.Send(buffer);
            }

            public void Dispose()
            {
                Socket?.Dispose();
            }
        }

        private class RealTimeVideoSocket : IRealTimeVideoSocket, IDisposable
        {
            public VideoSocket Socket { get; }

            public int SocketId => Socket.SocketId;

            public event EventHandler<VideoMediaReceivedEventArgs> VideoMediaReceived;

            public event EventHandler<VideoSendStatusChangedEventArgs> VideoSendStatusChanged;

            public RealTimeVideoSocket(VideoSocketSettings settings)
            {
                throw new NotImplementedException();
                Socket = new VideoSocket(settings);
            }

            public void Send(VideoMediaBuffer buffer)
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
                Socket?.Dispose();
            }
        }

        private RealTimeAudioSocket _audioSocket;
        private readonly List<RealTimeVideoSocket> _videoSockets;
        private RealTimeVideoSocket _vbssSocket;

        public string Id { get; private set; }

        private string _correlationId;
        public string CorrelationId
        {
            get => _correlationId;
            set
            {
                this.Id = $"{value}:{Guid.NewGuid()}";
                _correlationId = value;
            }
        }

        public IRealTimeAudioSocket AudioSocket => _audioSocket;

        public IReadOnlyList<IRealTimeVideoSocket> VideoSockets => _videoSockets;

        public IRealTimeVideoSocket VbssSocket => _vbssSocket;

        public RealTimeMediaSession()
        {
            _videoSockets = new List<RealTimeVideoSocket>();
        }

        public IRealTimeAudioSocket AddAudioSocket(AudioSocketSettings settings)
        {
            if (null == settings)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            settings.CallId = "abc";

            if (null != _audioSocket)
            {
                throw new InvalidOperationException("An audio socket has already been added");
            }

            _audioSocket = new RealTimeAudioSocket(settings);
            return _audioSocket;
        }

        public IRealTimeVideoSocket AddVideoSocket(VideoSocketSettings settings)
        {
            if (null == settings)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var videoSocket = new RealTimeVideoSocket(settings);
            _videoSockets.Add(videoSocket);
            return videoSocket;
        }

        public IRealTimeVideoSocket AddVbssSocket(VideoSocketSettings settings)
        {
            if (null == settings)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (null != _vbssSocket)
            {
                throw new InvalidOperationException("An audio socket has already been added");
            }

            _vbssSocket = new RealTimeVideoSocket(settings);
            return _vbssSocket;
        }

        public JObject MediaConfiguration
        {
            get
            {
                var audioSocket = _audioSocket?.Socket;
                IList<IVideoSocket> videoSockets = null;
                if (_videoSockets?.Count > 0)
                {
                    videoSockets = _videoSockets.Select(c => (IVideoSocket)c.Socket).ToList();
                }
                var vbssSocket = _vbssSocket?.Socket;
                return MediaPlatform.CreateMediaConfiguration(audioSocket, videoSockets, vbssSocket);
            }
        }
    }
}
