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

using Microsoft.Bot.Builder.Calling.Events;
using Microsoft.Bot.Builder.Calling.ObjectModel.Contracts;
using Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts;
using Microsoft.Skype.Bots.Media;
using System;
using System.Collections.Generic;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling.Events
{
    /// <summary>
    /// EventArg for the OnIncomingCallReceived event raised on <see cref="IRealTimeMediaCallService"/>.
    /// </summary>
    public class RealTimeMediaIncomingCallEvent : IncomingCallEvent
    {
        /// <summary>
        /// Workflow associated with the event
        /// </summary>
        private RealTimeMediaWorkflow RealTimeMediaWorkflow => 
            ResultingWorkflow as RealTimeMediaWorkflow;

        internal IReadOnlyMediaSession MediaSession { get; private set; }

        /// <summary>
        /// EventArg for the RealTimeMediaIncomingCallEvent event raised on <see cref="IRealTimeMediaCallService"/>.
        /// </summary>
        /// <param name="conversation">Conversation for the incoming call</param>
        /// <param name="resultingWorkflow">Workflow to be returned on completion</param>
        public RealTimeMediaIncomingCallEvent(Conversation conversation, RealTimeMediaWorkflow resultingWorkflow)
            : base(conversation, resultingWorkflow)
        {
        }

        public void Answer(IReadOnlyMediaSession mediaSession)
        {
            IList<IVideoSocket> videoSockets = mediaSession.VideoSockets?.Count > 0 
                ? new List<IVideoSocket>(mediaSession.VideoSockets) 
                : null;

            var mediaConfiguration = MediaPlatform.CreateMediaConfiguration(
                mediaSession.AudioSocket,
                videoSockets,
                mediaSession.VbssSocket);

            this.RealTimeMediaWorkflow.Actions = new ActionBase[]
            {
                new AnswerAppHostedMedia
                {
                    MediaConfiguration = mediaConfiguration,
                    OperationId = Guid.NewGuid().ToString()
                }
            };

            this.MediaSession = mediaSession;
            this.RealTimeMediaWorkflow.NotificationSubscriptions = mediaSession.Subscriptions;
        }

        public void Reject()
        {
            this.RealTimeMediaWorkflow.Actions = new ActionBase[]
            {
                new Reject()
                {
                    OperationId = Guid.NewGuid().ToString()
                }
            };
        }

        public void Transfer()
        {
            this.RealTimeMediaWorkflow.Actions = new ActionBase[]
            {
                new Reject()
                {
                    OperationId = Guid.NewGuid().ToString()
                }
            };
        }
    }
}
