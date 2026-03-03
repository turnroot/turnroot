using System;
using System.Collections.Generic;
using Turnroot.GameSettings;
using Turnroot.UI.Components.GridMenu;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain.Segments
{
    /// <summary>
    /// Routes menu item selections to appropriate handlers and actions.
    /// </summary>
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
            _menuActionRoutes[MenuRouteNames.Team] = _ => _brain.OpenPreBattleUnitsMenu();
            _menuActionRoutes[MenuRouteNames.Items] = _ => OpenInventory();
            _menuActionRoutes[MenuRouteNames.Settings] = _ => _brain.OpenMainGameSettingsMenu();
            _menuActionRoutes[MenuRouteNames.Skills] = _ => OpenSkills();
            _menuActionRoutes[MenuRouteNames.Map] = _ => _brain.OpenPreBattleMapOverview();
            _menuActionRoutes[MenuRouteNames.StartingPositions] = _ =>
                _brain.OpenPreBattleUnitPositionsMenu();
            _menuActionRoutes[MenuRouteNames.Support] = _ => OpenSupport();
            _menuActionRoutes[MenuRouteNames.Withdraw] = _ => HandleWithdraw();
            _menuActionRoutes[MenuRouteNames.StartBattle] = _ => _brain.HandleStartBattleClick();

            // Settings menu routes
            _menuActionRoutes[MenuRouteNames.Graphics] = _ =>
                TransitionToSubmenu(_brain.gameSettingsGraphicsLocation);
            _menuActionRoutes[MenuRouteNames.Gameplay] = _ =>
                TransitionToSubmenu(_brain.gameSettingsGameplayLocation);
            _menuActionRoutes[MenuRouteNames.Audio] = _ =>
                TransitionToSubmenu(_brain.gameSettingsAudioLocation);
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
            if (Time.time - _lastSelectTime < SelectDebounceSeconds)
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
            else if (item.gameObject.CompareTag(MenuRouteNames.UnitCellTag)) // Unit cell in a grid; special case
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
                $"No route defined for menu item: {item.ItemName}".LogError();
            }
        }

        // Route implementations
        private void OpenInventory() => "Opening Inventory - TODO: Implement".LogInfo(); // TODO: Implement inventory UI

        private void OpenSkills() => "Opening Skills - TODO: Implement".LogInfo(); // TODO: Implement skills UI

        private void OpenSupport() => "Opening Support - TODO: Implement".LogInfo(); // TODO: Implement support UI

        private void HandleWithdraw() => "Handling Withdraw - TODO: Implement".LogInfo(); // TODO: Handle withdraw action

        private OperationResult TransitionToSubmenu(MenuLocation submenuLocation)
        {
            var validation = OperationResultGuards.RequireNotNull(
                submenuLocation,
                nameof(submenuLocation)
            );
            if (!validation.Success)
            {
                return validation;
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
