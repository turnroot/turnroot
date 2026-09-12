using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public class HubSimpleInteraction : HubFadableVisualBase, IDistanceVisibilityHandler
    {
        public Transform AvatarPosition;

        public float ShowDistance = 8f;
        public float HideDistance = 10f;

        public bool IsDistanceVisible { get; set; }

        Transform IDistanceVisibilityHandler.AvatarPosition => AvatarPosition;
        float IDistanceVisibilityHandler.ShowDistance => ShowDistance;
        float IDistanceVisibilityHandler.HideDistance => HideDistance;

        public Vector3 DistanceVisibilityPosition => transform.position;

        public string DistanceVisibilityOwnerName => gameObject.name;

        private void Awake()
        {
            if (poiVisual == null)
            {
                $"HubSimpleInteraction on {gameObject.name} has no poiVisual assigned, disabling.".LogWarning();
                enabled = false;
                return;
            }

            InitializeVisualMaterials();
            Hide();
            IsDistanceVisible = false;
        }

        private void Update()
        {
            FaceCamera();
            this.UpdateDistanceVisibility();
        }
    }
}
