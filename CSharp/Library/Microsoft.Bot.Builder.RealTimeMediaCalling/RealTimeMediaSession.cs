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

using Microsoft.Skype.Bots.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling
{
    internal class RealTimeMediaSession : IRealTimeMediaSession
    {
        public string Id { get; }

        public string CorrelationId { get; }

        private List<NotificationType> _subscriptions;
        public NotificationType[] Subscriptions => _subscriptions?.ToArray();

        public IAudioSocket AudioSocket { get; private set; }

        private List<IVideoSocket> _videoSockets;
        public IVideoSocket VideoSocket
        {
            get
            {
                if (null == _videoSockets)
                {
                    return null;
                }

                if (_videoSockets.Count > 1)
                {
                    throw new InvalidOperationException("More than 1 video socket, don't know which one to choose.");
                }

                return _videoSockets[0];
            }
        }

        public IReadOnlyList<IVideoSocket> VideoSockets => _videoSockets;

        public IVideoSocket VbssSocket { get; private set; }

        public RealTimeMediaSession(string correlationId, NotificationType[] subscriptions)
        {
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                throw new ArgumentNullException(nameof(correlationId));
            }

            CorrelationId = correlationId;
            Id = $"{CorrelationId}:{Guid.NewGuid()}";
            _subscriptions = subscriptions == null || subscriptions.Length < 1
                ? null 
                : new List<NotificationType>(subscriptions);
        }

        public IAudioSocket SetAudioSocket(AudioSocketSettings settings)
        {
            if (null == settings)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            settings.CallId = CorrelationId;
            AudioSocket = new AudioSocket(settings);

            return AudioSocket;
        }

        public IVideoSocket SetVideoSocket(VideoSocketSettings settings)
        {
            if (null == settings)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            settings.CallId = CorrelationId;
            var socket = new VideoSocket(settings);
            _videoSockets = new List<IVideoSocket> {socket};

            return socket;
        }

        public IReadOnlyList<IVideoSocket> SetVideoSockets(params VideoSocketSettings[] settings)
        {
            if (null == settings || settings.Length == 0)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _videoSockets = settings.Select(s =>
            {
                s.CallId = CorrelationId;
                return (IVideoSocket)new VideoSocket(s);
            }).ToList();

            return _videoSockets;
        }

        public IVideoSocket SetVbssSocket(VideoSocketSettings settings)
        {
            if (null == settings)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            settings.CallId = CorrelationId;
            VbssSocket = new VideoSocket(settings);

            return VbssSocket;
        }

        public JObject GetMediaConfiguration()
        {
            return MediaPlatform.CreateMediaConfiguration(
                AudioSocket,
                _videoSockets,
                VbssSocket);
        }

        void IDisposable.Dispose()
        {
            AudioSocket.Dispose();
            _videoSockets?.ForEach(a => a.Dispose());
            _videoSockets?.Clear();
            VbssSocket.Dispose();
        }
    }
}
