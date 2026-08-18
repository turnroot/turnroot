using Turnroot.Conversations;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [RequireComponent(typeof(HubManager))]
    public partial class SpecificUiHandler : MonoBehaviour
    {
        private void OnDisable() => UnsubscribeFromConversationFinished();

        private ConversationController FindConversationController() =>
            FindFirstObjectByType<ConversationController>();

        private void SubscribeToConversationFinished()
        {
            var cc = FindConversationController();
            if (cc != null)
            {
                _subscribedController = cc;
                cc.OnAnyConversationFinished += OnConversationFinished;
            }
            else
            {
                "SpecificUiHandler: No ConversationController found — exit dialogue completion will not be detected.".LogWarning();
            }
        }

        private void UnsubscribeFromConversationFinished()
        {
            if (_subscribedController != null)
            {
                _subscribedController.OnAnyConversationFinished -= OnConversationFinished;
                _subscribedController = null;
            }
        }

        private void OnConversationFinished()
        {
            if (_waitingForShopEntryDialogue)
            {
                _waitingForShopEntryDialogue = false;
                UnsubscribeFromConversationFinished();

                // For character interactions, show the actions menu after the welcome dialogue.
                if (_activeHubCharacter != null)
                {
                    ShowCharacterInteractions();
                }

                return;
            }

            if (!_waitingForShopExitDialogue)
            {
                return;
            }

            _waitingForShopExitDialogue = false;
            UnsubscribeFromConversationFinished();
            CompleteExit();
        }
    }
}
