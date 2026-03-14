using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [RequireComponent(typeof(HubManager))]
    public class SpecificUiHandler : MonoBehaviour
    {
        private HubManager hubManager;

        private void Awake()
        {
            hubManager = GetComponent<HubManager>();
        }

        public void HandleInput(string action)
        {
            if (action == "Back")
            {
                hubManager.CurrentSubLocation?.ResetCameraToCameraPoint();
                hubManager.RevertToPreviousInputMode();
            }
        }
    }
}
