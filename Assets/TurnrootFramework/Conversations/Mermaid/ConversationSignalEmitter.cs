using Turnroot.Gameplay.Brain;
using Turnroot.Utilities;

namespace Turnroot.Conversations.Mermaid
{
    /// <summary>
    /// Emits brain signals declared by <c>Signal_</c> nodes in a Mermaid conversation graph.
    /// Other systems can subscribe to <see cref="Brain.OnConversationSignal"/> to react.
    /// </summary>
    public static class ConversationSignalEmitter
    {
        public static void Emit(MermaidNode node, Conversation conversation)
        {
            if (node == null || conversation == null)
            {
                return;
            }

            var signalName = node.ActionTarget;
            if (string.IsNullOrWhiteSpace(signalName))
            {
                $"ConversationSignalEmitter: signal node '{node.Id}' has no signal name.".LogWarning();
                return;
            }

            var brain = GetAndCacheBrain.GetBrain();
            if (brain == null)
            {
                "ConversationSignalEmitter: could not find Brain.".LogWarning();
                return;
            }

            brain.PublishConversationSignal(conversation.name, signalName);
            $"ConversationSignalEmitter: emitted signal '{signalName}' from conversation '{conversation.name}'.".LogInfo();
        }
    }
}
