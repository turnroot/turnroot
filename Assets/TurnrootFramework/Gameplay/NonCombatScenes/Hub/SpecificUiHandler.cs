using Turnroot.Conversations;
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

        // Track the active shop for notifying visited/exited events.
        private Shop.Shop _activeShop;

        // Used to pause the back/exit behavior until the shop exit dialogue finishes.
        private bool _waitingForShopExitDialogue;

        private void Awake()
        {
            hubManager = GetComponent<HubManager>();
        }

        private void OnEnable()
        {
            if (ConversationController.Instance != null)
            {
                ConversationController.Instance.OnAnyConversationFinished.AddListener(
                    OnConversationFinished
                );
            }
        }

        private void OnDisable()
        {
            if (ConversationController.Instance != null)
            {
                ConversationController.Instance.OnAnyConversationFinished.RemoveListener(
                    OnConversationFinished
                );
            }
        }

        private void OnConversationFinished()
        {
            if (!_waitingForShopExitDialogue)
            {
                return;
            }

            _waitingForShopExitDialogue = false;
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

            // If the newly selected POI is a shop, mark it as active and play the welcome dialogue.
            var newShop = poi?.GetComponent<Shop.Shop>();
            if (newShop != null && newShop != _activeShop)
            {
                _activeShop = newShop;
                _activeShop.NotifyShopVisited();

                var welcome = _activeShop.GetRandomWelcomeOneShot();
                if (!string.IsNullOrWhiteSpace(welcome.Dialogue))
                {
                    ConversationController.Instance?.PlayOneShot(welcome);
                }
            }
            else if (newShop == null)
            {
                // Clear active shop if we are selecting a non-shop POI.
                _activeShop = null;
            }
        }

        public void HandleInput(string action)
        {
            if (_waitingForShopExitDialogue)
            {
                // Ignore input while waiting for the exit dialogue to finish.
                return;
            }

            if (action == "Back")
            {
                // If we are currently inside a shop, play the exit dialogue first.
                if (_activeShop != null)
                {
                    // Decide whether we have an exit dialogue to play.
                    var exitOneShot = _activeShop.GetRandomFarewellOneShot();
                    bool hasExitDialogue = !string.IsNullOrWhiteSpace(exitOneShot.Dialogue);

                    _activeShop.NotifyShopExited();

                    if (hasExitDialogue && ConversationController.Instance != null)
                    {
                        _waitingForShopExitDialogue = true;
                        ConversationController.Instance.PlayOneShot(exitOneShot);
                        return;
                    }

                    // If there's no exit dialogue, fall through and perform the normal exit behavior.
                }

                CompleteShopExit();
            }
        }

        private void CompleteShopExit()
        {
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
            hubManager.RevertToPreviousInputMode();
        }
    }
}
