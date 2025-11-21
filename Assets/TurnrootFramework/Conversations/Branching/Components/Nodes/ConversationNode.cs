using Turnroot.Conversations;
using Turnroot.Skills.Nodes;
using UnityEngine;
using XNode;
#if UNITY_EDITOR
using System;
using System.IO;
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

        public override object GetValue(NodePort port)
        {
            if (port.fieldName == "next")
                return next;
            return null;
        }

#if UNITY_EDITOR
        // OnEnable diagnostics removed — cleanup after investigation.
#endif
    }

    [System.Serializable]
    public struct ConversationFlow { }
}
