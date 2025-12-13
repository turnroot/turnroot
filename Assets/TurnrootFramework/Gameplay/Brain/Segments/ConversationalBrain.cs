using Turnroot.Characters;
using Turnroot.Characters.Components.Support;
using Turnroot.Conversations;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Manages conversations and conversation progressions within the game's brain system.
    /// </summary>
    public class ConversationalBrain : BrainComponent
    {
        protected override void Awake()
        {
            base.Awake();
            Debug.Log("ConversationalBrain is ready.");
        }

        protected override void SubscribeToBrainEvents()
        {
            // Subscribe to conversation-related events if needed
            // Currently this brain primarily orchestrates conversations
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            // No subscriptions to clean up
        }

        #region Conversation Management

        /// <summary>
        /// Start a conversation and notify the brain.
        /// </summary>
        public void StartConversation(Conversation conversation)
        {
            if (conversation == null)
            {
                Debug.LogWarning("ConversationalBrain: Cannot start null conversation.");
                return;
            }

            _brain?.PublishConversationStarted(conversation);
            Debug.Log($"ConversationalBrain: Started conversation '{conversation.name}'");
        }

        /// <summary>
        /// End a conversation and notify the brain.
        /// </summary>
        public void EndConversation(Conversation conversation)
        {
            if (conversation == null)
            {
                Debug.LogWarning("ConversationalBrain: Cannot end null conversation.");
                return;
            }

            _brain?.PublishConversationEnded(conversation);
            Debug.Log($"ConversationalBrain: Ended conversation '{conversation.name}'");
        }

        /// <summary>
        /// Start a conversation layer and notify the brain.
        /// </summary>
        public void StartConversationLayer(ConversationLayer layer)
        {
            if (layer == null)
            {
                Debug.LogWarning("ConversationalBrain: Cannot start null conversation layer.");
                return;
            }

            _brain?.PublishConversationLayerStarted(layer);
        }

        /// <summary>
        /// End a conversation layer and notify the brain.
        /// </summary>
        public void EndConversationLayer(ConversationLayer layer)
        {
            if (layer == null)
            {
                Debug.LogWarning("ConversationalBrain: Cannot end null conversation layer.");
                return;
            }

            _brain?.PublishConversationLayerEnded(layer);
        }

        /// <summary>
        /// Notify when support points change for a relationship.
        /// </summary>
        public void NotifySupportPointsChanged(SupportRelationshipInstance relationship)
        {
            if (relationship == null)
            {
                return;
            }

            _brain?.PublishSupportPointsChanged(relationship);
        }

        /// <summary>
        /// Notify when a support conversation becomes available.
        /// </summary>
        public void NotifySupportConversationAvailable(SupportRelationshipInstance relationship)
        {
            if (relationship == null)
            {
                return;
            }

            _brain?.PublishSupportConversationAvailable(relationship);
        }

        /// <summary>
        /// Notify when an S-level support conversation becomes available.
        /// </summary>
        public void NotifySLevelSupportConversationAvailable(
            SupportRelationshipInstance relationship
        )
        {
            if (relationship == null)
            {
                return;
            }

            _brain?.PublishSLevelSupportConversationAvailable(relationship);
        }

        #endregion
    }
}
