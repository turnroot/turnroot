using Turnroot.Conversations;
using Turnroot.Gameplay.NonCombatScenes.Hub.Blacksmith;
using Turnroot.Gameplay.NonCombatScenes.Hub.Docks;
using Turnroot.Utilities;
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
            var activeVendor = _activeShop as Abstract.HubVendor ?? _activeBlacksmith;
            if (activeVendor != null)
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
                var blacksmithUi = _activeBlacksmith.GetComponent<BlacksmithUi>();
                if (blacksmithUi != null)
                {
                    blacksmithUi.HandleNavigateRightInput(action);
                    blacksmithUi.HandleNavigateLeftInput(action);
                }
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

            CompleteShopExit();
        }

        private void CompleteShopExit()
        {
            // Hide the shop/blacksmith UI before clearing the vendor reference.
            if (_activeShop != null && _activeShop.TryGetComponent<Shop.ShopUi>(out var shopUi))
            {
                shopUi.ShopUiFade.Hide();
            }

            if (
                _activeBlacksmith != null
                && _activeBlacksmith.TryGetComponent<BlacksmithUi>(out var blacksmithUi)
            )
            {
                blacksmithUi.BlacksmithUiFade.Hide();
            }

            // Restore the camera to the last user-controlled position/rotation (before selecting a POI)
            if (hasSavedCameraTransform && hubManager?.GeneralCamera != null)
            {
                hubManager.GeneralCamera.transform.SetPositionAndRotation(
                    savedCameraPosition,
                    savedCameraRotation
                );
                hasSavedCameraTransform = false;
            }
            else
            {
                // Fallback to default behavior if we don't have a saved transform
                hubManager.CurrentSubLocation?.ResetCameraToCameraPoint();
            }

            _activeShop = null;
            _activeBlacksmith = null;
            hubManager.RevertToPreviousInputMode();
        }
    }
}
