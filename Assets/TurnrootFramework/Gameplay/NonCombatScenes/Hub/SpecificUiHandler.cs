using Turnroot.Conversations;
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

        // Track the active shop for notifying visited/exited events.
        private Shop.Shop _activeShop;

        // Used to pause the back/exit behavior until the shop exit dialogue finishes.
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

            // If the newly selected POI is a shop, mark it as active and play the welcome dialogue.
            var newShop = poi?.GetComponent<Shop.Shop>();
            if (newShop != null && newShop != _activeShop)
            {
                _activeShop = newShop;
                // NotifyShopVisited already plays the welcome dialogue internally.
                _activeShop.NotifyShopVisited();
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
                    // Check whether the shop has a farewell dialogue.
                    var exitOneShot = _activeShop.GetRandomFarewellOneShot();
                    bool hasExitDialogue = !string.IsNullOrWhiteSpace(exitOneShot.Dialogue);

                    // NotifyShopExited already plays the farewell dialogue internally.
                    _activeShop.NotifyShopExited();

                    if (hasExitDialogue)
                    {
                        // Subscribe now — guaranteed ConversationController.Instance exists
                        // because NotifyShopExited just used it to play the dialogue.
                        _waitingForShopExitDialogue = true;
                        SubscribeToConversationFinished();
                        return;
                    }

                    // If there's no exit dialogue, fall through and perform the normal exit behavior.
                }

                CompleteShopExit();
            }
        }

        private void CompleteShopExit()
        {
            // Hide the shop UI before clearing the shop reference.
            if (_activeShop != null && _activeShop.TryGetComponent<Shop.ShopUi>(out var shopUi))
            {
                shopUi.ShopUiFade.Hide();
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
            hubManager.RevertToPreviousInputMode();
        }
    }
}
