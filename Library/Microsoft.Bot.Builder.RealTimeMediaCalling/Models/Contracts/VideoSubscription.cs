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
using Microsoft.Bot.Builder.Calling.ObjectModel.Contracts;
using Microsoft.Bot.Builder.Calling.ObjectModel.Misc;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts
{
    /// <summary>
    /// This class defines the details needed to subscribe to a participant for a video channel
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class VideoSubscription : ActionBase
    {
        /// <summary>
        /// Sequence ID of video socket. Index from 0-9 that is passed in the MediaConfiguration
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public uint SocketId { get; set; }

        /// <summary>
        /// Identity of the participant whose video is pinned if VideoMode is set to controlled
        /// </summary>
        [JsonProperty(Required = Required.AllowNull)]
        public string ParticipantIdentity { get; set; }

        /// <summary>
        /// Indicates whether the video is from the camera or from screen sharing
        /// Unknown, Video and VideoBasedScreenSharing are supported modalities for this request
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public ModalityType VideoModality { get; set; }

        /// <summary>
        /// Indicates the video resolution format.Default value is "sd360p".
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public ResolutionFormat VideoResolution { get; set; }

        /// <summary>
        /// Create a VideoSubscription action.
        /// </summary>
        public VideoSubscription()
        {
            this.Action = RealTimeMediaValidActions.VideoSubscriptionAction;
        }

        /// <summary>
        /// Validate the action.
        /// </summary>
        public override void Validate()
        {
            base.Validate();
            Utils.AssertArgument(this.Action == RealTimeMediaValidActions.VideoSubscriptionAction, "Action was not VideoSubscription");
            Utils.AssertArgument(this.VideoModality != ModalityType.Audio, "Audio modality is not supported for this operation");
            Utils.AssertArgument(!String.IsNullOrWhiteSpace(this.ParticipantIdentity), "Participant identity cannot be null or empty");
            Utils.AssertArgument(VideoModality != ModalityType.Unknown, "VideoModality cannot be unknown");
        }
    }
}
