using Turnroot.Characters;
using Turnroot.Characters.Components.Support;
using Turnroot.Conversations;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Manages conversations and conversation progressions within the game's brain system.
    /// Tracks which conversations have been seen and handles support conversation logic.
    /// </summary>
    [RequireComponent(typeof(LongTermMemory))]
    public class ConversationalBrain : BrainComponent
    {
        private LongTermMemory _ltm;

        protected override void Awake()
        {
            base.Awake();
            _ltm = GetComponent<LongTermMemory>();
            Debug.Log("ConversationalBrain is ready.");
        }

        protected override void SubscribeToBrainEvents()
        {
            // Subscribe to conversation end to track completion
            _brain.OnConversationEnded += HandleConversationEnded;
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            _brain.OnConversationEnded -= HandleConversationEnded;
        }

        private void HandleConversationEnded(Conversation conversation)
        {
            if (conversation != null)
            {
                MarkConversationCompleted(conversation);
            }
        }

        #region Conversation Persistence

        /// <summary>
        /// Mark a conversation as completed (fully watched through).
        /// </summary>
        public void MarkConversationCompleted(Conversation conversation)
        {
            if (conversation == null || _ltm == null)
            {
                return;
            }

            var key = $"{LtmKeys.ConversationCompletedPrefix}{conversation.name}";
            _ltm.RememberBool(key, true);
            Debug.Log(
                $"ConversationalBrain: Marked conversation '{conversation.name}' as completed."
            );
        }

        /// <summary>
        /// Check if a conversation has been completed.
        /// </summary>
        public bool HasCompletedConversation(Conversation conversation)
        {
            if (conversation == null || _ltm == null)
            {
                return false;
            }

            var key = $"{LtmKeys.ConversationCompletedPrefix}{conversation.name}";
            return _ltm.RecallBool(key);
        }

        /// <summary>
        /// Check if a conversation has been completed by name.
        /// </summary>
        public bool HasCompletedConversation(string conversationName)
        {
            if (string.IsNullOrEmpty(conversationName) || _ltm == null)
            {
                return false;
            }

            var key = $"{LtmKeys.ConversationCompletedPrefix}{conversationName}";
            return _ltm.RecallBool(key);
        }

        /// <summary>
        /// Mark a conversation as seen (started but not necessarily completed).
        /// </summary>
        public void MarkConversationSeen(Conversation conversation)
        {
            if (conversation == null || _ltm == null)
            {
                return;
            }

            var key = $"{LtmKeys.ConversationSeenPrefix}{conversation.name}";
            _ltm.RememberBool(key, true);
        }

        /// <summary>
        /// Check if a conversation has been seen.
        /// </summary>
        public bool HasSeenConversation(Conversation conversation)
        {
            if (conversation == null || _ltm == null)
            {
                return false;
            }

            var key = $"{LtmKeys.ConversationSeenPrefix}{conversation.name}";
            return _ltm.RecallBool(key);
        }

        /// <summary>
        /// Mark a support conversation between two characters as completed.
        /// </summary>
        public void MarkSupportConversationCompleted(
            CharacterData character1,
            CharacterData character2,
            string supportLevel
        )
        {
            if (character1 == null || character2 == null || _ltm == null)
            {
                return;
            }

            // Use alphabetical order to ensure consistent key regardless of parameter order
            var name1 = character1.name;
            var name2 = character2.name;
            var key =
                string.Compare(name1, name2) < 0
                    ? $"{LtmKeys.SupportConversationPrefix}{name1}_{name2}_{supportLevel}"
                    : $"{LtmKeys.SupportConversationPrefix}{name2}_{name1}_{supportLevel}";

            _ltm.RememberBool(key, true);
            Debug.Log(
                $"ConversationalBrain: Support conversation {name1}/{name2} rank {supportLevel} completed."
            );
        }

        /// <summary>
        /// Check if a support conversation has been completed.
        /// </summary>
        public bool HasCompletedSupportConversation(
            CharacterData character1,
            CharacterData character2,
            string supportLevel
        )
        {
            if (character1 == null || character2 == null || _ltm == null)
            {
                return false;
            }

            var name1 = character1.name;
            var name2 = character2.name;
            var key =
                string.Compare(name1, name2) < 0
                    ? $"{LtmKeys.SupportConversationPrefix}{name1}_{name2}_{supportLevel}"
                    : $"{LtmKeys.SupportConversationPrefix}{name2}_{name1}_{supportLevel}";

            return _ltm.RecallBool(key);
        }

        #endregion

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
