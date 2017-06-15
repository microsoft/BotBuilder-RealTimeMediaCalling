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

using Microsoft.Bot.Builder.Calling.ObjectModel.Contracts;
using Microsoft.Bot.Builder.Calling.ObjectModel.Misc;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts
{
    /// <summary>
    /// This class contains the response the customer sent for the notification POST to their callback url.
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class NotificationResponse
    {
        /// <summary>
        /// Callback link to call back the customer on, once we have processed the notification response from customer.
        /// 
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public CallbackLink Links { get; set; }

        /// <summary>
        /// Opaque string to facilitate app developers to pass their custom data in this field. 
        /// This field is echo'd back in the 'result' POST for this 'response'.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string AppState { get; set; }

        /// <summary>
        /// Validate the NotificationResponse
        /// </summary>
        public virtual void Validate()
        {
            if (this.Links != null)
            {
                Utils.AssertArgument(this.Links.Callback != null, "Callback link cannot be specified as null");
                Utils.AssertArgument(this.Links.Callback.IsAbsoluteUri, "Callback link must be an absolute uri");
            }
            ApplicationState.Validate(this.AppState);
        }
    }
}
