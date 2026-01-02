using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.GameSettings;
using Turnroot.UI.Components.RadialMenu;
using Turnroot.Utilities;
using UnityEngine;

namespace TurnrootFramework.Gameplay.Brain.Segments
{
    public partial class UiBrain : BrainComponent
    {
        [HideInInspector]
        public GamewideUiSettings uiSettings;

        private bool _isTransitioning = false;

        // Public property to access current pre-battle menu instance through MenuLocation system
        public GameObject CurrentPreBattleMenuInstance =>
            uiSettings?.GetPreBattleMenu()?.activeInstance;

        protected override EventPriority GetSubscriptionPriority() => EventPriority.Low;

        [HideInInspector]
        public int CurrentMenuDepth = 0;

        [HideInInspector]
        public bool IsInSubMenu => CurrentMenuDepth > 0;

        [HideInInspector]
        protected override void Awake()
        {
            base.Awake();
            uiSettings = GameSettingsLoader.LoadFirst<GamewideUiSettings>();
            if (uiSettings == null)
            {
#if UNITY_EDITOR
                Debug.LogError("UiBrain: GamewideUiSettings not found!");
#endif
            }
        }

        private System.Action<BrainState> _onStateChangedHandler;

        protected override void SubscribeToBrainEvents()
        {
            _onStateChangedHandler = (state) =>
            {
                var name = state?.Name ?? string.Empty;
#if UNITY_EDITOR
                Debug.Log($"UiBrain: Brain state changed to {name}");
#endif
                switch (name)
                {
                    case BrainStateNames.PreBattle:
                        HandlePreBattleUi();
                        break;
                }
            };

            Brain.OnStateChanged += _onStateChangedHandler;
            // If the Brain already has an active state, invoke handler immediately so UI can react to the current state
            var current = Brain?.stateBrain?.CurrentState;
            if (current != null)
            {
                _onStateChangedHandler(current);
            }
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            if (_onStateChangedHandler != null)
            {
                Brain.OnStateChanged -= _onStateChangedHandler;
                _onStateChangedHandler = null;
            }

            // Clean up radial menu events if menu still exists
            var preBattleMenuLocation = uiSettings?.GetPreBattleMenu();
            if (preBattleMenuLocation?.activeInstance != null)
            {
                var radialMenu = preBattleMenuLocation.activeInstance.GetComponent<RadialMenu>();
                if (radialMenu != null)
                {
                    radialMenu.OnNavigate -= HandlePreBattleMenuNavigate;
                    radialMenu.OnItemSelected -= HandlePreBattleMenuSelect;
                }
            }
        }

        public void HandlePreBattleUi()
        {
            if (uiSettings == null)
            {
#if UNITY_EDITOR
                Debug.LogError("UiBrain: Cannot create pre-battle UI - uiSettings is null");
#endif
                return;
            }

            var preBattleMenuLocation = uiSettings.GetPreBattleMenu();
            if (preBattleMenuLocation == null)
            {
#if UNITY_EDITOR
                Debug.LogError("UiBrain: Pre-battle menu location not found");
#endif
                return;
            }

            // Guard: Return early if activeInstance already exists to prevent duplicates
            if (preBattleMenuLocation.activeInstance != null)
            {
                return;
            }

            if (preBattleMenuLocation.prefab == null)
            {
#if UNITY_EDITOR
                Debug.LogError("UiBrain: No prefab set for pre-battle menu location");
#endif
                return;
            }

            preBattleMenuLocation.activeInstance = Instantiate(preBattleMenuLocation.prefab);
            var uiFade = preBattleMenuLocation.activeInstance.AddComponent<UIFade>();
            uiFade.lerpTime = uiSettings.MenuFadeTime;

            var menuStyle = preBattleMenuLocation.style;
            if (menuStyle == MenuStyle.Pie)
            {
                var radialMenu = preBattleMenuLocation.activeInstance.GetComponent<RadialMenu>();
                if (radialMenu != null)
                {
                    radialMenu.uiBrain = this;
                    radialMenu.OnNavigate += HandlePreBattleMenuNavigate;
                    radialMenu.OnItemSelected += HandlePreBattleMenuSelect;
                }
            }
            else if (menuStyle == MenuStyle.Filmstrip)
            {
                // TODO: Set up filmstrip prebattle menu handling
            }
            else if (menuStyle == MenuStyle.List)
            {
                // TODO: Set up list prebattle menu handling
            }
            else if (menuStyle == MenuStyle.Grid)
            {
                // TODO: Set up grid prebattle menu handling
            }
        }
    }
}
