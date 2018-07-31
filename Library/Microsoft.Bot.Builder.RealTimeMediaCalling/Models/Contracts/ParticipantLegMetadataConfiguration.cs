using Microsoft.Bot.Builder.Calling.ObjectModel.Contracts;
using Microsoft.Bot.Builder.RealTimeMediaCalling.ObjectModel.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Builder.RealTimeMediaCalling.Models.Contracts
{
    /// <summary>
    /// 
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class ParticipantLegMetadataConfiguration : ActionBase
    {

        /// <summary>
        /// Create a VideoSubscription action.
        /// </summary>
        public ParticipantLegMetadataConfiguration()
        {
            this.Action = RealTimeMediaValidActions.ParticipantLegMetadataConfiguration;
        }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty(Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public JObject ParticipantLegMetadata { get; set; }
    }
}
