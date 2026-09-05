using Turnroot.Characters;
using Turnroot.Characters.Components.Support;
using Turnroot.Conversations;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Utilities;
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

        protected override EventPriority GetSubscriptionPriority() => EventPriority.Normal;

        protected override void Awake()
        {
            base.Awake();
            _ltm = GetComponent<LongTermMemory>();
        }

        protected override void SubscribeToBrainEvents() =>
            _brain.OnConversationEnded += HandleConversationEnded;

        protected override void UnsubscribeFromBrainEvents() =>
            _brain.OnConversationEnded -= HandleConversationEnded;

        private void HandleConversationEnded(Conversation conversation)
        {
            if (conversation != null)
            {
                MarkConversationCompleted(conversation);
            }
        }

        #region Conversation Persistence

        public void MarkConversationCompleted(Conversation conversation)
        {
            if (conversation == null || _ltm == null)
            {
                return;
            }

            var key = $"{LtmKeys.ConversationCompletedPrefix}{conversation.UniqueId}";
            _ltm.RememberBool(key, true);

            $"ConversationalBrain: Marked conversation '{conversation.UniqueId}' as completed.".LogInfo();
        }

        public bool HasCompletedConversation(Conversation conversation)
        {
            if (conversation == null || _ltm == null)
            {
                return false;
            }

            var key = $"{LtmKeys.ConversationCompletedPrefix}{conversation.UniqueId}";
            return _ltm.RecallBool(key);
        }

        public bool HasCompletedConversation(string conversationName)
        {
            if (string.IsNullOrEmpty(conversationName) || _ltm == null)
            {
                return false;
            }

            var key = $"{LtmKeys.ConversationCompletedPrefix}{conversationName}";
            return _ltm.RecallBool(key);
        }

        public void MarkConversationSeen(Conversation conversation)
        {
            if (conversation == null || _ltm == null)
            {
                return;
            }

            var key = $"{LtmKeys.ConversationSeenPrefix}{conversation.UniqueId}";
            _ltm.RememberBool(key, true);
        }

        public bool HasSeenConversation(Conversation conversation)
        {
            if (conversation == null || _ltm == null)
            {
                return false;
            }

            var key = $"{LtmKeys.ConversationSeenPrefix}{conversation.UniqueId}";
            return _ltm.RecallBool(key);
        }

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

            $"ConversationalBrain: Support conversation {name1}/{name2} rank {supportLevel} completed.".LogInfo();
        }

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

        public void StartConversation(Conversation conversation)
        {
            if (conversation == null)
            {
                "ConversationalBrain: Cannot start null conversation.".LogWarning();
                return;
            }

            Brain.PublishConversationStarted(conversation);
            $"ConversationalBrain: Started conversation '{conversation.UniqueId}'".LogInfo();
        }

        public bool CanStartConversation(Conversation conversation) =>
            conversation != null && (conversation.CanRepeat || !HasSeenConversation(conversation));

        public void MarkConversationStarted(Conversation conversation) =>
            MarkConversationSeen(conversation);

        public void EndConversation(Conversation conversation)
        {
            Brain.PublishConversationEnded(conversation);
            $"ConversationalBrain: Ended conversation '{conversation.UniqueId}'".LogInfo();
        }

        public void StartConversationLayer(ConversationLayer layer)
        {
            if (layer == null)
            {
                "ConversationalBrain: Cannot start null conversation layer.".LogWarning();
                return;
            }

            Brain.PublishConversationLayerStarted(layer);
        }

        public void EndConversationLayer(ConversationLayer layer) =>
            Brain.PublishConversationLayerEnded(layer);

        public void NotifySupportPointsChanged(SupportRelationshipInstance relationship)
        {
            if (relationship == null)
            {
                return;
            }

            Brain.PublishSupportPointsChanged(relationship);
        }

        public void NotifySupportLevelIncreased(
            CharacterInstance source,
            SupportRelationshipInstance relationship
        )
        {
            if (source == null || relationship == null)
            {
                return;
            }

            Brain.PublishSupportLevelIncreased(source, relationship);
        }

        public void NotifySupportConversationAvailable(SupportRelationshipInstance relationship)
        {
            if (relationship == null)
            {
                return;
            }

            Brain.PublishSupportConversationAvailable(relationship);
        }

        public void NotifySLevelSupportConversationAvailable(
            SupportRelationshipInstance relationship
        )
        {
            if (relationship == null)
            {
                return;
            }

            Brain.PublishSLevelSupportConversationAvailable(relationship);
        }

        /// <summary>
        /// Emits a signal from the active conversation. Other brain segments can subscribe to
        /// <see cref="Brain.OnConversationSignal"/> to react.
        /// </summary>
        public void NotifyConversationSignal(Conversation conversation, string signalName)
        {
            if (conversation == null || string.IsNullOrWhiteSpace(signalName))
            {
                return;
            }

            Brain.PublishConversationSignal(conversation.UniqueId, signalName);
        }

        /// <summary>
        /// Reports that an external condition for the active conversation has been met.
        /// The conversation controller will resume at the matching condition branch.
        /// </summary>
        public void NotifyConversationCondition(Conversation conversation, string conditionName)
        {
            if (conversation == null || string.IsNullOrWhiteSpace(conditionName))
            {
                return;
            }

            Brain.PublishConversationConditionMet(conversation.UniqueId, conditionName);
        }

        #endregion
    }
}
