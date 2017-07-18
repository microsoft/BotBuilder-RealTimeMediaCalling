using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts
{
    /// <summary>
    /// This class describes a participant.
    /// This can be a participant in any modality in a 2 or multi-party conversation
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class Participant
    {
        /// <summary>
        /// MRI of the participant .ex : 2:+14258828080 or '8:alice' 
        /// </summary>
        [JsonProperty(Required = Required.Always, Order = -2)]
        public string Identity { get; set; }

        /// <summary>
        /// Display name of participant if received from the controllers
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string DisplayName { get; set; }
    }
}
