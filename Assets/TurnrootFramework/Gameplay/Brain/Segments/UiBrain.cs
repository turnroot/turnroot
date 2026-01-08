using System.Collections;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.GameSettings;
using Turnroot.UI.Components.Menu;
using Turnroot.UI.Components.RadialMenu;
using Turnroot.UI.Components.SimpleButton;
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

        // New component instances - made private for better encapsulation
        private MenuTransitionManager _transitionManager;
        private SettingsBindingManager _settingsBindingManager;
        private MenuDepthTracker _menuTracker;
        private MenuRouteHandler _routeHandler;

        // Public property to access current pre-battle menu instance through MenuLocation system
        public GameObject CurrentPreBattleMenuInstance =>
            uiSettings?.GetPreBattleMenu()?.activeInstance;

        protected override EventPriority GetSubscriptionPriority() => EventPriority.Low;

        // Use MenuDepthTracker instead of manual tracking
        public int CurrentMenuDepth => _menuTracker?.CurrentDepth ?? 0;
        public bool IsInSubMenu => _menuTracker?.IsInSubMenu ?? false;

        // Public method for MenuRouteHandler and MenuTransitionManager
        public void PublishPreBattleCompleted() => _brain.PublishPreBattleCompleted();

        // Internal interface for service classes
        internal bool IsTransitioning => _isTransitioning;

        internal void SetTransitioning(bool value) => _isTransitioning = value;

        internal MonoBehaviour GetMonoBehaviour() => this;

        internal Turnroot.Gameplay.Brain.Brain GetBrain() => _brain;

        internal MenuDepthTracker GetMenuTracker() => _menuTracker;

        internal MenuTransitionManager GetTransitionManager() => _transitionManager;

        // Public methods for MenuTransitionManager and MenuRouteHandler to access
        public void SetupMenuInputActions(MenuBase menu) =>
            InputActionFactory.SetupMenuNavigation(menu);

        public void SetupSettingsUIBindings(GameObject instance) =>
            _settingsBindingManager?.BindSettings(
                instance,
                _brain.GetComponent<GamewideContextBrain>()
            );

        public void ApplyMenuColors(GameObject instance, MenuStyle style)
        {
            if (uiSettings == null)
            {
                return;
            }

            if (style == MenuStyle.Pie)
            {
                // Radial menus pull colors automatically
                return;
            }

            // Apply grid/list/filmstrip colors
            var buttons = instance.GetComponentsInChildren<UnityEngine.UI.Button>();
            foreach (var button in buttons)
            {
                var colorBlock = button.colors;
                colorBlock.normalColor = uiSettings.GridListFilmstripButtonNormalColor;
                colorBlock.highlightedColor = uiSettings.GridListFilmstripButtonHoveredColor;
                colorBlock.selectedColor = uiSettings.GridListFilmstripButtonSelectedColor;
                colorBlock.fadeDuration = uiSettings.ButtonTransitionDuration;
                button.colors = colorBlock;
            }
        }

        public void TransitionToSubmenu(MenuLocation from, MenuLocation to) =>
            TransitionToSubmenu(from, to, isBackNavigation: false);

        public void TransitionToSubmenu(MenuLocation from, MenuLocation to, bool isBackNavigation)
        {
            if (_isTransitioning)
            {
                return;
            }

            _isTransitioning = true;
            StartCoroutine(TransitionToSubmenuCoroutine(from, to, isBackNavigation));
        }

        private IEnumerator TransitionToSubmenuCoroutine(
            MenuLocation from,
            MenuLocation to,
            bool isBackNavigation = false
        )
        {
            if (!isBackNavigation)
            {
                _menuTracker?.TrackTransition(from, to);
            }

            // For back navigation, don't destroy the 'from' menu so we can return to it later
            // For forward navigation, we can destroy sub-menus but preserve main menus
            bool destroyFrom =
                !isBackNavigation
                && (
                    from == gameSettingsGraphicsLocation
                    || from == gameSettingsGameplayLocation
                    || from == gameSettingsAudioLocation
                    || from == gameSettingsControlsLocation
                );

            yield return _transitionManager.TransitionBetween(from, to, destroyFrom);
            _isTransitioning = false;
        }

        public void SetPreBattleMenuFadeSpeed(float fadeTime)
        {
            var preBattleMenuLocation = uiSettings?.GetPreBattleMenu();
            if (
                preBattleMenuLocation?.activeInstance != null
                && preBattleMenuLocation.activeInstance.TryGetComponent<UIFade>(out var uiFade)
            )
            {
                uiFade.lerpTime = fadeTime;
            }
        }

        public void HandleStartBattleClick()
        {
            var preBattleMenuLocation = uiSettings?.GetPreBattleMenu();
            if (preBattleMenuLocation?.activeInstance == null || _isTransitioning)
            {
                return;
            }

            // Play any center item effects (UITweener/UIEffect/etc.) before starting transition
            float effectDelay = PlayEffectsOnSelectedPrebattleCenter(
                preBattleMenuLocation.activeInstance
            );

            // Start a coroutine that waits for effect to play then transitions to battle
            StartCoroutine(StartBattleCoroutine(preBattleMenuLocation, effectDelay));
        }

        // Menu event handlers for route system
        public void HandleMenuNavigate(Turnroot.UI.Components.MenuItemBase item) =>
            _routeHandler?.HandleMenuNavigate(item);

        public void HandleMenuSelect(Turnroot.UI.Components.MenuItemBase item) =>
            _routeHandler?.HandleMenuSelect(item);

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

        private MenuLocation GetValidatedMenuLocation(
            System.Func<MenuLocation> getter,
            string menuName
        )
        {
            if (uiSettings == null)
            {
                Debug.LogError("UiBrain: GamewideUiSettings not found!");
                return null;
            }

            var location = getter();
            if (location == null)
            {
                Debug.LogError($"UiBrain: {menuName} menu location not found!");
            }

            return location;
        }

        protected void WarnPrefabs()
        {
            // Validate all menu locations
            GetValidatedMenuLocation(() => settingsMenuLocation, "Game settings");
            GetValidatedMenuLocation(() => gameSettingsGraphicsLocation, "Game settings graphics");
            GetValidatedMenuLocation(() => gameSettingsGameplayLocation, "Game settings gameplay");
            GetValidatedMenuLocation(() => gameSettingsAudioLocation, "Game settings audio");
            GetValidatedMenuLocation(() => gameSettingsControlsLocation, "Game settings controls");
            GetValidatedMenuLocation(() => preBattleMenuLocation, "Pre-battle");
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

        #region PreBattle UI Management

        public void HandlePreBattleUi()
        {
            var preBattleMenuLocation = GetValidatedMenuLocation(
                () => uiSettings?.GetPreBattleMenu(),
                "Pre-battle"
            );
            if (preBattleMenuLocation == null)
            {
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

            Debug.Log(
                $"UiBrain: Creating pre-battle menu instance from prefab {preBattleMenuLocation.prefab?.name}"
            );
            preBattleMenuLocation.activeInstance = Instantiate(preBattleMenuLocation.prefab);
            Debug.Log(
                $"UiBrain: Created pre-battle instance {preBattleMenuLocation.activeInstance?.name}"
            );
            if (!preBattleMenuLocation.activeInstance.TryGetComponent<UIFade>(out var uiFade))
            {
                uiFade = preBattleMenuLocation.activeInstance.AddComponent<UIFade>();
                uiFade.lerpTime = uiSettings.MenuFadeTime;
            }

            var menuStyle = preBattleMenuLocation.style;
            if (menuStyle == MenuStyle.Pie)
            {
                if (
                    preBattleMenuLocation.activeInstance.TryGetComponent<RadialMenu>(
                        out var radialMenu
                    )
                )
                {
                    radialMenu.uiBrain = this;
                    radialMenu.OnNavigate += HandlePreBattleMenuNavigate;
                    radialMenu.OnItemSelected += HandlePreBattleMenuSelect;
                    Debug.Log(
                        $"UiBrain: Attached prebattle handlers to radial instance {preBattleMenuLocation.activeInstance?.name}"
                    );
                }
            }
            else if (menuStyle == MenuStyle.Filmstrip)
            {
                // TODO: Set up filmstrip prebattle menu handling
            }
            else if (menuStyle == MenuStyle.List)
            {
                if (
                    preBattleMenuLocation.activeInstance.TryGetComponent<MenuBase>(out var listMenu)
                )
                {
                    listMenu.uiBrain = this;
                    listMenu.OnNavigate += HandlePreBattleMenuNavigate;
                    listMenu.OnItemSelected += HandlePreBattleMenuSelect;
                }
            }
            else if (menuStyle == MenuStyle.Grid)
            {
                // TODO: Set up grid prebattle menu handling
            }
        }

        #endregion

        #region Battle UI Management

        public void HandleBattleUi()
        {
#if UNITY_EDITOR
            Debug.Log("UiBrain: Handling battle UI setup");
#endif
            // Battle UI initialization logic will be added here
            // For now, just log that we're in battle state
        }

        #endregion

        #region Back Button Management

        private void HandleBackButtonForState(string stateName)
        {
            bool needsBackButton = System.Array.Exists(
                StateBrain.StatesThatNeedMenus,
                state => state == stateName
            );

            if (needsBackButton && _currentMenuCanvasPrefab == null)
            {
                CreateBackButton();
            }
            else if (!needsBackButton && _currentMenuCanvasPrefab != null)
            {
                DestroyBackButton();
            }
        }

        private void CreateBackButton()
        {
            if (uiSettings?.MenuCanvasPrefab == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("UiBrain: MenuCanvasPrefab is not set in GamewideUiSettings");
#endif
                return;
            }

            _currentMenuCanvasPrefab = Instantiate(uiSettings.MenuCanvasPrefab);

            // Wire up the back button to handle menu navigation
            // Find the SimpleButton component in children since it's a child of the canvas
            var simpleButton = _currentMenuCanvasPrefab.GetComponentInChildren<SimpleButton>();
            if (simpleButton != null && simpleButton.Role == SimpleButtonRole.Back)
            {
                simpleButton.OnSelected += HandleBackButtonPressed;
            }
            else
            {
                // TODO: Handle other button types
            }
        }

        private void DestroyBackButton()
        {
            if (_currentMenuCanvasPrefab != null)
            {
                // Clean up event subscription
                var simpleButton = _currentMenuCanvasPrefab.GetComponentInChildren<SimpleButton>();
                if (simpleButton != null && simpleButton.Role == SimpleButtonRole.Back)
                {
                    simpleButton.OnSelected -= HandleBackButtonPressed;
                }

                Destroy(_currentMenuCanvasPrefab);
                _currentMenuCanvasPrefab = null;
            }
        }

        #endregion

        #region Menu Navigation

        private void HandleBackButtonPressed()
        {
            if (_isTransitioning)
            {
                return;
            }

            if (_menuTracker?.CanGoBack() == true)
            {
                var (fromLocation, toLocation) = _menuTracker.PopTransition();
                if (fromLocation != null && toLocation != null)
                {
                    TransitionToSubmenu(fromLocation, toLocation, isBackNavigation: true);
                }
            }
            else
            {
                // At root level, handle based on current state
                HandleRootLevelBack();
            }
        }

        private void HandleRootLevelBack()
        {
            var currentState = Brain?.stateBrain?.CurrentState?.Name;

            // TODO: Implement root level back behavior based on state
            switch (currentState)
            {
                case BrainStateNames.PreBattle:
                    // TODO: Return to previous state or world map
                    break;
                case BrainStateNames.Paused:
                    Brain.stateBrain.Resume();
                    break;
                case BrainStateNames.MainMenu:
                    // TODO: Exit game or return to previous screen
                    break;
                default:
                    break;
            }

#if UNITY_EDITOR
            Debug.Log($"UiBrain: Root level back pressed in state: {currentState}");
#endif
        }

        #endregion
    }
}
