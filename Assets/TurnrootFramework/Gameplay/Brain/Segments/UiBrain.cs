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
        public GameObject PreBattleMenuInstance { get; private set; }

        private MenuStyle _preBattleMenuStyle;

        [HideInInspector]
        public GamewideUiSettings uiSettings;

        private bool _isTransitioning = false;

        protected override EventPriority GetSubscriptionPriority() => EventPriority.Low;

        protected override void Awake()
        {
            base.Awake();
            uiSettings = GameSettingsLoader.LoadFirst<GamewideUiSettings>();
            if (uiSettings != null)
            {
                _preBattleMenuStyle = uiSettings.BattlePreparationMenuStyle;
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogError("UiBrain: GamewideUiSettings not found! Using default menu style.");
#endif
                _preBattleMenuStyle = MenuStyle.Pie; // Default fallback
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
            if (PreBattleMenuInstance != null)
            {
                var radialMenu = PreBattleMenuInstance.GetComponent<RadialMenu>();
                if (radialMenu != null)
                {
                    radialMenu.OnNavigate -= HandlePreBattleMenuNavigate;
                    radialMenu.OnItemSelected -= HandlePreBattleMenuSelect;
                }
            }
        }

        public void HandlePreBattleUi()
        {
            // Guard: Return early if PreBattleMenuInstance already exists to prevent duplicates
            if (PreBattleMenuInstance != null)
            {
                return;
            }

            // Instantiate the pre-battle menu prefab
            if (uiSettings.PreBattleMenuPrefab != null)
            {
                PreBattleMenuInstance = Instantiate(uiSettings.PreBattleMenuPrefab);
                var uiFade = PreBattleMenuInstance.AddComponent<UIFade>();
                uiFade.lerpTime = uiSettings.MenuFadeTime;
                if (_preBattleMenuStyle == MenuStyle.Pie)
                {
                    var radialMenu = PreBattleMenuInstance.GetComponent<RadialMenu>();
                    radialMenu.uiBrain = this;
                    radialMenu.OnNavigate += HandlePreBattleMenuNavigate;
                    radialMenu.OnItemSelected += HandlePreBattleMenuSelect;
                }
                else if (_preBattleMenuStyle == MenuStyle.Filmstrip)
                {
                    // TODO: Set up filmstrip prebattle menu handling
                }
                else if (_preBattleMenuStyle == MenuStyle.List)
                {
                    // TODO: Set up list prebattle menu handling
                }
                else if (_preBattleMenuStyle == MenuStyle.Grid)
                {
                    // TODO: Set up grid prebattle menu handling
                }
            }
        }
    }
}
