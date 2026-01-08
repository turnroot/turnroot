using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.GameSettings;
using Turnroot.UI.Components.Menu;
using Turnroot.UI.Components.RadialMenu;
using Turnroot.Utilities;
using UnityEngine;

namespace TurnrootFramework.Gameplay.Brain.Segments
{
    public partial class UiBrain : BrainComponent
    {
        #region Fields and Properties

        [HideInInspector]
        public GamewideUiSettings uiSettings;

        [HideInInspector]
        public MenuLocation settingsMenuLocation;

        [HideInInspector]
        public MenuLocation gameSettingsGraphicsLocation;

        [HideInInspector]
        public MenuLocation gameSettingsGameplayLocation;

        [HideInInspector]
        public MenuLocation gameSettingsAudioLocation;

        [HideInInspector]
        public MenuLocation gameSettingsControlsLocation;

        [HideInInspector]
        public MenuLocation preBattleMenuLocation;

        [HideInInspector]
        public bool _isTransitioning = false;
        private GameObject _currentMenuCanvasPrefab;
        private MenuTransitionManager _transitionManager;
        private SettingsBindingManager _settingsBindingManager;
        private MenuDepthTracker _menuTracker;
        private MenuRouteHandler _routeHandler;
        public GameObject CurrentPreBattleMenuInstance =>
            uiSettings?.GetPreBattleMenu()?.activeInstance;

        protected override EventPriority GetSubscriptionPriority() => EventPriority.Low;

        public int CurrentMenuDepth => _menuTracker?.CurrentDepth ?? 0;
        public bool IsInSubMenu => _menuTracker?.IsInSubMenu ?? false;

        public void PublishPreBattleCompleted() => _brain.PublishPreBattleCompleted();

        internal bool IsTransitioning => _isTransitioning;

        internal void SetTransitioning(bool value) => _isTransitioning = value;

        internal MonoBehaviour GetMonoBehaviour() => this;

        internal Turnroot.Gameplay.Brain.Brain GetBrain() => _brain;

        internal MenuDepthTracker GetMenuTracker() => _menuTracker;

        internal MenuTransitionManager GetTransitionManager() => _transitionManager;

        #endregion

        #region Unity Lifecycle and Initialization

        protected override void Awake()
        {
            base.Awake();
            uiSettings = GameSettingsLoader.LoadFirst<GamewideUiSettings>();
            if (uiSettings != null)
            {
                preBattleMenuLocation = uiSettings.GetPreBattleMenu();
                settingsMenuLocation = uiSettings.GetGameSettingsMenu();
                gameSettingsGraphicsLocation = uiSettings.GetGameSettingsGraphicsMenu();
                gameSettingsGameplayLocation = uiSettings.GetGameSettingsGameplayMenu();
                gameSettingsAudioLocation = uiSettings.GetGameSettingsAudioMenu();
                gameSettingsControlsLocation = uiSettings.GetGameSettingsControlsMenu();

                // Initialize new components
                _transitionManager = new MenuTransitionManager(this, uiSettings);
                _settingsBindingManager = new SettingsBindingManager();
                _menuTracker = new MenuDepthTracker();
                _routeHandler = new MenuRouteHandler(this);
            }

#if UNITY_EDITOR
            WarnPrefabs();
#endif
        }

        #endregion

        #region Brain Events and State Management

        private System.Action<BrainState> _onStateChangedHandler;

        protected override void SubscribeToBrainEvents()
        {
            Brain.OnBattleCursorMoved += HandleBattleCursorMoved;
            _onStateChangedHandler = (state) =>
            {
                var name = state?.Name ?? string.Empty;

                // Handle back button based on state
                HandleBackButtonForState(name);

                switch (name)
                {
                    case BrainStateNames.PreBattle:
                        HandlePreBattleUi();
                        break;
                    case BrainStateNames.Battle:
                        HandleBattleUi();
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
            Brain.OnBattleCursorMoved -= HandleBattleCursorMoved;

            if (_onStateChangedHandler != null)
            {
                Brain.OnStateChanged -= _onStateChangedHandler;
                _onStateChangedHandler = null;
            }

            // Clean up menu events if menu still exists
            if (preBattleMenuLocation?.activeInstance != null)
            {
                if (
                    preBattleMenuLocation.activeInstance.TryGetComponent<RadialMenu>(
                        out var radialMenu
                    )
                )
                {
                    radialMenu.OnNavigate -= HandlePreBattleMenuNavigate;
                    radialMenu.OnItemSelected -= HandlePreBattleMenuSelect;
                }

                if (
                    preBattleMenuLocation.activeInstance.TryGetComponent<MenuBase>(out var listMenu)
                )
                {
                    listMenu.OnNavigate -= HandlePreBattleMenuNavigate;
                    listMenu.OnItemSelected -= HandlePreBattleMenuSelect;
                }
            }

            // Clean up back button
            DestroyBackButton();
        }

        #endregion
    }
}
