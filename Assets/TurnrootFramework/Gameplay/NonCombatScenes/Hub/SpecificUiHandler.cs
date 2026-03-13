using Turnroot.Utilities;
using UnityEngine;
using static Turnroot.Gameplay.NonCombatScenes.Hub.HubManager;

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
            $"SpecificUiHandler received input: {action}".LogInfo();
            if (action == "Back")
            {
                hubManager.CurrentSubLocation?.ResetCameraToCameraPoint();
                hubManager.RevertToPreviousInputMode();
            }
        }
    }
}
