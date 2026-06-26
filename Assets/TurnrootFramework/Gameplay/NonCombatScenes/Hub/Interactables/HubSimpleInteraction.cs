using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public class HubSimpleInteraction : HubFadableVisualBase, IDistanceVisibilityHandler
    {
        [Tooltip("Avatar transform used to decide whether this interaction should be visible.")]
        public Transform AvatarPosition;

        [Tooltip("When hidden, show once the avatar gets within this distance.")]
        public float ShowDistance = 8f;

        [Tooltip("When visible, stay visible until the avatar exceeds this distance.")]
        public float HideDistance = 10f;

        [Tooltip("If true, hide this interaction when AvatarPosition is not assigned.")]
        public bool HideWhenAvatarMissing = true;

        private bool _isVisible;
        private bool _missingAvatarWarningLogged;

        public bool IsDistanceVisible
        {
            get => _isVisible;
            set => _isVisible = value;
        }

        public bool MissingAvatarWarningLogged
        {
            get => _missingAvatarWarningLogged;
            set => _missingAvatarWarningLogged = value;
        }

        Transform IDistanceVisibilityHandler.AvatarPosition => AvatarPosition;
        float IDistanceVisibilityHandler.ShowDistance => ShowDistance;
        float IDistanceVisibilityHandler.HideDistance => HideDistance;
        bool IDistanceVisibilityHandler.HideWhenAvatarMissing => HideWhenAvatarMissing;

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
            _isVisible = false;
        }

        private void Update()
        {
            FaceCamera();
            this.UpdateDistanceVisibility();
        }
    }
}
