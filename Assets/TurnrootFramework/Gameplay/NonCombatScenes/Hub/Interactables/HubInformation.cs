using System.Collections.Generic;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [RequireComponent(typeof(Collider))]
    public class HubInformation : HubFadableVisualBase, ILookTargetable, IPageHandler
    {
        [Tooltip("Ordered list of UIFade panels to display as information pages.")]
        public List<UIFade> Pages = new();

        [Tooltip("Maximum distance at which the player can highlight this node by looking at it.")]
        public float LookDistance = 8f;

        private int _currentIndex = -1;
        private SpecificUiHandler _specificUiHandler;
        private HubManager _hubManager;

        public int CurrentPageIndex
        {
            get => _currentIndex;
            set => _currentIndex = value;
        }

        public int PageCount => Pages?.Count ?? 0;

        public bool CanSelect => enabled && PageCount > 0;

        float ILookTargetable.LookDistance => LookDistance;

        private void Awake()
        {
            _hubManager = FindFirstObjectByType<HubManager>();

            if (poiVisual == null)
            {
                $"HubInformation on {gameObject.name} has no poiVisual assigned, disabling.".LogWarning();
                enabled = false;
                return;
            }

            EnsureSpecificUiHandler();
            InitializeVisualMaterials();
            Hide();
        }

        private void OnDisable() => ClosePageSequence();

        private void Update() => FaceCamera();

        public void Select()
        {
            if (!EnsureSpecificUiHandler())
            {
                "HubInformation: Could not find SpecificUiHandler in scene.".LogWarning();
                return;
            }

            if (!CanSelect)
            {
                $"HubInformation on {gameObject.name} has no pages assigned.".LogWarning();
                return;
            }

            _hubManager?.SetInputMode(HubManager.HubInputMode.Chosen);

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

            _hubManager?.RevertToPreviousInputMode();
        }
    }
}
