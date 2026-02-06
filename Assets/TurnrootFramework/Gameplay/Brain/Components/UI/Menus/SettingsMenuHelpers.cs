using Turnroot.GameSettings;
using Turnroot.UI.Components;
using UnityEngine;

namespace Turnroot.Gameplay.Brain.Segments
{
    public partial class UiBrain : BrainComponent
    {
        #region Settings Menu Opening and Core Operations


        private void OpenSubmenu(MenuLocation targetMenu, string menuTypeName)
        {
            if (_isTransitioning || targetMenu == null || targetMenu.prefab == null)
            {
                return;
            }

            if (targetMenu.activeInstance != null)
            {
                return;
            }

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
            {
                return preBattleMenuLocation;
            }

            var allMenus = uiSettings?.allPossibleMenuLocations;
            if (allMenus != null)
            {
                foreach (var menu in allMenus)
                {
                    if (menu?.activeInstance != null)
                    {
                        return menu;
                    }
                }
            }
            return null;
        }

        public void OpenMainGameSettingsMenu() =>
            OpenSubmenu(uiSettings.GetGameSettingsMenu(), "game settings");

        public void OpenPreBattleMapOverview() =>
            OpenSubmenu(uiSettings.GetPrebattleMapMenu(), "pre-battle map");

        public void OpenPreBattleUnitsMenu() =>
            OpenSubmenu(uiSettings.GetPrebattleUnitsMenu(), "pre-battle units");

        public void OpenPreBattleUnitPositionsMenu() =>
            OpenSubmenu(uiSettings.GetPrebattleUnitPositionsMenu(), "pre-battle unit positions");

        #endregion

        #region Settings Menu Event Handlers
        public void HandleGameSettingsMenuSelect(MenuItemBase item) =>
            _routeHandler?.HandleMenuSelect(item);

        #endregion
    }
}
