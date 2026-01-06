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

        private bool _isTransitioning = false;
        private GameObject _currentMenuCanvasPrefab;

        // Public property to access current pre-battle menu instance through MenuLocation system
        public GameObject CurrentPreBattleMenuInstance =>
            uiSettings?.GetPreBattleMenu()?.activeInstance;

        protected override EventPriority GetSubscriptionPriority() => EventPriority.Low;

        [HideInInspector]
        public int CurrentMenuDepth = 0;

        [HideInInspector]
        public bool IsInSubMenu => CurrentMenuDepth > 0;

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
            }

#if UNITY_EDITOR
            WarnPrefabs();
#endif
        }

        protected void WarnPrefabs()
        {
            if (uiSettings == null)
            {
                Debug.LogError("UiBrain: GamewideUiSettings not found!");
                return; // Don't check other things if uiSettings is null
            }

            if (settingsMenuLocation == null)
            {
                Debug.LogError("UiBrain: Game settings menu location not found!");
            }

            if (gameSettingsGraphicsLocation == null)
            {
                Debug.LogError("UiBrain: Game settings graphics menu location not found!");
            }

            if (preBattleMenuLocation == null)
            {
                Debug.LogError("UiBrain: Pre-battle menu location not found!");
            }
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
#if UNITY_EDITOR
                Debug.Log($"UiBrain: Brain state changed to {name}");
#endif
                // Handle back button based on state
                HandleBackButtonForState(name);

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
#if UNITY_EDITOR
                if (simpleButton == null)
                {
                    Debug.LogWarning(
                        "UiBrain: MenuCanvasPrefab doesn't have SimpleButton component in children"
                    );
                }
                else
                {
                    Debug.LogWarning(
                        $"UiBrain: SimpleButton found but Role is {simpleButton.Role}, expected Back"
                    );
                }
#endif
            }

#if UNITY_EDITOR
            Debug.Log("UiBrain: Back button created and wired up");
#endif
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
#if UNITY_EDITOR
                Debug.Log("UiBrain: Back button destroyed and events cleaned up");
#endif
            }
        }

        #endregion

        #region Menu Navigation

        private void HandleBackButtonPressed()
        {
            if (CurrentMenuDepth > 0)
            {
                // Navigate up one level in menu hierarchy
                NavigateToParentMenu();
            }
            else
            {
                // At root level, handle based on current state
                HandleRootLevelBack();
            }
        }

        private void NavigateToParentMenu()
        {
            // If we're at depth 2 or higher, we're in a submenu and should go back to its parent
            if (CurrentMenuDepth >= 2)
            {
                // Find which submenu is currently active and transition back to settings menu
                var activeSubMenu = FindActiveSettingsSubmenu();
                if (activeSubMenu != null)
                {
                    _isTransitioning = true;
                    StartCoroutine(
                        TransitionBackToSettingsMenu(activeSubMenu, settingsMenuLocation)
                    );
                    return;
                }
            }

            // If we're at depth 1, check if we're in the main settings menu
            if (CurrentMenuDepth == 1 && settingsMenuLocation?.activeInstance != null)
            {
                // Transition from main settings back to prebattle menu
                BackToPreBattleMenu();
                return;
            }

            // Fallback for unhandled cases - just decrement depth
            CurrentMenuDepth = Mathf.Max(0, CurrentMenuDepth - 1);

#if UNITY_EDITOR
            Debug.Log($"UiBrain: Navigated to parent menu. New depth: {CurrentMenuDepth}");
#endif
        }

        private MenuLocation FindActiveSettingsSubmenu()
        {
            // Check all possible settings submenus to find which one is active
            if (gameSettingsGraphicsLocation?.activeInstance != null)
            {
                return gameSettingsGraphicsLocation;
            }

            // Add other submenu locations as they're created
            var audioMenuLocation = uiSettings?.GetMenuLocation(MenuName.AudioMenu);
            if (audioMenuLocation?.activeInstance != null)
            {
                return audioMenuLocation;
            }

            var controlsMenuLocation = uiSettings?.GetMenuLocation(MenuName.ControlsMenu);
            if (controlsMenuLocation?.activeInstance != null)
            {
                return controlsMenuLocation;
            }

            var gameplayMenuLocation = uiSettings?.GetMenuLocation(MenuName.GameplayMenu);
            if (gameplayMenuLocation?.activeInstance != null)
            {
                return gameplayMenuLocation;
            }

            return null;
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
