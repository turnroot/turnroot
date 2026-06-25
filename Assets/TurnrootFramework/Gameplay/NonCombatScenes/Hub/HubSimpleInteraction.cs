using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public class HubSimpleInteraction : HubFadableVisualBase
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
            UpdateDistanceVisibility();
        }

        private void UpdateDistanceVisibility()
        {
            if (AvatarPosition == null)
            {
                if (HideWhenAvatarMissing && _isVisible)
                {
                    Hide();
                    _isVisible = false;
                }
                return;
            }

            float showDistance = Mathf.Max(0f, ShowDistance);
            float hideDistance = Mathf.Max(showDistance, HideDistance);
            float sqrDistance = (transform.position - AvatarPosition.position).sqrMagnitude;

            bool shouldShow = _isVisible
                ? sqrDistance <= (hideDistance * hideDistance)
                : sqrDistance <= (showDistance * showDistance);

            if (shouldShow == _isVisible)
            {
                return;
            }

            if (shouldShow)
            {
                Show();
            }
            else
            {
                Hide();
            }

            _isVisible = shouldShow;
        }
    }
}
