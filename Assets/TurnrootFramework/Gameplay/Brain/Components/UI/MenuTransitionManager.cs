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
            if (to.activeInstance == null && to.prefab != null)
            {
                Debug.Log($"MenuTransitionManager: Instantiating target menu {to.menuName}");
                to.activeInstance = Object.Instantiate(to.prefab);
                SetupMenu(to);
            }
            else if (to.activeInstance != null)
            {
                // Re-enable existing menu for back navigation
                // Ensure event handlers and input actions are re-attached
                Debug.Log(
                    $"MenuTransitionManager: Reattaching and cleaning events for existing instance {to.activeInstance?.name} of menu {to.menuName}"
                );
                CleanupMenuEvents(to.activeInstance);
                SetupMenu(to);

                to.activeInstance.SetActive(true);
                if (to.activeInstance.TryGetComponent<MenuBase>(out var existingMenu))
                {
                    existingMenu.enabled = true;
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
            if (instance.TryGetComponent<MenuBase>(out var menu))
            {
                Debug.Log(
                    $"MenuTransitionManager.SetupMenu: setting up MenuBase for {location.menuName} instance={instance.name}"
                );
                menu.uiBrain = _brain;
                // For settings menus, use settings handlers
                menu.OnNavigate += _brain.HandleGameSettingsMenuNavigate;
                menu.OnItemSelected += _brain.HandleGameSettingsMenuSelect;
                _brain.SetupMenuInputActions(menu);

                // Ensure child SimpleButton components use the menu's select action so keyboard 'Select'
                // works consistently even after menu destroy/restore cycles.
                var simpleButtons =
                    instance.GetComponentsInChildren<Turnroot.UI.Components.SimpleButton.SimpleButton>();
                foreach (var sb in simpleButtons)
                {
                    // Assign the menu select action to each button and enable it
                    sb.SelectAction = menu.selectAction;
                    sb.SelectAction?.Enable();
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
            else if (menuStyle == MenuStyle.List || menuStyle == MenuStyle.Grid)
            {
                if (instance.TryGetComponent<MenuBase>(out var listMenu))
                {
                    listMenu.uiBrain = _brain;
                    listMenu.OnNavigate += _brain.HandlePreBattleMenuNavigate;
                    listMenu.OnItemSelected += _brain.HandlePreBattleMenuSelect;
                    _brain.SetupMenuInputActions(listMenu);

                    // Ensure child SimpleButton components get the list menu's select action
                    var simpleButtons =
                        instance.GetComponentsInChildren<Turnroot.UI.Components.SimpleButton.SimpleButton>();
                    foreach (var sb in simpleButtons)
                    {
                        sb.SelectAction = listMenu.selectAction;
                        sb.SelectAction?.Enable();
                    }
                }
            }

            _brain.ApplyMenuColors(instance, menuStyle);
        }

        private void CleanupMenuEvents(GameObject instance)
        {
            Debug.Log($"MenuTransitionManager.CleanupMenuEvents: instance={instance?.name}");
            if (instance.TryGetComponent<MenuBase>(out var menu))
            {
                // Clean up all possible event handlers
                menu.OnNavigate -= _brain.HandlePreBattleMenuNavigate;
                menu.OnItemSelected -= _brain.HandlePreBattleMenuSelect;
                menu.OnNavigate -= _brain.HandleGameSettingsMenuNavigate;
                menu.OnItemSelected -= _brain.HandleGameSettingsMenuSelect;
            }

            if (instance.TryGetComponent<RadialMenu>(out var radial))
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
