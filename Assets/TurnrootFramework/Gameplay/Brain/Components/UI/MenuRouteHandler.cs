using System;
using System.Collections.Generic;
using Turnroot.Gameplay.Brain.UI;
using Turnroot.GameSettings;
using Turnroot.UI.Components.Menu;
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
            _menuActionRoutes["Team"] = _ => OpenTeamManagement();
            _menuActionRoutes["Items"] = _ => OpenInventory();
            _menuActionRoutes["Settings"] = _ => _brain.OpenMainGameSettingsMenu();
            _menuActionRoutes["Skills"] = _ => OpenSkills();
            _menuActionRoutes["Map"] = _ => _brain.OpenPreBattleMapOverview();
            _menuActionRoutes["Support"] = _ => OpenSupport();
            _menuActionRoutes["Withdraw"] = _ => HandleWithdraw();

            // Settings menu routes
            _menuActionRoutes["Graphics"] = _ =>
                TransitionToSubmenu(_brain.gameSettingsGraphicsLocation);
            _menuActionRoutes["Gameplay"] = _ =>
                TransitionToSubmenu(_brain.gameSettingsGameplayLocation);
            _menuActionRoutes["Audio"] = _ => TransitionToSubmenu(_brain.gameSettingsAudioLocation);
            _menuActionRoutes["Controls"] = _ =>
                TransitionToSubmenu(_brain.gameSettingsControlsLocation);
        }

        public void HandleMenuSelect(Turnroot.UI.Components.MenuItemBase item)
        {
            if (item == null)
                return;

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
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning($"No route defined for menu item: {item.ItemName}");
#endif
            }
        }

        public void HandleMenuNavigate(Turnroot.UI.Components.MenuItemBase item)
        {
#if UNITY_EDITOR
            Debug.Log($"MenuRouteHandler: Navigated to item: {item?.ItemName}");
#endif
            // TODO: Handle navigation (highlighting, audio feedback, etc.)
        }

        // Route implementations
        private void OpenTeamManagement()
        {
#if UNITY_EDITOR
            Debug.Log("Opening Team Management - TODO: Implement");
#endif
            // TODO: Implement team management UI
        }

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

            if (_brain._isTransitioning)
                return;

            _brain._isTransitioning = true;
            _brain.StartCoroutine(
                TransitionCoroutine(_brain.settingsMenuLocation, submenuLocation)
            );
        }

        private System.Collections.IEnumerator TransitionCoroutine(
            MenuLocation from,
            MenuLocation to
        )
        {
            _brain._menuTracker?.TrackTransition(from, to);
            yield return _brain._transitionManager.TransitionBetween(from, to);
            _brain._isTransitioning = false;
        }

        public void AddRoute(string itemName, Action<Turnroot.UI.Components.MenuItemBase> action)
        {
            _menuActionRoutes[itemName] = action;
        }

        public void RemoveRoute(string itemName)
        {
            _menuActionRoutes.Remove(itemName);
        }
    }
}
