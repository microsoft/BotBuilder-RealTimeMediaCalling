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
using System.Collections.Generic;
using Microsoft.Bot.Builder.Calling.ObjectModel.Misc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts
{
    /// <summary>
    /// This is the action which customers can specify to indicate that the platform should accept the call but that the
    /// bot will host the media.
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class AnswerAppHostedMedia : ActionBase
    {
        /// <summary>
        /// Create AnswerAppHostedMedia action as a standalone action
        /// </summary>
        public AnswerAppHostedMedia()
            : base(isStandaloneAction: true)
        {
            this.Action = RealTimeMediaValidActions.AnswerAppHostedMediaAction;
        }

        /// <summary>
        /// Opaque object to pass media configuration from the bot to the ExecutionAgent.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public JObject MediaConfiguration { get; set; }


        /// <summary>
        /// Validate the action.
        /// </summary>
        public override void Validate()
        {
            base.Validate();
            Utils.AssertArgument(this.Action == RealTimeMediaValidActions.AnswerAppHostedMediaAction, "Action was not AnswerAppHostedMedia");
            Utils.AssertArgument(this.MediaConfiguration != null, "MediaConfiguration must not be null.");
            Utils.AssertArgument(this.MediaConfiguration.ToString().Length <= MaxValues.MediaConfigurationLength, "MediaConfiguration must serialize to less than or equal to {0} characters.", MaxValues.MediaConfigurationLength);
        }
    }
}
