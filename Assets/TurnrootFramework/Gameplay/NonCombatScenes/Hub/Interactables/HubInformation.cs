using System.Collections.Generic;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public class HubInformation : HubFadableVisualBase, IPageHandler, IDistanceVisibilityHandler
    {
        [Tooltip("Avatar transform used to decide whether this interaction should be visible.")]
        public Transform AvatarPosition;

        [Tooltip("When hidden, show once the avatar gets within this distance.")]
        public float ShowDistance = 8f;

        [Tooltip("When visible, stay visible until the avatar exceeds this distance.")]
        public float HideDistance = 10f;

        [Tooltip("If true, hide this interaction when AvatarPosition is not assigned.")]
        public bool HideWhenAvatarMissing = true;

        [Tooltip("Ordered list of UIFade panels to display as information pages.")]
        public List<UIFade> Pages = new();

        private bool _isVisible;
        private bool _missingAvatarWarningLogged;
        private int _currentIndex = -1;
        private SpecificUiHandler _specificUiHandler;

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

        public int CurrentPageIndex
        {
            get => _currentIndex;
            set => _currentIndex = value;
        }

        public int PageCount => Pages?.Count ?? 0;

        private void Awake()
        {
            if (poiVisual == null)
            {
                $"HubInformation on {gameObject.name} has no poiVisual assigned, disabling.".LogWarning();
                enabled = false;
                return;
            }

            EnsureSpecificUiHandler();
            InitializeVisualMaterials();
            Hide();
            _isVisible = false;
        }

        private void Update()
        {
            FaceCamera();
            this.UpdateDistanceVisibility();
        }

        private void OnDisable() => ClosePageSequence();

        public void Select()
        {
            if (!EnsureSpecificUiHandler())
            {
                "HubInformation: Could not find SpecificUiHandler in scene.".LogWarning();
                return;
            }

            _specificUiHandler.ActivePageHandler = this;
            this.BeginPageSequence();
        }

        public UIFade GetPageFade(int index) => Pages[index];

        public void OnPageShown(int index) { }

        public void OnPagesCompleted() => ClosePageSequence();

        private bool EnsureSpecificUiHandler()
        {
            if (_specificUiHandler != null)
            {
                return true;
            }

            _specificUiHandler = FindFirstObjectByType<SpecificUiHandler>();
            return _specificUiHandler != null;
        }

        private void ClosePageSequence()
        {
            this.HideAllPages();
            _currentIndex = -1;

            if (
                _specificUiHandler != null
                && ReferenceEquals(_specificUiHandler.ActivePageHandler, this)
            )
            {
                _specificUiHandler.ActivePageHandler = null;
            }
        }
    }
}
