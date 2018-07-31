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
using System.Linq;
using Microsoft.Bot.Builder.Calling.ObjectModel.Contracts;
using Microsoft.Bot.Builder.Calling.ObjectModel.Misc;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts
{
    /// <summary>
    /// This class contains the workflow the customer sent for the OnInComingCall POST or any subsequent POST to their callback url.
    /// Basically this workflow defines the set of actions, the customer wants us to perform and then callback to them.
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class RealTimeMediaWorkflow : Workflow
    {        
        /// <summary>
        /// This element indicates that application wants to receive notification updates. 
        /// Call state notifications are added to this list by default and cannot be unsubscribed to.
        /// Subscriptions to rosterUpdate are only used for multiParty calls.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public IEnumerable<NotificationType> NotificationSubscriptions { get; set; }

        /// <summary>
        /// Validate the Workflow.
        /// </summary>
        /// <param name="expectEmptyActions"></param>
        public override void Validate(bool expectEmptyActions)
        {
            if (expectEmptyActions)
            {
                Utils.AssertArgument(this.Actions == null || this.Actions.Count() == 0, "Actions must either be null or empty collection");
            }
            else
            {
                RealTimeMediaValidActions.Validate(this.Actions);
            }

            if (this.Links != null)
            {
                if (this.Links.Notification != null)
                {
                    // Notification link is optional. Notification link - if specified - must be absolute https uri.
                    Utils.AssertArgument(this.Links.Notification.IsAbsoluteUri, "Notification link must be an absolute uri");
                    Utils.AssertArgument(this.Links.Notification.Scheme == "https", "Notification link must be an secure https uri");
                    Utils.AssertArgument(this.NotificationSubscriptions != null, "Notification subscriptions must be specified if notification link is given");
                }
            
                Utils.AssertArgument(this.Links.Callback != null, "Callback link cannot be specified as null");
                Utils.AssertArgument(this.Links.Callback.IsAbsoluteUri, "Callback link must be an absolute uri");
                Utils.AssertArgument(this.Links.Callback.Scheme == "https", "Callback link must be an secure HTTPS uri");
            }

            ApplicationState.Validate(this.AppState);
        }
    }
}
