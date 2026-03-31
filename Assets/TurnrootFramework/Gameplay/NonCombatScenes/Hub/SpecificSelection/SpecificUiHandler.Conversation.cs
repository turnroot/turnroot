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
                cc.OnAnyConversationFinished.AddListener(OnConversationFinished);
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
                _subscribedController.OnAnyConversationFinished.RemoveListener(
                    OnConversationFinished
                );
                _subscribedController = null;
            }
        }

        private void OnConversationFinished()
        {
            if (_waitingForShopEntryDialogue)
            {
                _waitingForShopEntryDialogue = false;
                UnsubscribeFromConversationFinished();
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
