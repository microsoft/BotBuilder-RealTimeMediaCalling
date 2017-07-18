using System;
using Microsoft.Bot.Builder.Calling.ObjectModel.Misc;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts
{
    /// <summary>
    /// Uniquely identifies a scheduled meeting. Using this will start the meeting if it is
    /// not running already or join the already running meeting.
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class MeetingInfo
    {
        /// <summary>
        /// Id of the group or thread where the meeting was scheduled.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string ThreadId { get; set; }

        /// <summary>
        /// Id of the message inside the group where the meeting was scheduled.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string MessageId { get; set; }

        /// <summary>
        /// Azure AD object id of the meeting organizer.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public Guid OrganizerId { get; set; }

        /// <summary>
        /// Id of organizer's tenant.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public Guid TenantId { get; set; }

        /// <summary>
        /// Optional. Id of the reply if the meeting was scheduled as a reply to a message.
        /// </summary>
        [JsonProperty(Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string ReplyChainMessageId { get; set; }

        public void Validate()
        {
            Utils.AssertArgument(this.ThreadId != null, "ThreadId must not be null.");
            Utils.AssertArgument(this.MessageId != null, "MessageId must not be null.");
        }
    }
}
