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
using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Builder.Calling.ObjectModel.Misc;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts
{
    /// <summary>
    /// This class defines a participant object within a rosterUpdate message
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class RosterParticipant
    {
        /// <summary>
        /// MRI of the participant
        /// </summary>
        [JsonProperty(Required = Required.Always, Order = -2)]
        public string Identity { get; set; }

        /// <summary>
        /// Participant Media Type . ex : audio
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public ModalityType MediaType { get; set; }

        /// <summary>
        /// Direction of media . ex : SendReceive
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string MediaStreamDirection { get; set; }

        /// <summary>
        /// This is the "sourceId" of the mediaStream as represented in the roster internal wire protocol.
        /// This is in actuality an uint but for future simplicity in mind, we are using a string to allow other types.
        /// This field will have a valid unique value for audio, video and vbss modality. 
        /// ContentSharing modality doesn't have a sourceId.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string MediaStreamId { get; set; }

        /// <summary>
        /// Indicates if participant is an Attendee or a Presenter if a content sharing session is ongoing
        /// </summary>
        [JsonProperty(Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ContentSharingRole ContentSharingRole { get; set; }

        /// <summary>
        /// Conversation leg id of this participant in the call.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string LegId { get; set; }

        /// <summary>
        /// Validate the RosterParticipant.
        /// </summary>
        public void Validate()
        {
            Utils.AssertArgument(!String.IsNullOrWhiteSpace(this.Identity), "Identity of participant must be specified");
            Utils.AssertArgument(!String.IsNullOrWhiteSpace(MediaStreamDirection), "MediaStreamDirection must be specified");
        }

        internal static void Validate(IEnumerable<RosterParticipant> rosterParticipants)
        {
            Utils.AssertArgument(((rosterParticipants != null) && (rosterParticipants.Count<RosterParticipant>() > 0)), "Participant list cannot be null or empty");
            foreach (RosterParticipant participant in rosterParticipants)
            {
                Utils.AssertArgument(participant != null, "Participant cannot be null");
                participant.Validate();
            }
        }
    }

    /// <summary>
    /// Represents the Role the participant might be playing in a Content Sharing session
    /// </summary>
    [JsonConverter(typeof(StringEnumConverterWithDefault<ContentSharingRole>))]
    public enum ContentSharingRole
    {
        /// <summary>
        /// The participant is not in any content sharing session
        /// </summary>
        None,

        /// <summary>
        /// The participant is the presenter in a content sharing session
        /// </summary>
        Presenter,

        /// <summary>
        /// The participant is an attendee in a content sharing session
        /// </summary>
        Attendee,
    }
}
