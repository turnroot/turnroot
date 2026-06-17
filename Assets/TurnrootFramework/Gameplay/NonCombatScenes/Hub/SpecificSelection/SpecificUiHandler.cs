using Turnroot.Conversations;
using Turnroot.Gameplay.NonCombatScenes.Hub.Blacksmith;
using Turnroot.Gameplay.NonCombatScenes.Hub.Character;
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

        public HubSublocationName? CurrentSubLocation { get; private set; }
        public HubPoiUi CurrentPoi { get; private set; }

        private HubPoiType _currentType;
        private Shop.Shop _activeShop;
        private Blacksmith.Blacksmith _activeBlacksmith;
        private DockShip _activeDockShip;
        private HubCharacterManager _activeHubCharacter;

        // Used to pause shop/blacksmith input while welcome/exit dialogue is running.
        private bool _waitingForShopEntryDialogue;
        private bool _waitingForShopExitDialogue;

        /// <summary>
        /// Set by a tutorial handler when a first-visit or Explore tutorial is active.
        /// Input is forwarded exclusively to the tutorial until it clears this reference.
        /// </summary>
        public ISpecificUiTutorialHandler ActiveTutorialHandler { get; set; }

        // Stored reference so unsubscribe always targets the same object we subscribed to.
        private ConversationController _subscribedController;

        private void Awake() => hubManager = GetComponent<HubManager>();

        public void SetCurrentSelection(HubSublocationName? subLocation, HubPoiUi poi)
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

            hubManager.MainOverlayUiFade?.Hide();
            hubManager.FocusOverlayFade?.Hide();

            if (type == HubPoiType.MarketPOI)
            {
                HandleMarketSelection(poi);
            }
            else if (type == HubPoiType.DocksPOI)
            {
                HandleDockSelection(poi);
            }
            else if (type == HubPoiType.UnitPOI)
            {
                HandleCharacterPoiSelection(poi);
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
            var newDockShip =
                poi?.GetComponentInChildren<DockShip>() ?? poi?.GetComponentInParent<DockShip>();

            if (newDockShip == null)
            {
                $"SpecificUiHandler: POI '{poi?.name}' has Type=Docks but no DockShip component found on itself, its children, or its parents. Ensure DockShip is in the same hierarchy as the POI.".LogWarning();
                _activeDockShip = null;
                return;
            }

            $"SpecificUiHandler: HandleDockSelection for '{poi?.name}', found DockShip '{newDockShip.name}' (active previously '{_activeDockShip?.name ?? "<none>"}').".LogInfo();

            if (newDockShip == _activeDockShip)
            {
                $"SpecificUiHandler: DockShip already active, re-notifying visited to refresh UI.".LogInfo();
            }

            _activeDockShip = newDockShip;

            var welcomeOneShot = _activeDockShip.GetRandomWelcomeOneShot();
            if (!string.IsNullOrWhiteSpace(welcomeOneShot.Dialogue))
            {
                _waitingForShopEntryDialogue = true;
                SubscribeToConversationFinished();
            }

            _activeDockShip.NotifyShipVisited();
        }

        public void HandleInput(string action)
        {
            if (ActiveTutorialHandler != null)
            {
                ActiveTutorialHandler.HandleInput(action);
                return;
            }

            if (_waitingForShopEntryDialogue || _waitingForShopExitDialogue)
            {
                if (
                    action
                    is InputActionConstants.Cancel
                        or InputActionConstants.Back
                        or InputActionConstants.Submit
                        or InputActionConstants.Select
                )
                {
                    _subscribedController?.Advance();
                }
                return;
            }

            if (action is InputActionConstants.Back or InputActionConstants.Cancel)
            {
                if (_currentType == HubPoiType.MarketPOI)
                {
                    HandleMarketExit(action);
                }
                else if (_currentType == HubPoiType.DocksPOI)
                {
                    HandleDockShopBack(action);
                }
                else if (_currentType == HubPoiType.UnitPOI)
                {
                    HandleCharacterBack(action);
                }
            }
            if (action is InputActionConstants.NavigateRight or InputActionConstants.NavigateLeft)
            {
                if (_currentType == HubPoiType.MarketPOI)
                {
                    HandleMarketLeftRight(action);
                }
                else if (_currentType == HubPoiType.DocksPOI)
                {
                    HandleDockShopLeftRight(action);
                }
                else if (_currentType == HubPoiType.UnitPOI)
                {
                    HandleCharacterLeftRight(action);
                }
            }
            if (action is InputActionConstants.NavigateUp or InputActionConstants.NavigateDown)
            {
                if (_currentType == HubPoiType.MarketPOI)
                {
                    HandleMarketUpDown(action);
                }
                else if (_currentType == HubPoiType.DocksPOI)
                {
                    HandleDockShopUpDown(action);
                }
                else if (_currentType == HubPoiType.UnitPOI)
                {
                    HandleCharacterUpDown(action);
                }
            }
            if (action is InputActionConstants.Submit or InputActionConstants.Select)
            {
                if (_currentType == HubPoiType.MarketPOI)
                {
                    HandleMarketSelection(action);
                }
                else if (_currentType == HubPoiType.DocksPOI)
                {
                    HandleDockShopSelection(action);
                }
                else if (_currentType == HubPoiType.UnitPOI)
                {
                    HandleCharacterSelection(action);
                }
            }
            if (action is InputActionConstants.ScrollLeft or InputActionConstants.ScrollRight)
            {
                if (_currentType == HubPoiType.MarketPOI)
                {
                    HandleMarketPageChange(action);
                }
                else if (_currentType == HubPoiType.DocksPOI)
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

            if (_activeHubCharacter != null)
            {
                _activeHubCharacter.NotifyCharacterExited();
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
                // Fallback: keep current camera if no saved transform was captured.
            }

            _activeShop = null;
            _activeBlacksmith = null;
            _activeDockShip = null;
            _activeHubCharacter = null;
            hubManager?.MainOverlayUiFade?.Show();
            hubManager.RevertToPreviousInputMode();
        }
    }
}
