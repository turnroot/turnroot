using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Gameplay.PlayerSettings;
using Turnroot.GameSettings;
using Turnroot.UI.Components.Menu;
using Turnroot.UI.Components.RadialMenu;
using Turnroot.UI.Components.SimpleButton;
using Turnroot.Utilities;
using UnityEngine;

namespace TurnrootFramework.Gameplay.Brain.Segments
{
    [RequireComponent(typeof(CursorBrain))]
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
        public MenuLocation prebattleMapMenuLocation;

        [HideInInspector]
        public MenuLocation prebattleUnitsMenuLocation;

        [HideInInspector]
        public bool _isTransitioning = false;
        private GameObject _currentMenuCanvasPrefab;
        private GameObject _currentDetailsCanvasPrefab;
        private MenuTransitionManager _transitionManager;
        private SettingsBindingManager _settingsBindingManager;
        private MenuDepthTracker _menuTracker;
        private MenuRouteHandler _routeHandler;
        private GameplayPlayerSettings _playerSettings;
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
                prebattleMapMenuLocation = uiSettings.GetPrebattleMapMenu();
                prebattleUnitsMenuLocation = uiSettings.GetPrebattleUnitsMenu();

                // Initialize new components
                _transitionManager = new MenuTransitionManager(this, uiSettings);
                _settingsBindingManager = new SettingsBindingManager();
                _menuTracker = new MenuDepthTracker();
                _routeHandler = new MenuRouteHandler(this);

                // Subscribe to binding changes so we can rewire inputs at runtime
                _playerSettings = GameSettingsLoader.LoadFirst<GameplayPlayerSettings>();
                if (_playerSettings != null)
                {
                    _playerSettings.BindingsChanged += OnBindingsChanged;
                }
                var CursorBrain = GetComponent<CursorBrain>();
                CursorBrain.SetUiSettingsReference(uiSettings);
            }

#if UNITY_EDITOR
            WarnPrefabs();
#endif
        }

        #endregion

        #region Brain Events and State Management

        private void OnBindingsChanged()
        {
            // Rewire inputs for all menus and role buttons when bindings change
            try
            {
                // Rebind every MenuBase found in the scene
                var menus = UnityEngine.Object.FindObjectsByType<MenuBase>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );
                foreach (var menu in menus)
                {
                    // Recreate the input actions for the menu
                    SetupMenuInputActions(menu);

                    // Reassign select actions for child SimpleButton components to the new menu.selectAction
                    var simpleButtons = menu.GetComponentsInChildren<SimpleButton>(true);
                    foreach (var sb in simpleButtons)
                    {
                        try
                        {
                            if (sb.SelectAction != null)
                            {
                                sb.SelectAction.Disable();
                                sb.SelectAction.Dispose();
                            }
                        }
                        catch { }

                        sb.AssignSelectAction(menu.selectAction);
                    }
                }

                // Reassign Back/Details buttons instantiated on menu canvases
                ReassignRoleButton(_currentMenuCanvasPrefab);
                ReassignRoleButton(_currentDetailsCanvasPrefab);
            }
            catch (System.Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogError($"UiBrain: Error rebinding inputs: {ex.Message}");
#endif
            }
        }

        private void ReassignRoleButton(GameObject canvas)
        {
            if (canvas == null)
            {
                return;
            }

            var sb = canvas.GetComponentInChildren<SimpleButton>();
            if (sb == null)
            {
                return;
            }

            try
            {
                if (sb.SelectAction != null)
                {
                    sb.SelectAction.Disable();
                    sb.SelectAction.Dispose();
                }
            }
            catch { }

            if (sb.Role == SimpleButtonRole.Back)
            {
                sb.AssignSelectAction(InputActionFactory.CreateBack());
            }
            else if (sb.Role == SimpleButtonRole.Details)
            {
                sb.AssignSelectAction(InputActionFactory.CreateDetails());
            }
        }

        private System.Action<BrainState> _onStateChangedHandler;

        protected override void SubscribeToBrainEvents()
        {
            Brain.OnBattleCursorMoved += HandleBattleCursorMoved;
            _onStateChangedHandler = (state) =>
            {
                var name = state?.Name ?? string.Empty;

                // Handle back and details buttons based on state
                HandleButtonsForState(name);

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
                    radialMenu.OnItemSelected -= HandlePreBattleMenuSelect;
                }

                if (
                    preBattleMenuLocation.activeInstance.TryGetComponent<MenuBase>(out var listMenu)
                )
                {
                    listMenu.OnItemSelected -= HandlePreBattleMenuSelect;
                }
            }

            // Clean up back and details buttons
            DestroyBackButton();
            DestroyDetailsButton();
        }

        #endregion
    }
}
