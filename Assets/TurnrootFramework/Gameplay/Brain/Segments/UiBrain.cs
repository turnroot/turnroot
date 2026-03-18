using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Gameplay.PlayerSettings;
using Turnroot.GameSettings;
using Turnroot.UI;
using Turnroot.UI.Components.Menu;
using Turnroot.UI.Components.RadialMenu;
using Turnroot.UI.Components.SimpleButton;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Gameplay.Brain.Segments
{
    /// <summary>
    /// Manages UI menus, transitions, and settings bindings for pre-battle and battle game states.
    /// </summary>
    [RequireComponent(typeof(CursorBrain))]
    public partial class UiBrain : BrainComponent
    {
        #region Fields and Properties

        [HideInInspector]
        public GamewideUiSettings uiSettings;

        [HideInInspector]
        public MenuEntry settingsMenuLocation;

        [HideInInspector]
        public MenuEntry gameSettingsGraphicsLocation;

        [HideInInspector]
        public MenuEntry gameSettingsGameplayLocation;

        [HideInInspector]
        public MenuEntry gameSettingsAudioLocation;

        [HideInInspector]
        public MenuEntry gameSettingsControlsLocation;

        [HideInInspector]
        public MenuEntry preBattleMenuLocation;

        [HideInInspector]
        public MenuEntry prebattleMapMenuLocation;

        [HideInInspector]
        public MenuEntry prebattleUnitsMenuLocation;

        [HideInInspector]
        public MenuEntry battleActionSelectMenuLocation;

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
            uiSettings.GetPreBattleMenu()?.activeInstance;

        protected override EventPriority GetSubscriptionPriority() => EventPriority.Low;

        public int CurrentMenuDepth => _menuTracker?.CurrentDepth ?? 0;
        public bool IsInSubMenu => _menuTracker?.IsInSubMenu ?? false;

        public void PublishPreBattleCompleted() => _brain.PublishPreBattleCompleted();

        internal bool IsTransitioning => _isTransitioning;

        internal void SetTransitioning(bool value) => _isTransitioning = value;

        internal MonoBehaviour GetMonoBehaviour() => this;

        internal MenuDepthTracker GetMenuTracker() => _menuTracker;

        internal MenuTransitionManager GetTransitionManager() => _transitionManager;

        // Convenience wrappers for common menu checks
        public bool IsInMenu(MenuEntry menu) => _menuTracker?.IsInMenu(menu) ?? false;

        public bool IsInPreBattleMenu() => _menuTracker?.IsInPreBattleMenu(uiSettings) ?? false;

        #endregion

        #region Unity Lifecycle and Initialization

        protected override void Awake()
        {
            base.Awake();
            uiSettings = GamewideUiSettings.Instance;
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
                battleActionSelectMenuLocation = uiSettings.GetBattleActionSelectMenu();

                // Initialize new components
                _transitionManager = new MenuTransitionManager(this, uiSettings);
                _settingsBindingManager = new SettingsBindingManager();
                _menuTracker = new MenuDepthTracker();
                _routeHandler = new MenuRouteHandler(this);

                // Subscribe to depth changes so the Back button follows submenu navigation
                if (_menuTracker != null)
                {
                    _menuTracker.OnDepthChanged += OnMenuDepthChanged;
                }

                // Player settings (no input customization present)
                _playerSettings = GameplayPlayerSettings.Instance;

                // Note: GetComponent is safe here even though we're in Awake
                // because we're just getting a reference, not accessing its state
                var cursorBrain = GetComponent<CursorBrain>();
                cursorBrain?.SetUiSettingsReference(uiSettings);
            }

            // Subscribe to selection changes so they are persisted to LTM centrally
            if (_brain != null)
            {
                _brain.OnUnitSelectionChanged -= HandleUnitSelectionChangedPersist;
                _brain.OnUnitSelectionChanged += HandleUnitSelectionChangedPersist;
            }

#if UNITY_EDITOR
            WarnPrefabs();
#endif
        }

        #endregion

        #region Brain Events and State Management


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

            // Don't dispose shared actions - just clear any previously assigned action.
            try
            {
                if (sb.SelectAction != null)
                {
                    sb.SelectAction.Disable();
                }
            }
            catch { }

            if (sb.Role == SimpleButtonRole.Back)
            {
                sb.AssignSelectAction(UIInputActionDefaults.Back);
                // Ensure the Back handler is present (rebinding may have caused subscriptions to be lost)
                try
                {
                    sb.OnSelected -= HandleBackButtonPressed;
                }
                catch { }
                sb.OnSelected += HandleBackButtonPressed;
            }
            else if (sb.Role == SimpleButtonRole.Details)
            {
                sb.AssignSelectAction(UIInputActionDefaults.Confirm);
                try
                {
                    sb.OnSelected -= HandleDetailsButtonPressed;
                }
                catch { }
                sb.OnSelected += HandleDetailsButtonPressed;
            }
        }

        private void HandleUnitSelectionChangedPersist(CharacterInstance unit, bool selected)
        {
            if (unit == null || unit.CharacterTemplate == null || Brain.ltm == null)
            {
                return;
            }

            var key = LtmKeys.UnitSelectedForBattlePrefix + unit.CharacterTemplate.name;
            _brain.ltm.RememberBool(key, selected);
        }

        private UIFade EnsureUIFadeOnObject(GameObject obj)
        {
            if (obj == null)
            {
                return null;
            }

            var fade = UIFadeCache.GetOrCreate(obj, uiSettings?.MenuInternalTransitionTime ?? 0f);
            return fade;
        }

        // Attempt to find a panel object on a MenuCanvas instance. We prefer a child named "Panel",
        // otherwise fall back to any child with a CanvasGroup or an Image component.
        private GameObject FindMenuCanvasPanel(GameObject canvas)
        {
            if (canvas == null)
            {
                return null;
            }

            foreach (Transform t in canvas.GetComponentsInChildren<Transform>(true))
            {
                if (
                    string.Equals(
                        t.gameObject.name,
                        "Panel",
                        System.StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return t.gameObject;
                }
            }

            // Look for CanvasGroup first
            var cg = canvas.GetComponentInChildren<CanvasGroup>(true);
            if (cg != null)
            {
                return cg.gameObject;
            }

            // Fall back to an Image-based panel
            var img = canvas.GetComponentInChildren<UnityEngine.UI.Image>(true);
            if (img != null)
            {
                return img.gameObject;
            }
            // As a final fallback, return the canvas root itself so we can still fade something
            return canvas;
        }

        private void HandlePositioningModeEntered()
        {
            // Fade out the Details role button if present
            if (_currentDetailsCanvasPrefab != null)
            {
                var detailsFade = EnsureUIFadeOnObject(_currentDetailsCanvasPrefab);
                detailsFade?.Hide();
            }

            // Fade out the panel on the menu canvas so it doesn't obstruct the map
            if (_currentMenuCanvasPrefab != null)
            {
                var panel = FindMenuCanvasPanel(_currentMenuCanvasPrefab);
                var panelFade = EnsureUIFadeOnObject(panel);
                panelFade?.Hide();
            }
        }

        private void HandlePositioningModeExited()
        {
            // Fade details back in (ensure the details canvas is active first)
            if (_currentDetailsCanvasPrefab != null)
            {
                if (!_currentDetailsCanvasPrefab.activeInHierarchy)
                {
                    _currentDetailsCanvasPrefab.SetActive(true);
                }

                var detailsFade = EnsureUIFadeOnObject(_currentDetailsCanvasPrefab);
                detailsFade?.Show();
            }

            // Fade menu panel back in (ensure panel is active first)
            if (_currentMenuCanvasPrefab != null)
            {
                var panel = FindMenuCanvasPanel(_currentMenuCanvasPrefab);
                if (panel != null && !panel.activeInHierarchy)
                {
                    panel.SetActive(true);
                }
                var panelFade = EnsureUIFadeOnObject(panel);
                panelFade?.Show();
            }
        }

        private System.Action<BrainState> _onStateChangedHandler;

        protected override void SubscribeToBrainEvents()
        {
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
            Brain.OnPositioningModeEntered += HandlePositioningModeEntered;
            Brain.OnPositioningModeExited += HandlePositioningModeExited;

            // If the Brain already has an active state, invoke handler immediately so UI can react to the current state
            var current = Brain?.stateBrain.CurrentState;
            if (current != null)
            {
                _onStateChangedHandler(current);
            }
        }

        private void OnMenuDepthChanged()
        {
            var currentState = Brain?.stateBrain.CurrentState?.Name ?? string.Empty;
            HandleButtonsForState(currentState);
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            Brain.OnPositioningModeEntered -= HandlePositioningModeEntered;
            Brain.OnPositioningModeExited -= HandlePositioningModeExited;

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
            if (_menuTracker != null)
            {
                _menuTracker.OnDepthChanged -= OnMenuDepthChanged;
            }

            DestroyBackButton();
            DestroyDetailsButton();
        }

        #endregion
    }
}
