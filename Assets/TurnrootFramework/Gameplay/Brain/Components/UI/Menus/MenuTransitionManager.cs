using System.Collections;
using Turnroot.GameSettings;
using Turnroot.UI.Components;
using Turnroot.UI.Components.Menu;
using Turnroot.UI.Components.RadialMenu;
using Turnroot.UI.Components.SimpleButton;
using UnityEngine;

namespace TurnrootFramework.Gameplay.Brain.Segments
{
    public enum MenuType
    {
        Unknown,
        PreBattle,
        Settings,
        Graphics,
        Gameplay,
        Audio,
        Controls,
        Battle,
        Map,
        Team,
    }

    public class MenuTransitionManager
    {
        private readonly UiBrain _brain;
        private readonly GamewideUiSettings _settings;

        // Track menu types for better cleanup
        private MenuType _currentMenuType = MenuType.Unknown;

        public MenuTransitionManager(UiBrain brain, GamewideUiSettings settings)
        {
            _brain = brain;
            _settings = settings;
        }

        public MenuType CurrentMenuType => _currentMenuType;

        private MenuType DetectMenuType(MenuLocation location)
        {
            if (location == null)
            {
                return MenuType.Unknown;
            }

            if (location == _settings?.GetPreBattleMenu())
            {
                return MenuType.PreBattle;
            }

            if (location == _settings?.GetGameSettingsMenu())
            {
                return MenuType.Settings;
            }

            if (location == _settings?.GetGameSettingsGraphicsMenu())
            {
                return MenuType.Graphics;
            }

            if (location == _settings?.GetGameSettingsGameplayMenu())
            {
                return MenuType.Gameplay;
            }

            if (location == _settings?.GetGameSettingsAudioMenu())
            {
                return MenuType.Audio;
            }

            if (location == _settings?.GetGameSettingsControlsMenu())
            {
                return MenuType.Controls;
            }

            if (location == _settings?.GetPrebattleMapMenu())
            {
                return MenuType.Map;
            }
            if (location == _settings?.GetPrebattleUnitsMenu())
            {
                return MenuType.Team;
            }

            return MenuType.Unknown;
        }

        public IEnumerator TransitionBetween(
            MenuLocation from,
            MenuLocation to,
            bool destroyFrom = false
        )
        {
            var fromInstance = from?.activeInstance;
            Debug.Log(
                $"MenuTransitionManager: TransitionBetween from={from?.menuName} hasFromInstance={fromInstance != null} to={to?.menuName} destroyFrom={destroyFrom}"
            );

            // Update current menu type
            _currentMenuType = DetectMenuType(to);

            // Hide source menu if it exists
            if (fromInstance != null && fromInstance.TryGetComponent<UIFade>(out var fromFade))
            {
                fromFade.Hide();
                yield return new WaitForSeconds(fromFade.lerpTime + 0.1f);
            }

            // Handle source cleanup - always disable/hide source menu to prevent overlap
            if (fromInstance != null)
            {
                Debug.Log(
                    $"MenuTransitionManager: Cleaning up source menu {from.menuName} (instance={fromInstance.name})"
                );
                CleanupMenuEvents(fromInstance);

                if (destroyFrom)
                {
                    Debug.Log(
                        $"MenuTransitionManager: Destroying source instance {fromInstance.name}"
                    );
                    Object.Destroy(fromInstance);
                    from.activeInstance = null;
                }
                else
                {
                    // Disable and hide for potential reuse during back navigation
                    if (fromInstance.TryGetComponent<MenuBase>(out var menu))
                    {
                        menu.enabled = false;
                    }
                    fromInstance.SetActive(false);
                    from.activeInstance = null; // Clear active instance since it's hidden and not reusable for submenus
                }
            }

            // Instantiate target menu if needed or re-enable if it exists
            bool instantiated = false;
            if (to.activeInstance == null && to.prefab != null)
            {
                Debug.Log($"MenuTransitionManager: Instantiating target menu {to.menuName}");
                to.activeInstance = Object.Instantiate(to.prefab);
                instantiated = true;
            }
            else if (to.activeInstance != null)
            {
                // Re-enable existing menu for back navigation
                // Ensure event handlers and input actions are re-attached
                Debug.Log(
                    $"MenuTransitionManager: Reattaching and cleaning events for existing instance {to.activeInstance?.name} of menu {to.menuName}"
                );
                CleanupMenuEvents(to.activeInstance);
            }

            // If an instance now exists (either newly instantiated or pre-existing), set it up and initialize
            if (to.activeInstance != null)
            {
                SetupMenu(to);

                HandleCreatedMenuInstance(to);

                // If we re-enabled a previously existing instance, ensure it's active and enabled
                if (!instantiated)
                {
                    to.activeInstance.SetActive(true);
                    if (to.activeInstance.TryGetComponent<MenuBase>(out var existingMenu))
                    {
                        existingMenu.enabled = true;
                    }
                }
            }

            if (to.activeInstance != null)
            {
                // Show target menu
                var targetFade = EnsureUIFade(
                    to.activeInstance,
                    _settings.MenuInternalTransitionTime
                );
                targetFade.Show();
            }
        }

        public IEnumerator TransitionToPreBattle(MenuLocation from, MenuLocation preBattle)
        {
            // Update current menu type
            _currentMenuType = MenuType.PreBattle;

            // Hide current menu
            if (
                from?.activeInstance != null
                && from.activeInstance.TryGetComponent<UIFade>(out var fromFade)
            )
            {
                fromFade.Hide();
                yield return new WaitForSeconds(fromFade.lerpTime + 0.1f);
            }

            // Clean up and destroy current menu
            if (from?.activeInstance != null)
            {
                CleanupMenuEvents(from.activeInstance);
                Object.Destroy(from.activeInstance);
                from.activeInstance = null;
            }

            // Create and setup prebattle menu
            if (preBattle.prefab != null)
            {
                preBattle.activeInstance = Object.Instantiate(preBattle.prefab);
                SetupPreBattleMenu(preBattle);

                var fade = EnsureUIFade(
                    preBattle.activeInstance,
                    _settings.MenuInternalTransitionTime
                );
                fade.Show();
            }
        }

        public IEnumerator TransitionToBattle(MenuLocation preBattle)
        {
            // Update current menu type
            _currentMenuType = MenuType.Battle;

            var menuInstance = preBattle?.activeInstance;
            if (menuInstance == null)
            {
                yield break;
            }

            // Start fade out
            if (menuInstance.TryGetComponent<UIFade>(out var uiFade))
            {
                uiFade.Hide();
                yield return new WaitForSeconds(uiFade.lerpTime + 0.1f);
            }

            // Clean up and destroy menu
            CleanupMenuEvents(menuInstance);
            Object.Destroy(menuInstance);
            preBattle.activeInstance = null;

            // Notify brain of completion
            _brain.Brain.PublishPreBattleCompleted();
        }

        private void SetupMenu(MenuLocation location)
        {
            var instance = location.activeInstance;

            // Setup events based on menu type - use specific handlers based on context
            // Bind every MenuBase inside the instance (handles nested lists like 'Right')
            var menuBases = instance.GetComponentsInChildren<MenuBase>(true);
            foreach (var menu in menuBases)
            {
                Debug.Log(
                    $"MenuTransitionManager.SetupMenu: setting up MenuBase for {location.menuName} menu={menu.name} instance={instance.name}"
                );
                menu.uiBrain = _brain;

                var menuType = DetectMenuType(location);
                if (menuType is MenuType.PreBattle or MenuType.Map)
                {
                    // Pre-battle context (map submenus etc.)
                    menu.OnNavigate += _brain.HandlePreBattleMenuNavigate;
                    menu.OnItemSelected += _brain.HandlePreBattleMenuSelect;
                }
                else if (
                    menuType
                    is MenuType.Settings
                        or MenuType.Graphics
                        or MenuType.Gameplay
                        or MenuType.Audio
                        or MenuType.Controls
                )
                {
                    // Settings-related menus
                    menu.OnNavigate += _brain.HandleGameSettingsMenuNavigate;
                    menu.OnItemSelected += _brain.HandleGameSettingsMenuSelect;
                }
                else
                {
                    // Standalone / other menus: route through general handlers
                    menu.OnNavigate += _brain.HandleMenuNavigate;
                    menu.OnItemSelected += _brain.HandleMenuSelect;
                }

                _brain.SetupMenuInputActions(menu);

                // Ensure child SimpleButton components use the menu's select action and are wired
                var simpleButtons = menu.GetComponentsInChildren<SimpleButton>(true);
                foreach (var sb in simpleButtons)
                {
                    sb.AssignSelectAction(menu.selectAction);
                }
            }

            if (instance.TryGetComponent<RadialMenu>(out var radial))
            {
                Debug.Log(
                    $"MenuTransitionManager.SetupMenu: setting up RadialMenu for {location.menuName} instance={instance.name}"
                );
                radial.uiBrain = _brain;
                // For radial menus, use settings handlers (these are typically settings menus)
                radial.OnNavigate += _brain.HandleGameSettingsMenuNavigate;
                radial.OnItemSelected += _brain.HandleGameSettingsMenuSelect;

                radial.navigateAction.Enable();

                if (radial.selectAction == null || radial.selectAction.bindings.Count == 0)
                {
                    radial.selectAction?.Disable();
                    radial.selectAction?.Dispose();
                    radial.selectAction = InputActionFactory.CreateSelect();
                }
                else
                {
                    radial.selectAction.Enable();
                }
            }

            _brain.SetupSettingsUIBindings(instance);
            _brain.ApplyMenuColors(instance, location.style);
        }

        private void SetupPreBattleMenu(MenuLocation preBattleLocation)
        {
            var instance = preBattleLocation.activeInstance;
            var menuStyle = preBattleLocation.style;
            Debug.Log(
                $"MenuTransitionManager.SetupPreBattleMenu: menu={preBattleLocation.menuName} instance={instance?.name} style={menuStyle}"
            );

            if (menuStyle == MenuStyle.Pie)
            {
                if (instance.TryGetComponent<RadialMenu>(out var radialMenu))
                {
                    radialMenu.uiBrain = _brain;
                    radialMenu.OnNavigate += _brain.HandlePreBattleMenuNavigate;
                    radialMenu.OnItemSelected += _brain.HandlePreBattleMenuSelect;

                    radialMenu.navigateAction.Enable();

                    if (
                        radialMenu.selectAction == null
                        || radialMenu.selectAction.bindings.Count == 0
                    )
                    {
                        radialMenu.selectAction?.Disable();
                        radialMenu.selectAction?.Dispose();
                        radialMenu.selectAction = InputActionFactory.CreateSelect();
                    }
                    else
                    {
                        radialMenu.selectAction.Enable();
                    }
                }
            }
            else if (menuStyle is MenuStyle.List or MenuStyle.Grid)
            {
                // Bind every MenuBase found inside the prebattle prefab (handles 'Right' or other sub-panels)
                var listMenus = instance.GetComponentsInChildren<MenuBase>(true);
                foreach (var listMenu in listMenus)
                {
                    Debug.Log(
                        $"MenuTransitionManager.SetupPreBattleMenu: binding prebattle list menu {listMenu.name} in {preBattleLocation.menuName}"
                    );

                    listMenu.uiBrain = _brain;
                    listMenu.OnNavigate += _brain.HandlePreBattleMenuNavigate;
                    listMenu.OnItemSelected += _brain.HandlePreBattleMenuSelect;
                    _brain.SetupMenuInputActions(listMenu);

                    // Ensure child SimpleButton components get the list menu's select action
                    var simpleButtons =
                        listMenu.GetComponentsInChildren<Turnroot.UI.Components.SimpleButton.SimpleButton>(
                            true
                        );
                    foreach (var sb in simpleButtons)
                    {
                        sb.AssignSelectAction(listMenu.selectAction);
                    }
                }

                // Initialize team menu components if present (no-op when not a team prefab)
                InitializeTeamMenu(instance);
            }

            _brain.ApplyMenuColors(instance, menuStyle);
        }

        private void HandleCreatedMenuInstance(MenuLocation to)
        {
            var instance = to.activeInstance;

            // Initialize components for known special menus (no-op when component not present)
            InitializeMapPopulator(instance);
            InitializeTeamMenu(instance);
        }

        private void InitializeMapPopulator(GameObject instance)
        {
            var environmentalConditionsPopulator =
                instance.GetComponentInChildren<PopulateMapPrefabEnviromentConditions>(true);
            environmentalConditionsPopulator?.Initialize(_brain.GetBrain());
        }

        private void InitializeTeamMenu(GameObject instance)
        {
            var unitColumns = instance.GetComponentInChildren<UnitSelectionColumns>(true);
            unitColumns?.Initialize(_brain.GetBrain());
            InitializeMapPopulator(instance);
        }

        private void CleanupMenuEvents(GameObject instance)
        {
            Debug.Log($"MenuTransitionManager.CleanupMenuEvents: instance={instance?.name}");
            // Clean up MenuBase handlers on all nested menus
            var menus = instance.GetComponentsInChildren<MenuBase>(true);
            foreach (var menu in menus)
            {
                // Clean up all possible event handlers
                menu.OnNavigate -= _brain.HandlePreBattleMenuNavigate;
                menu.OnItemSelected -= _brain.HandlePreBattleMenuSelect;
                menu.OnNavigate -= _brain.HandleGameSettingsMenuNavigate;
                menu.OnItemSelected -= _brain.HandleGameSettingsMenuSelect;
            }

            // Clean up any nested RadialMenu handlers too
            var radials = instance.GetComponentsInChildren<RadialMenu>(true);
            foreach (var radial in radials)
            {
                Debug.Log(
                    $"MenuTransitionManager: Removing RadialMenu event handlers from {instance.name}"
                );
                radial.OnNavigate -= _brain.HandlePreBattleMenuNavigate;
                radial.OnItemSelected -= _brain.HandlePreBattleMenuSelect;
                radial.OnNavigate -= _brain.HandleGameSettingsMenuNavigate;
                radial.OnItemSelected -= _brain.HandleGameSettingsMenuSelect;
            }
        }

        private UIFade EnsureUIFade(GameObject instance, float lerpTime)
        {
            if (!instance.TryGetComponent<UIFade>(out var uiFade))
            {
                uiFade = instance.AddComponent<UIFade>();
            }
            uiFade.lerpTime = lerpTime;
            return uiFade;
        }
    }
}
