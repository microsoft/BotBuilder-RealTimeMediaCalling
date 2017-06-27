namespace Microsoft.Bot.Builder.RealTimeMediaCalling.Events
{
    /// <summary>
    /// The event raised when a RealTimeMediaCall is created or ended
    /// </summary>
    public class RealTimeMediaCallEvent
    {
        /// <summary>
        /// The conversation Id associated with the call
        /// </summary>
        public string ConversationId { get; }

        /// <summary>
        /// The call instance itself
        /// </summary>
        public IRealTimeMediaCall Call { get; set; }

        /// <summary>
        /// The event raised when a RealTimeMediaCall is created or ended
        /// <param conversationId="id">The ID of the call.</param>
        /// <param call="call">The call being created or ended</param>
        /// </summary>
        public RealTimeMediaCallEvent(string conversationId, IRealTimeMediaCall call)
        {
            ConversationId = conversationId;
            Call = call;
        }
    }
}
