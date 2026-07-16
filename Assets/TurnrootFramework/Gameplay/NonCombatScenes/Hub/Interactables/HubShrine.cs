using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [RequireComponent(typeof(Collider))]
    public class HubShrine : HubFadableVisualBase, ILookTargetable
    {
        public float LookDistance = 8f;

        private SpecificUiHandler _specificUiHandler;
        private HubManager _hubManager;

        public bool CanSelect => enabled;
        float ILookTargetable.LookDistance => LookDistance;

        bool IHubSelectable.CanSelect => CanSelect;

        private void Awake()
        {
            _hubManager = HubManager.GetCurrent();

            if (poiVisual == null)
            {
                $"HubShrine on {gameObject.name} has no poiVisual assigned, disabling.".LogWarning();
                enabled = false;
                return;
            }

            EnsureSpecificUiHandler();
            InitializeVisualMaterials();
            Hide();
        }

        private void Update() => FaceCamera();

        public void Select()
        {
            if (!EnsureSpecificUiHandler())
            {
                $"HubShrine: Could not find SpecificUiHandler in scene.".LogWarning();
                return;
            }

            PlayPoiSelectSound();
            _hubManager.SetInputMode(HubManager.HubInputMode.Chosen);
        }

        private bool EnsureSpecificUiHandler()
        {
            if (_specificUiHandler != null)
            {
                return true;
            }

            _specificUiHandler = FindFirstObjectByType<SpecificUiHandler>();
            return _specificUiHandler != null;
        }
    }
}
