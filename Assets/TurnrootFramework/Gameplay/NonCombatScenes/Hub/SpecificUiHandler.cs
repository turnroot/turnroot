using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [RequireComponent(typeof(HubManager))]
    public class SpecificUiHandler : MonoBehaviour
    {
        private HubManager hubManager;

        public HubSubLocation CurrentSubLocation { get; private set; }
        public HubPoiUi CurrentPoi { get; private set; }

        private void Awake()
        {
            hubManager = GetComponent<HubManager>();
        }

        public void SetCurrentSelection(HubSubLocation subLocation, HubPoiUi poi)
        {
            CurrentSubLocation = subLocation;
            CurrentPoi = poi;
            $"SpecificUiHandler: Selected POI '{poi?.name ?? "<null>"}' in sublocation '{subLocation?.LocationName.ToString() ?? "<none>"}'".LogInfo();
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
