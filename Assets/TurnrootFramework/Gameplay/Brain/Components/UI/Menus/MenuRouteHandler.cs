using System;
using System.Collections.Generic;
using Turnroot.GameSettings;
using Turnroot.UI.Components.GridMenu;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain.Segments
{
    public class MenuRouteHandler
    {
        private readonly UiBrain _brain;
        private readonly Dictionary<string, Action<UI.Components.MenuItemBase>> _menuActionRoutes =
            new();

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
            _menuActionRoutes["StartingPositions"] = _ => _brain.OpenPreBattleUnitPositionsMenu();
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

        public void HandleMenuSelect(UI.Components.MenuItemBase item)
        {
            if (item == null)
            {
                return;
            }

            // Debounce rapid repeated selections to avoid accidental double-activation
            if (UnityEngine.Time.time - _lastSelectTime < SelectDebounceSeconds)
            {
                return;
            }
            _lastSelectTime = Time.time;

            // Check if we're currently in the pre-battle menu so we only adjust fade speed for pre-battle transitions
            if (item.IsCenter)
            {
                if (_brain.IsInPreBattleMenu())
                {
                    _brain.SetPreBattleMenuFadeSpeed(_brain.uiSettings.MenuFadeTime);
                }

                _brain.HandleStartBattleClick();
                return;
            }

            // Only adjust internal transition fade when we are originating from the pre-battle menu
            if (_brain.IsInPreBattleMenu())
            {
                _brain.SetPreBattleMenuFadeSpeed(_brain.uiSettings.MenuInternalTransitionTime);
            }

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
                    // Existing behavior: delegate to UiBrain's handler (which contains the selection logic)
                    _brain.HandleUnitCellSelectionToggle(unitCellItem, currentMenu);
                }
            }
            else
            {
                TurnrootLogger.Log(
                    $"No route defined for menu item: {item.ItemName}",
                    TurnrootLogger.LogLevel.Error
                );
            }
        }

        // Route implementations
        private void OpenInventory()
        {
            TurnrootLogger.Log("Opening Inventory - TODO: Implement");
            // TODO: Implement inventory UI
        }

        private void OpenSkills()
        {
            TurnrootLogger.Log("Opening Skills - TODO: Implement");
            // TODO: Implement skills UI
        }

        private void OpenSupport()
        {
            TurnrootLogger.Log("Opening Support - TODO: Implement");
            // TODO: Implement support UI
        }

        private void HandleWithdraw()
        {
            TurnrootLogger.Log("Handling Withdraw - TODO: Implement");
            // TODO: Handle withdraw action
        }

        private OperationResult TransitionToSubmenu(MenuLocation submenuLocation)
        {
            if (submenuLocation == null)
            {
                return OperationResult.Failure("Submenu location is null");
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
                return OperationResult.Failure("No active source menu found for transition");
            }

            // Use UiBrain's public method with proper source menu
            _brain.TransitionToSubmenu(sourceMenu, submenuLocation);
            return OperationResult.Successful();
        }

        public void AddRoute(string itemName, Action<UI.Components.MenuItemBase> action) =>
            _menuActionRoutes[itemName] = action;

        public void RemoveRoute(string itemName) => _menuActionRoutes.Remove(itemName);
    }
}
