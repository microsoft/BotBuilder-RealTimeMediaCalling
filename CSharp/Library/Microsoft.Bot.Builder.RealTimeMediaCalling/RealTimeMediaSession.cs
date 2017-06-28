using System;
using System.Collections.Generic;
using Microsoft.Skype.Bots.Media;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling
{
    class RealTimeMediaSession : IRealTimeMediaSession
    {
        private IAudioSocket _audioSocket;
        private readonly List<IVideoSocket> _videoSockets;
        private IVideoSocket _vbssSocket;

        public string Id { get; }

        public string CorrelationId { get; }

        public IAudioSocket AudioSocket => _audioSocket;

        public IReadOnlyList<IVideoSocket> VideoSockets => _videoSockets;

        public IVideoSocket VbssSocket => _vbssSocket;

        public RealTimeMediaSession(RealTimeMediaCallServiceParameters parameters)
        {
            CorrelationId = parameters.CorrelationId;
            Id = $"{CorrelationId}:{Guid.NewGuid()}";
            _videoSockets = new List<IVideoSocket>();
        }

        public IAudioSocket AddAudioSocket(AudioSocketSettings settings)
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

            _audioSocket = new AudioSocket(settings);
            return _audioSocket;
        }

        public IVideoSocket AddVideoSocket(VideoSocketSettings settings)
        {
            if (null == settings)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var videoSocket = new VideoSocket(settings);
            _videoSockets.Add(videoSocket);
            return videoSocket;
        }

        public IVideoSocket AddVbssSocket(VideoSocketSettings settings)
        {
            if (null == settings)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (null != _vbssSocket)
            {
                throw new InvalidOperationException("An audio socket has already been added");
            }

            _vbssSocket = new VideoSocket(settings);
            return _vbssSocket;
        }
    }
}
