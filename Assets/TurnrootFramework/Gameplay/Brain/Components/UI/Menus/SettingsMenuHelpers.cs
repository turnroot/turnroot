using Turnroot.Gameplay.Brain;
using Turnroot.GameSettings;
using Turnroot.UI.Components;
using UnityEngine;

namespace TurnrootFramework.Gameplay.Brain.Segments
{
    public partial class UiBrain : BrainComponent
    {
        #region Settings Menu Opening and Core Operations


        private void OpenSubmenu(MenuLocation targetMenu, string menuTypeName)
        {
            if (_isTransitioning || targetMenu == null || targetMenu.prefab == null)
                return;

            if (targetMenu.activeInstance != null)
                return;

            var sourceMenu = FindActiveMenu();
            if (sourceMenu?.activeInstance == null)
            {
#if UNITY_EDITOR
                Debug.LogError($"UiBrain: No active menu to transition from to {menuTypeName}");
#endif
                return;
            }

            _isTransitioning = true;
            _menuTracker?.TrackTransition(sourceMenu, targetMenu);
            StartCoroutine(TransitionToSubmenuCoroutine(sourceMenu, targetMenu));
        }

        private MenuLocation FindActiveMenu()
        {
            if (preBattleMenuLocation?.activeInstance != null)
                return preBattleMenuLocation;

            var allMenus = uiSettings?.allPossibleMenuLocations;
            if (allMenus != null)
            {
                foreach (var menu in allMenus)
                {
                    if (menu?.activeInstance != null)
                        return menu;
                }
            }
            return null;
        }

        public void OpenMainGameSettingsMenu() =>
            OpenSubmenu(uiSettings?.GetGameSettingsMenu(), "game settings");

        public void OpenPreBattleMapOverview() =>
            OpenSubmenu(uiSettings?.GetPrebattleMapMenu(), "pre-battle map");

        public void OpenPreBattleUnitsMenu() =>
            OpenSubmenu(uiSettings?.GetPrebattleUnitsMenu(), "pre-battle units");

        #endregion

        #region Settings Menu Event Handlers


        public void HandleGameSettingsMenuSelect(MenuItemBase item)
        {
#if UNITY_EDITOR
            Debug.Log($"UiBrain: HandleGameSettingsMenuSelect received item: {item?.ItemName}");
#endif
            // Delegate to the route handler for unified menu handling
            _routeHandler?.HandleMenuSelect(item);
        }

        #endregion

        #region Settings Menu Navigation and Transitions


        // Back navigation moved to centralized handlers; legacy helper removed.
        // Use MenuDepthTracker.PopTransition() and start TransitionToSubmenuCoroutine(from, to) directly.

        private System.Collections.IEnumerator TransitionToSettingsMenu(
            MenuLocation fromMenuLocation,
            MenuLocation toMenuLocation
        )
        {
            // Use the transition manager - always destroy and recreate
            yield return _transitionManager.TransitionBetween(fromMenuLocation, toMenuLocation);

            _isTransitioning = false;
        }

        #endregion
    }
}
