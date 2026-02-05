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

            var key = $"{LtmKeys.ConversationCompletedPrefix}{conversation.name}";
            _ltm.RememberBool(key, true);
            TurnrootLogger.Log(
                $"ConversationalBrain: Marked conversation '{conversation.name}' as completed."
            );
        }

        public bool HasCompletedConversation(Conversation conversation)
        {
            if (conversation == null || _ltm == null)
            {
                return false;
            }

            var key = $"{LtmKeys.ConversationCompletedPrefix}{conversation.name}";
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

            var key = $"{LtmKeys.ConversationSeenPrefix}{conversation.name}";
            _ltm.RememberBool(key, true);
        }

        public bool HasSeenConversation(Conversation conversation)
        {
            if (conversation == null || _ltm == null)
            {
                return false;
            }

            var key = $"{LtmKeys.ConversationSeenPrefix}{conversation.name}";
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
            TurnrootLogger.Log(
                $"ConversationalBrain: Support conversation {name1}/{name2} rank {supportLevel} completed."
            );
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
                TurnrootLogger.Log(
                    "ConversationalBrain: Cannot start null conversation.",
                    TurnrootLogger.LogLevel.Warning
                );
                return;
            }

            Brain.PublishConversationStarted(conversation);
            TurnrootLogger.Log($"ConversationalBrain: Started conversation '{conversation.name}'");
        }

        public void EndConversation(Conversation conversation)
        {
            if (conversation == null)
            {
                TurnrootLogger.Log(
                    "ConversationalBrain: Cannot end null conversation.",
                    TurnrootLogger.LogLevel.Warning
                );
                return;
            }

            Brain.PublishConversationEnded(conversation);

            TurnrootLogger.Log(
                $"ConversationalBrain: Ended conversation '{conversation.name}'",
                TurnrootLogger.LogLevel.Warning
            );
        }

        public void StartConversationLayer(ConversationLayer layer)
        {
            if (layer == null)
            {
                TurnrootLogger.Log(
                    "ConversationalBrain: Cannot start null conversation layer.",
                    TurnrootLogger.LogLevel.Warning
                );
                return;
            }

            Brain.PublishConversationLayerStarted(layer);
        }

        public void EndConversationLayer(ConversationLayer layer)
        {
            if (layer == null)
            {
                TurnrootLogger.Log(
                    "ConversationalBrain: Cannot end null conversation layer.",
                    TurnrootLogger.LogLevel.Warning
                );
                return;
            }

            Brain.PublishConversationLayerEnded(layer);
        }

        public void NotifySupportPointsChanged(SupportRelationshipInstance relationship)
        {
            if (relationship == null)
            {
                return;
            }

            Brain.PublishSupportPointsChanged(relationship);
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

        #endregion
    }
}
