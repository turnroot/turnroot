using UnityEngine;
using XNode;

namespace Turnroot.Conversations.Branching
{
    /// <summary>
    /// Represents a single conversation dialogue node in a branching conversation graph.
    /// </summary>
    [CreateNodeMenu("Conversation/Conversation")]
    public class ConversationNode : Node
    {
        [Input]
        public ConversationFlow previous;

        [Output(ShowBackingValue.Unconnected, ConnectionType.Override)]
        public ConversationFlow next;
        public ConversationLayer conversationLayer;

        public override object GetValue(NodePort port) => port.fieldName == "next" ? next : null;
    }

    /// <summary>
    /// Represents the flow connection between conversation nodes.
    /// </summary>
    [System.Serializable]
    public struct ConversationFlow { }
}
