using Turnroot.Conversations;
using Turnroot.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Turnroot.Conversations.Branching
{
    [CreateNodeMenu("Conversation/Conversation")]
    public class ConversationNode : Node
    {
        [Input]
        public ConversationFlow previous;

        [Output(ShowBackingValue.Unconnected, ConnectionType.Override)]
        public ConversationFlow next;
        public ConversationLayer conversationLayer;

        public override object GetValue(NodePort port)
        {
            if (port.fieldName == "next")
                return next;
            return null;
        }
    }

    [System.Serializable]
    public struct ConversationFlow { }
}
