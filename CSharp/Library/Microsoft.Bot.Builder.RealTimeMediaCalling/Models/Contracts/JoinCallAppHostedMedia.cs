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
using Microsoft.Bot.Builder.Calling.ObjectModel.Misc;
using Microsoft.Bot.Builder.Calling.ObjectModel.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts
{
    /// <summary>
    /// In case of Join action bot does not start a new call but joins a call
    /// that's already ongoing. Example of call that could be joined is multiparty
    /// conversation between 3 or more participants. Join operation is not valid
    /// for starting bot to single user calls.
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class JoinCallAppHostedMedia : ActionBase
    {
        /// <summary>
        /// Max size of ParticipantLegMetadata in answer or join call actions.
        /// </summary>
        public static readonly int ParticipantLegMetadataLength = 1024;

        /// <summary>
        /// Custom display name of the bot
        /// </summary>
        [JsonProperty(Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string DisplayName { get; set; }

        /// <summary>
        /// Custom id of the bot.
        /// </summary>
        [JsonProperty(Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string JoinAsId { get; set; }

        /// <summary>
        /// Conversation join token. This value defines the target group conversation
        /// to be joined.
        /// </summary>
        //TODO remove this once five IDs are supported on pma side
        [JsonProperty(Required = Required.Always)]
        public string JoinToken { get; set; }

        /// <summary>
        /// The id of the thread, for multiparty calls.
        /// </summary>
        [JsonProperty(Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string ThreadId { get; set; }

        /// <summary>
        /// The id of the thread message, for multiparty calls.
        /// </summary>
        /// //TODO needs to be serialized
        public string ThreadMessageId { get; set; }

        /// <summary>
        /// The id of the call leg in the conversation
        /// </summary>
        [JsonProperty(Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string CallLegId { get; set; }

        /// <summary>
        /// The id of the organizer in the conversation
        /// </summary>
        /// //TODO needs to be serialized 
        public string OrganizerId { get; set; }

        /// <summary>
        /// The id of the tenant for the conversation to be joined
        /// </summary>
        /// //TODO needs to be serialized 
        public string TenantId { get; set; }

        /// <summary>
        /// Joins the conversation as a hidden entity
        /// </summary>
        [JsonProperty(Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public bool? Hidden { get; set; }

        /// <summary>
        /// Configuration returned by in-app hosted media stack.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public JObject MediaConfiguration { get; set; }

        /// <summary>
        /// Opaque object to pass from bot to other participants that are part of multiparty call.
        /// </summary>
        [JsonProperty(Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public JObject ParticipantLegMetadata { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public JoinCallAppHostedMedia(JoinCallParameters joinCallParameters = null)
            : base(isStandaloneAction: true)
        {
            if (joinCallParameters != null)
            {
                if (joinCallParameters.DisplayName != null)
                {
                    this.DisplayName = joinCallParameters.DisplayName;
                    // If no join as id is given, generate one.
                    this.JoinAsId = joinCallParameters.JoinAsId ?? "8:origid:" + Guid.NewGuid();
                }
                if (string.IsNullOrWhiteSpace(joinCallParameters.OrganizerId))
                {
                    throw new ArgumentNullException(nameof(OrganizerId));
                }
                if (string.IsNullOrWhiteSpace(joinCallParameters.TenantId))
                {
                    throw new ArgumentNullException(nameof(TenantId));
                }
                if (string.IsNullOrWhiteSpace(joinCallParameters.ThreadMessageId))
                {
                    throw new ArgumentNullException(nameof(ThreadMessageId));
                }
                if (string.IsNullOrWhiteSpace(joinCallParameters.ThreadId))
                {
                    throw new ArgumentNullException(nameof(ThreadId));
                }

                this.JoinAsId = joinCallParameters.JoinAsId;
                this.JoinToken = joinCallParameters.JoinToken;
                this.OrganizerId = joinCallParameters.OrganizerId;
                this.TenantId = joinCallParameters.TenantId;
                this.ThreadId = joinCallParameters.ThreadId;
                this.CallLegId = joinCallParameters.CallLegId;
                this.ThreadMessageId = joinCallParameters.ThreadMessageId;
                this.Hidden = joinCallParameters.Hidden;
            }

            this.Action = RealTimeMediaValidActions.JoinCallAppHostedMediaAction;
        }

        /// <summary>
        /// Validation for JoinCallAppHostedMedia
        /// </summary>
        public override void Validate()
        {
            base.Validate();

            Utils.AssertArgument(this.MediaConfiguration != null, "MediaConfiguration must not be null.");
            Utils.AssertArgument(this.MediaConfiguration.ToString().Length <= MaxValues.MediaConfigurationLength, "MediaConfiguration must serialize to less than or equal to {0} characters.", MaxValues.MediaConfigurationLength);
            Utils.AssertArgument(this.JoinToken != null, "JoinToken cannot be null");

            if (this.ParticipantLegMetadata != null)
            {
                //TODO: 
                //ParticipantLegMetadataLength is in the newest calling nuget, will change it to get from the nuget once calling nuget is updated
                Utils.AssertArgument(this.ParticipantLegMetadata.ToString().Length <= ParticipantLegMetadataLength, "ParticipantLegMetadata must serialize to less than or equal to {0} characters.", ParticipantLegMetadataLength);
            }
        }
    }
}
