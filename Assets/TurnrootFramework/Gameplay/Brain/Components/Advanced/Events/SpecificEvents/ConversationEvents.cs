using System;
using Turnroot.Characters.Components.Support;
using Turnroot.Conversations;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain
    {
        #region Conversation Events

        public event Action<SupportRelationshipInstance> OnSupportPointsChanged;
        public event Action<SupportRelationshipInstance> OnSupportConversationAvailable;
        public event Action<SupportRelationshipInstance> OnSLevelSupportConversationAvailable;
        public event Action<Conversation> OnConversationStarted;
        public event Action<Conversation> OnConversationEnded;
        public event Action<ConversationLayer> OnConversationLayerStarted;
        public event Action<ConversationLayer> OnConversationLayerEnded;

        /// <summary>
        /// Fired when gameplay code reports a conversation signal.
        /// Parameters: conversation name, signal name.
        /// </summary>
        public event Action<string, string> OnConversationSignal;

        /// <summary>
        /// Fired when an external system reports that a conversation condition has been met.
        /// Parameters: conversation name, condition name.
        /// </summary>
        public event Action<string, string> OnConversationConditionMet;

        public event Action OnPlayerAcknowledgedConversationEvent;

        public event Action<string> OnWaitForPlayerAcknowledgment;

        public void PublishSupportPointsChanged(SupportRelationshipInstance relationship) =>
            OnSupportPointsChanged?.Invoke(relationship);

        public void PublishSupportConversationAvailable(SupportRelationshipInstance relationship) =>
            OnSupportConversationAvailable?.Invoke(relationship);

        public void PublishSLevelSupportConversationAvailable(
            SupportRelationshipInstance relationship
        ) => OnSLevelSupportConversationAvailable?.Invoke(relationship);

        public void PublishConversationStarted(Conversation conversation) =>
            OnConversationStarted?.Invoke(conversation);

        public void PublishConversationEnded(Conversation conversation) =>
            OnConversationEnded?.Invoke(conversation);

        public void PublishConversationLayerStarted(ConversationLayer layer) =>
            OnConversationLayerStarted?.Invoke(layer);

        public void PublishConversationLayerEnded(ConversationLayer layer) =>
            OnConversationLayerEnded?.Invoke(layer);

        public void PublishConversationSignal(string conversationName, string signalName) =>
            OnConversationSignal?.Invoke(conversationName, signalName);

        public void PublishConversationConditionMet(
            string conversationName,
            string conditionName
        ) => OnConversationConditionMet?.Invoke(conversationName, conditionName);

        public void PublishPlayerAcknowledgedConversationEvent() =>
            OnPlayerAcknowledgedConversationEvent?.Invoke();

        public void PublishWaitForPlayerAcknowledgment(string id) =>
            OnWaitForPlayerAcknowledgment?.Invoke(id);
        #endregion
    }
}
