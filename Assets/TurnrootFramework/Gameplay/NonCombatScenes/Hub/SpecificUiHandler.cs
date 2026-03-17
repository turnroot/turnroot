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

        private void Awake()
        {
            hubManager = GetComponent<HubManager>();
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
        }

        public void HandleInput(string action)
        {
            if (action == "Back")
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

                hubManager.RevertToPreviousInputMode();
            }
        }
    }
}
