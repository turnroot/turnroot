using Turnroot.GameSettings;
using Turnroot.UI.Components;

namespace Turnroot.Gameplay.Brain.Segments
{
    public partial class UiBrain : BrainComponent
    {
        #region Settings Menu Opening and Core Operations


        private MenuEntry _hubMenuPlaceholder;

        private MenuEntry GetHubMenuPlaceholder()
        {
            // Placeholder used to track the hub as a "parent" menu when
            // opening a settings menu directly from the hub (no menu stack exists).
            _hubMenuPlaceholder ??= new MenuEntry
            {
                menuName = MenuName.HubActionsMenu,
                style = MenuStyle.List,
            };
            return _hubMenuPlaceholder;
        }

        private bool IsInHubState() => Brain?.stateBrain?.CurrentState?.Name == BrainStateNames.Hub;

        private void OpenSubmenu(MenuEntry targetMenu, string menuTypeName)
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

            // Allow opening menus even when no other menu is currently active
            // (e.g. opening settings from the hub scene without an existing menu).
            // Also treat a null/empty active menu while in Hub as a valid "from" menu
            // so the back button returns to the hub rather than doing a root back.
            if (sourceMenu == null && IsInHubState())
            {
                sourceMenu = GetHubMenuPlaceholder();
            }

            // If we found a source menu, but it has no active instance, treat it as no source.
            if (sourceMenu != null && sourceMenu.activeInstance == null)
            {
                sourceMenu = null;
            }

            _isTransitioning = true;
            _menuTracker?.TrackTransition(sourceMenu, targetMenu);
            StartCoroutine(TransitionToSubmenuCoroutine(sourceMenu, targetMenu));
        }

        private MenuEntry FindActiveMenu()
        {
            if (preBattleMenuLocation?.activeInstance != null)
            {
                return preBattleMenuLocation;
            }

            // Prefer the menu tracked by the menu depth stack.
            var tracked = _menuTracker?.CurrentMenu;
            return tracked?.activeInstance != null ? tracked : null;
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
