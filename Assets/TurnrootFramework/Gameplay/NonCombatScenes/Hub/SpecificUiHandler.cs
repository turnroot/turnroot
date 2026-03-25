using Turnroot.Conversations;
using Turnroot.Gameplay.NonCombatScenes.Hub.Blacksmith;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [RequireComponent(typeof(HubManager))]
    public class SpecificUiHandler : MonoBehaviour
    {
        private HubManager hubManager;

        private bool hasSavedCameraTransform;
        private Vector3 savedCameraPosition;
        private Quaternion savedCameraRotation;

        public HubSubLocation CurrentSubLocation { get; private set; }
        public HubPoiUi CurrentPoi { get; private set; }

        // Track the active shop or blacksmith for notifying visited/exited events.
        private Shop.Shop _activeShop;
        private Blacksmith.Blacksmith _activeBlacksmith;

        // Used to pause shop/blacksmith input while welcome/exit dialogue is running.
        private bool _waitingForShopEntryDialogue;
        private bool _waitingForShopExitDialogue;

        // Stored reference so unsubscribe always targets the same object we subscribed to.
        private ConversationController _subscribedController;

        private void Awake()
        {
            hubManager = GetComponent<HubManager>();
        }

        private void OnDisable()
        {
            UnsubscribeFromConversationFinished();
        }

        private ConversationController FindConversationController()
        {
            return FindFirstObjectByType<ConversationController>();
        }

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
            CompleteShopExit();
        }

        public void SetCurrentSelection(HubSubLocation subLocation, HubPoiUi poi)
        {
            // Remember where the player was looking before we moved the camera to the POI
            // This lets us restore the exact transform when they hit Back
            if (hubManager != null && hubManager.GeneralCamera != null)
            {
                savedCameraPosition = hubManager.GeneralCamera.transform.position;
                savedCameraRotation = hubManager.GeneralCamera.transform.rotation;
                hasSavedCameraTransform = true;
            }

            CurrentSubLocation = subLocation;
            CurrentPoi = poi;

            var newShop = poi?.GetComponent<Shop.Shop>();
            if (newShop != null && newShop != _activeShop)
            {
                _activeShop = newShop;
                _activeBlacksmith = null;

                var welcomeOneShot = _activeShop.GetRandomWelcomeOneShot();
                if (!string.IsNullOrWhiteSpace(welcomeOneShot.Dialogue))
                {
                    _waitingForShopEntryDialogue = true;
                    SubscribeToConversationFinished();
                }

                _activeShop.NotifyShopVisited();
            }
            else if (newShop == null)
            {
                _activeShop = null;

                if (poi.TryGetComponent<Blacksmith.Blacksmith>(out var blacksmith))
                {
                    _activeBlacksmith = blacksmith;

                    var welcomeOneShot = _activeBlacksmith.GetRandomWelcomeOneShot();
                    if (!string.IsNullOrWhiteSpace(welcomeOneShot.Dialogue))
                    {
                        _waitingForShopEntryDialogue = true;
                        SubscribeToConversationFinished();
                    }

                    blacksmith.NotifyBlacksmithVisited();
                }
                else
                {
                    _activeBlacksmith = null;
                }
            }
        }

        public void HandleInput(string action)
        {
            if (_waitingForShopEntryDialogue || _waitingForShopExitDialogue)
            {
                $"SpecificUiHandler: Ignoring input '{action}' while waiting for shop dialogue to finish.".LogInfo();
                // Ignore input while waiting for the shop welcome or exit dialogue to finish.
                return;
            }

            if (action == "Back" || action == InputActionConstants.Cancel)
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
            if (action is InputActionConstants.NavigateRight or InputActionConstants.NavigateLeft)
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
            if (action is InputActionConstants.NavigateUp or InputActionConstants.NavigateDown)
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
            if (action == InputActionConstants.Submit || action == InputActionConstants.Select)
            {
                var activeVendor = _activeShop as Abstract.HubVendor ?? _activeBlacksmith;
                if (activeVendor != null)
                {
                    activeVendor.HandleConfirmInput(action);
                }
            }
            if (action is InputActionConstants.ScrollLeft or InputActionConstants.ScrollRight)
            {
                // shop?
                // change the page
                if (_activeShop != null)
                {
                    $"SpecificUiHandler: Received page change input '{action}' for active shop '{_activeShop.name}'".LogInfo();
                    _activeShop.Ui.ChangePageInput(action);
                }
            }
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
                && _activeBlacksmith.TryGetComponent<Blacksmith.BlacksmithUi>(out var blacksmithUi)
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
