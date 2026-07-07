using Turnroot.Gameplay.NonCombatScenes.Hub.Blacksmith;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [RequireComponent(typeof(HubManager))]
    public partial class SpecificUiHandler : MonoBehaviour
    {
        public void HandleMarketSelection(string action)
        {
            var activeVendor = _activeShop as Abstract.HubVendor ?? _activeBlacksmith;
            if (activeVendor != null)
            {
                activeVendor.HandleConfirmInput(action);
            }
        }

        public void HandleMarketPageChange(string action)
        {
            if (_activeShop != null)
            {
                _activeShop.Ui.ChangePageInput(action);
            }
            else if (_activeBlacksmith != null)
            {
                var blacksmithUi = _activeBlacksmith.GetComponent<BlacksmithUi>();
                blacksmithUi?.ChangePageInput(action);
            }
        }

        public void HandleMarketUpDown(string action)
        {
            if (_activeShop != null)
            {
                _activeShop.Ui.HandleItemChangeInput(action);
            }
            else if (_activeBlacksmith != null)
            {
                var blacksmithUi = _activeBlacksmith.GetComponent<BlacksmithUi>();
                blacksmithUi?.HandleItemChangeInput(action);
            }
        }

        public void HandleMarketLeftRight(string action)
        {
            if (_activeShop != null)
            {
                _activeShop.Ui.HandleQuantityChangeInput(action);
            }
            else if (_activeBlacksmith != null)
            {
                if (_activeBlacksmith.TryGetComponent<BlacksmithUi>(out var blacksmithUi))
                {
                    blacksmithUi.HandleNavigateRightInput(action);
                    blacksmithUi.HandleNavigateLeftInput(action);
                }
            }
        }

        public void HandleMarketSpecial(string action)
        {
            if (_activeBlacksmith != null)
            {
                var blacksmithUi = _activeBlacksmith.GetComponent<BlacksmithUi>();
                blacksmithUi?.HandleSpecialInput(action);
            }
        }

        public void HandleMarketExit(string action)
        {
            var activeVendor = _activeShop as Abstract.HubVendor ?? _activeBlacksmith;
            if (activeVendor != null)
            {
                bool hasExitDialogue = activeVendor.HasFarewellDialogue();
                activeVendor.HandleBackInput(action);

                if (hasExitDialogue)
                {
                    _waitingForShopExitDialogue = true;
                    SubscribeToConversationFinished();
                    return;
                }
            }
            CompleteExit();
        }
    }
}
