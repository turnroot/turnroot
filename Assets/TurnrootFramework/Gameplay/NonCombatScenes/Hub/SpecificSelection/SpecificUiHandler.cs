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
        private HubManager hubManager;

        private bool hasSavedCameraTransform;
        private Vector3 savedCameraPosition;
        private Quaternion savedCameraRotation;

        public HubSubLocation CurrentSubLocation { get; private set; }
        public HubPoiUi CurrentPoi { get; private set; }

        private HubSublocationName _currentType;
        private Shop.Shop _activeShop;
        private Blacksmith.Blacksmith _activeBlacksmith;
        private DockShip _activeDockShip;

        // Used to pause shop/blacksmith input while welcome/exit dialogue is running.
        private bool _waitingForShopEntryDialogue;
        private bool _waitingForShopExitDialogue;

        // Stored reference so unsubscribe always targets the same object we subscribed to.
        private ConversationController _subscribedController;

        private void Awake() => hubManager = GetComponent<HubManager>();

        public void SetCurrentSelection(HubSubLocation subLocation, HubPoiUi poi)
        {
            var type = poi.Type;
            _currentType = type;

            if (hubManager != null && hubManager.GeneralCamera != null)
            {
                savedCameraPosition = hubManager.GeneralCamera.transform.position;
                savedCameraRotation = hubManager.GeneralCamera.transform.rotation;
                hasSavedCameraTransform = true;
            }

            CurrentSubLocation = subLocation;
            CurrentPoi = poi;

            if (type == HubSublocationName.Market)
            {
                HandleMarketSelection(poi);
            }
            else if (type == HubSublocationName.Docks)
            {
                HandleDockSelection(poi);
            }
        }

        public void HandleMarketSelection(HubPoiUi poi)
        {
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

        public void HandleDockSelection(HubPoiUi poi)
        {
            var newDockShip = poi?.GetComponent<DockShip>();
            if (newDockShip != null && newDockShip != _activeDockShip)
            {
                _activeDockShip = newDockShip;

                var welcomeOneShot = _activeDockShip.GetRandomWelcomeOneShot();
                if (!string.IsNullOrWhiteSpace(welcomeOneShot.Dialogue))
                {
                    _waitingForShopEntryDialogue = true;
                    SubscribeToConversationFinished();
                }

                _activeDockShip.NotifyShipVisited();
            }
            else if (newDockShip == null)
            {
                _activeDockShip = null;
            }
        }

        public void HandleInput(string action)
        {
            if (_waitingForShopEntryDialogue || _waitingForShopExitDialogue)
            {
                if (
                    action
                    is InputActionConstants.Cancel
                        or "Back"
                        or InputActionConstants.Submit
                        or InputActionConstants.Select
                )
                {
                    _subscribedController?.Advance();
                }
                return;
            }

            if (action is "Back" or InputActionConstants.Cancel)
            {
                if (_currentType == HubSublocationName.Market)
                {
                    HandleMarketExit(action);
                }
                else if (_currentType == HubSublocationName.Docks)
                {
                    HandleDockShopBack(action);
                }
            }
            if (action is InputActionConstants.NavigateRight or InputActionConstants.NavigateLeft)
            {
                if (_currentType == HubSublocationName.Market)
                {
                    HandleMarketLeftRight(action);
                }
                else if (_currentType == HubSublocationName.Docks)
                {
                    HandleDockShopLeftRight(action);
                }
            }
            if (action is InputActionConstants.NavigateUp or InputActionConstants.NavigateDown)
            {
                if (_currentType == HubSublocationName.Market)
                {
                    HandleMarketUpDown(action);
                }
                else if (_currentType == HubSublocationName.Docks)
                {
                    HandleDockShopUpDown(action);
                }
            }
            if (action is InputActionConstants.Submit or InputActionConstants.Select)
            {
                if (_currentType == HubSublocationName.Market)
                {
                    HandleMarketSelection(action);
                }
                else if (_currentType == HubSublocationName.Docks)
                {
                    HandleDockShopSelection(action);
                }
            }
            if (action is InputActionConstants.ScrollLeft or InputActionConstants.ScrollRight)
            {
                if (_currentType == HubSublocationName.Market)
                {
                    HandleMarketPageChange(action);
                }
                else if (_currentType == HubSublocationName.Docks)
                {
                    HandleDockShopPageChange(action);
                }
            }
        }

        private void CompleteExit()
        {
            // Re-activate the POI icon before re-enabling look input so UpdateFov never
            // calls Hide() on an inactive game object and the player can re-select it.
            CurrentPoi?.Show();

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

            if (
                _activeDockShip != null
                && _activeDockShip.TryGetComponent<DockShipUi>(out var dockShipUi)
            )
            {
                dockShipUi.DockShipUiFade.Hide();
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
            _activeDockShip = null;
            hubManager.RevertToPreviousInputMode();
        }
    }
}
