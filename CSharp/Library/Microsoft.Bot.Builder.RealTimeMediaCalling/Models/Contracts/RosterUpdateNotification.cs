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

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts
{
    /// <summary>
    /// This defines the set of the properties that define a rosterUpdate. 
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class RosterUpdateNotification : NotificationBase
    {
        /// <summary>
        /// List of participants in the conversation
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public IEnumerable<RosterParticipant> Participants { get; set; }

        /// <summary>
        /// Create a notification for roster updates
        /// </summary>
        public RosterUpdateNotification()
        {
            this.Type = NotificationType.RosterUpdate;
        }

        /// <summary>
        /// Validate the notification.
        /// </summary>
        public override void Validate()
        {
            base.Validate();
            RosterParticipant.Validate(this.Participants);
        }
    }
}
