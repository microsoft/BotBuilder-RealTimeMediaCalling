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

using System;
using Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling.Events
{
    /// <summary>
    /// EventArg for the OnJoinCallReceived event raised on <see cref="IRealTimeMediaCallService"/>.
    /// </summary>
    public class RealTimeMediaJoinCallEvent
    {
        /// <summary>
        /// Workflow associated with the event
        /// </summary>
        public JoinCallParameters JoinCallParameters { get; }

        internal IReadOnlyMediaSession MediaSession { get; private set; }

        /// <summary>
        /// EventArg for the RealTimeMediaJoinCallEvent event raised on <see cref="IRealTimeMediaCallService"/>.
        /// </summary>
        /// <param name="joinCallParameters">The parameters specifying how to join the call.</param>
        public RealTimeMediaJoinCallEvent(JoinCallParameters joinCallParameters)
        {
            JoinCallParameters = joinCallParameters;
        }

        /// <summary>
        /// Specify the media session for the call being joined.
        /// </summary>
        /// <param name="mediaSession"></param>
        public void JoinCall(IReadOnlyMediaSession mediaSession)
        {
            if (null == mediaSession)
            {
                throw new ArgumentNullException(nameof(mediaSession));
            }

            MediaSession = mediaSession;
        }

        public void Reject()
        {
        }
    }
}
