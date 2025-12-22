using UnityEngine;
using XNode;
#if UNITY_EDITOR
using System;
#endif

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

        public override object GetValue(NodePort port) => port.fieldName == "next" ? next : null;
    }

    [Serializable]
    public struct ConversationFlow { }
}
