using UnityEngine;
using XNode;

namespace Turnroot.Conversations.Branching
{
    /// <summary>
    /// Base class for conversation graph nodes that perform a gameplay action when traversed.
    /// Action nodes have a single input and a single output and execute their side effect
    /// before the conversation flow continues.
    /// </summary>
    public abstract class ConversationActionNode : Node
    {
        [Input]
        public ConversationFlow previous;

        [Output(ShowBackingValue.Unconnected, ConnectionType.Override)]
        public ConversationFlow next;

        /// <summary>
        /// Executes the node's side effect. Called by <see cref="ConversationController"/>
        /// when this node is reached during branching conversation playback.
        /// </summary>
        public abstract void Execute(ConversationController controller);

        public override object GetValue(NodePort port) => port.fieldName == "next" ? next : null;
    }
}
