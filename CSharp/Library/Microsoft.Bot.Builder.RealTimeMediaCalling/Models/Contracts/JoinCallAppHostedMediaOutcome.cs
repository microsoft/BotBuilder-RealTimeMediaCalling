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

using Newtonsoft.Json;
using Microsoft.Bot.Builder.Calling.ObjectModel.Contracts;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts
{
    /// <summary>
    /// The outcome of JoinCallAppHostedMedia operation returned to bot
    /// once join operation completes.
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class JoinCallAppHostedMediaOutcome : OperationOutcomeBase
    {
        /// <summary>
        /// Additional information about outcome. This is enum value from
        /// well defined set of values and bot can programmatically act on the reason.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public JoinCallCompletionReason CompletionReason { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public JoinCallAppHostedMediaOutcome()
        {
            this.Type = RealTimeMediaValidOutcomes.JoinCallAppHostedMediaOutcome;
        }
        /// <summary>
        /// Validation for JoinCallAppHostedMediaOutcome
        /// </summary>
        public override void Validate()
        {
            base.Validate();
        }
    }
}
