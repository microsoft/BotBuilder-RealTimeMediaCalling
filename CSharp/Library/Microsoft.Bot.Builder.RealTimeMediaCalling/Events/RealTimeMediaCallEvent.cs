namespace Microsoft.Bot.Builder.RealTimeMediaCalling.Events
{
    public class RealTimeMediaCallEvent
    {
        public string ConversationId { get; }

        public IRealTimeMediaCall Call { get; set; }

        public RealTimeMediaCallEvent(string conversationId, IRealTimeMediaCall call)
        {
            ConversationId = conversationId;
            Call = call;
        }
    }
}
