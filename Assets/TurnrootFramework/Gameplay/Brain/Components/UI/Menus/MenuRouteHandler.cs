using System;
using System.Collections.Generic;
using Turnroot.GameSettings;
using Turnroot.UI.Components.GridMenu;
using UnityEngine;

namespace TurnrootFramework.Gameplay.Brain.Segments
{
    public class MenuRouteHandler
    {
        private readonly UiBrain _brain;
        private readonly Dictionary<
            string,
            Action<Turnroot.UI.Components.MenuItemBase>
        > _menuActionRoutes = new();

        public MenuRouteHandler(UiBrain brain)
        {
            _brain = brain;
            InitializeRoutes();
        }

        private void InitializeRoutes()
        {
            // Pre-battle menu routes
            _menuActionRoutes["Team"] = _ => _brain.OpenPreBattleUnitsMenu();
            _menuActionRoutes["Items"] = _ => OpenInventory();
            _menuActionRoutes["Settings"] = _ => _brain.OpenMainGameSettingsMenu();
            _menuActionRoutes["Skills"] = _ => OpenSkills();
            _menuActionRoutes["Map"] = _ => _brain.OpenPreBattleMapOverview();
            _menuActionRoutes["Support"] = _ => OpenSupport();
            _menuActionRoutes["Withdraw"] = _ => HandleWithdraw();
            _menuActionRoutes["StartBattle"] = _ => _brain.HandleStartBattleClick();

            // Settings menu routes
            _menuActionRoutes["Graphics"] = _ =>
                TransitionToSubmenu(_brain.gameSettingsGraphicsLocation);
            _menuActionRoutes["Gameplay"] = _ =>
                TransitionToSubmenu(_brain.gameSettingsGameplayLocation);
            _menuActionRoutes["Audio"] = _ => TransitionToSubmenu(_brain.gameSettingsAudioLocation);
            _menuActionRoutes["Controls"] = _ =>
                TransitionToSubmenu(_brain.gameSettingsControlsLocation);
        }

        private float _lastSelectTime = -10f;
        private const float SelectDebounceSeconds = 0.2f;

        public void HandleMenuSelect(Turnroot.UI.Components.MenuItemBase item)
        {
            if (item == null)
            {
                return;
            }

            // Debounce rapid repeated selections to avoid accidental double-activation
            if (UnityEngine.Time.time - _lastSelectTime < SelectDebounceSeconds)
            {
#if UNITY_EDITOR
                Debug.Log($"MenuRouteHandler: Ignored rapid selection of {item?.ItemName}");
#endif
                return;
            }
            _lastSelectTime = UnityEngine.Time.time;

#if UNITY_EDITOR
            Debug.Log($"MenuRouteHandler: HandleMenuSelect item: {item?.ItemName}");
#endif

            if (item.IsCenter)
            {
                _brain.SetPreBattleMenuFadeSpeed(_brain.uiSettings.MenuFadeTime);
                _brain.HandleStartBattleClick();
                return;
            }

            _brain.SetPreBattleMenuFadeSpeed(_brain.uiSettings.MenuInternalTransitionTime);

            if (_menuActionRoutes.TryGetValue(item.ItemName, out var action))
            {
                action(item);
            }
            else if (item.gameObject.CompareTag("UnitCell")) // Unit cell in a grid; special case
            {
                // Try to cast to UnitCellGridMenuItem
                if (item is UnitCellGridMenuItem unitCellItem)
                {
                    // Get the MenuLocation type of the currently open menu
                    var currentMenu = _brain.GetMenuTracker()?.CurrentMenu;
#if UNITY_EDITOR
                    Debug.Log($"MenuRouteHandler: Current source menu: {currentMenu?.menuName}");
#endif
                    // Existing behavior: delegate to UiBrain's handler (which contains the selection logic)
                    _brain.HandleUnitCellSelectionToggle(unitCellItem, currentMenu);
                }
                else
                {
#if UNITY_EDITOR
                    Debug.LogWarning(
                        $"MenuRouteHandler: UnitCell selected but item is not a UnitCellGridMenuItem: {item?.ItemName}"
                    );
#endif
                }
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning($"No route defined for menu item: {item.ItemName}");
#endif
            }
        }

        // Route implementations
        private void OpenInventory()
        {
#if UNITY_EDITOR
            Debug.Log("Opening Inventory - TODO: Implement");
#endif
            // TODO: Implement inventory UI
        }

        private void OpenSkills()
        {
#if UNITY_EDITOR
            Debug.Log("Opening Skills - TODO: Implement");
#endif
            // TODO: Implement skills UI
        }

        private void OpenSupport()
        {
#if UNITY_EDITOR
            Debug.Log("Opening Support - TODO: Implement");
#endif
            // TODO: Implement support UI
        }

        private void HandleWithdraw()
        {
#if UNITY_EDITOR
            Debug.Log("Handling Withdraw - TODO: Implement");
#endif
            // TODO: Handle withdraw action
        }

        private void TransitionToSubmenu(MenuLocation submenuLocation)
        {
            if (submenuLocation == null)
            {
#if UNITY_EDITOR
                Debug.LogError("MenuRouteHandler: Submenu location is null");
#endif
                return;
            }

            // Find the currently active menu as the source
            MenuLocation sourceMenu = null;
            var allMenus = _brain.uiSettings?.allPossibleMenuLocations;
            if (allMenus != null)
            {
                foreach (var menu in allMenus)
                {
                    if (menu?.activeInstance != null)
                    {
                        sourceMenu = menu;
                        break;
                    }
                }
            }

            if (sourceMenu == null)
            {
#if UNITY_EDITOR
                Debug.LogError("MenuRouteHandler: No active source menu found for transition");
#endif
                return;
            }

            // Use UiBrain's public method with proper source menu
            _brain.TransitionToSubmenu(sourceMenu, submenuLocation);
        }

        public void AddRoute(string itemName, Action<Turnroot.UI.Components.MenuItemBase> action) =>
            _menuActionRoutes[itemName] = action;

        public void RemoveRoute(string itemName) => _menuActionRoutes.Remove(itemName);
    }
}
