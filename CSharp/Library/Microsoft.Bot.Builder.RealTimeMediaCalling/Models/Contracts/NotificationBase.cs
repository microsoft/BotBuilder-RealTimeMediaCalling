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
using Microsoft.Bot.Builder.Calling.ObjectModel.Misc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts
{
    /// <summary>
    /// This base class defines a subset of properties which define a notification.
    /// CallStateNotification and RosterUpdates are examples of Notifications.
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public abstract class NotificationBase
    {
        /// <summary>
        /// Unique identifier for call leg.
        /// </summary>
        [JsonProperty(Required = Required.Always, Order = -2)]
        public string Id { get; set; }

        /// <summary>
        /// Type of Notification
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public NotificationType Type { get; set; }

        [JsonExtensionData]
        private IDictionary<string, JToken> _additionalData;

        /// <summary>
        /// Extension data for the fields that are not part of schema.
        /// </summary>
        [JsonIgnore]
        public IDictionary<string, JToken> AdditionalData
        {
            get
            {
                if (_additionalData == null)
                {
                    _additionalData = new Dictionary<string, JToken>();
                }

                return _additionalData;
            }
            set
            {
                _additionalData = value;
            }
        }

        /// <summary>
        /// Validation method to verify notification is well formed.
        /// </summary>
        public virtual void Validate()
        {
            Utils.AssertArgument(!String.IsNullOrWhiteSpace(this.Id), "Id cannot be null or empty");
        }
    }
}
