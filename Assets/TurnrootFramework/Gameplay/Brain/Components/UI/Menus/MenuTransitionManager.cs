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

            // Handle source cleanup
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
                    // For back navigation: keep instance but hide it
                    if (fromInstance.TryGetComponent<MenuBase>(out var menu))
                    {
                        menu.enabled = false;
                    }
                    fromInstance.SetActive(false);
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
                // IMPORTANT: When going back, the instance exists but is disabled
                Debug.Log($"MenuTransitionManager: Reactivating existing menu {to.menuName}");
                to.activeInstance.SetActive(true);

                if (to.activeInstance.TryGetComponent<MenuBase>(out var menu))
                {
                    menu.enabled = true;
                }

                // Re-setup events since they were cleaned up
                SetupMenu(to);
            }

            // If newly instantiated, do full setup
            if (instantiated && to.activeInstance != null)
            {
                SetupMenu(to);
                HandleCreatedMenuInstance(to);
            }

            // Show target menu
            if (to.activeInstance != null)
            {
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
                    menu.OnItemSelected += _brain.HandleGameSettingsMenuSelect;
                }
                else
                {
                    // Standalone / other menus: route through general handlers
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
                // Choose the correct handler based on menu type (radials can appear in different contexts)
                var radialMenuType = DetectMenuType(location);
                if (radialMenuType is MenuType.PreBattle or MenuType.Map or MenuType.Team)
                {
                    radial.OnItemSelected += _brain.HandlePreBattleMenuSelect;
                }
                else if (
                    radialMenuType
                    is MenuType.Settings
                        or MenuType.Graphics
                        or MenuType.Gameplay
                        or MenuType.Audio
                        or MenuType.Controls
                )
                {
                    radial.OnItemSelected += _brain.HandleGameSettingsMenuSelect;
                }
                else
                {
                    radial.OnItemSelected += _brain.HandleMenuSelect;
                }

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
            InitializePreparationObjectResolver(instance);
            InitializeTeamMenu(instance);
        }

        private void InitializePreparationObjectResolver(GameObject instance)
        {
            var preparationObjectResolver =
                instance.GetComponentInChildren<PreparationObjectResolver>(true);
            preparationObjectResolver?.Initialize(_brain.GetBrain());
        }

        private void InitializeTeamMenu(GameObject instance)
        {
            var unitColumns = instance.GetComponentInChildren<UnitSelectionColumns>(true);
            unitColumns?.Initialize(_brain.GetBrain());
        }

        private void CleanupMenuEvents(GameObject instance)
        {
            // Clean up MenuBase handlers on all nested menus. NOTE: Keep this limited to
            // menu-related handlers only; do not touch unrelated UI elements such as
            // standalone SimpleButton instances which may live outside of menus.
            var menus = instance.GetComponentsInChildren<MenuBase>(true);
            foreach (var menu in menus)
            {
                // Clean up all possible event handlers that may have been wired in SetupMenu()/SetupPreBattleMenu()
                menu.OnItemSelected -= _brain.HandlePreBattleMenuSelect;
                menu.OnItemSelected -= _brain.HandleGameSettingsMenuSelect;
                menu.OnItemSelected -= _brain.HandleMenuSelect; // ensure general handlers are removed as well
            }

            // Clean up any nested RadialMenu handlers too
            var radials = instance.GetComponentsInChildren<RadialMenu>(true);
            foreach (var radial in radials)
            {
                radial.OnItemSelected -= _brain.HandlePreBattleMenuSelect;
                radial.OnItemSelected -= _brain.HandleGameSettingsMenuSelect;
                radial.OnItemSelected -= _brain.HandleMenuSelect;
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
