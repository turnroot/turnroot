using Turnroot.Gameplay.Brain;
using Turnroot.GameSettings;
using Turnroot.UI.Components;
using Turnroot.UI.Components.Menu;
using Turnroot.UI.Components.RadialMenu;
using UnityEngine;

namespace TurnrootFramework.Gameplay.Brain.Segments
{
    public partial class UiBrain : BrainComponent
    {
        #region Settings Menu Opening and Core Operations

        private void OpenPrebattleSubmenu(
            System.Func<MenuLocation> getMenuLocation,
            string menuTypeName
        )
        {
            if (_isTransitioning)
            {
                return;
            }

            var submenuLocation = getMenuLocation?.Invoke();
            if (submenuLocation == null)
            {
                return;
            }

            // If the pre-battle menu instance doesn't exist, fall back to the currently active menu as source
            MenuLocation sourceMenu = preBattleMenuLocation;
            if (preBattleMenuLocation?.activeInstance == null)
            {
                // Find any active menu to use as the source for the transition
                var all = uiSettings?.allPossibleMenuLocations;
                if (all != null)
                {
                    foreach (var m in all)
                    {
                        if (m?.activeInstance != null)
                        {
                            sourceMenu = m;
                            break;
                        }
                    }
                }

                if (sourceMenu?.activeInstance == null)
                {
#if UNITY_EDITOR
                    Debug.LogError(
                        "UiBrain: No active menu instance found to use as transition source"
                    );
#endif
                    return;
                }
            }

            // Guard: Return early if activeInstance already exists to prevent duplicates
            if (submenuLocation.activeInstance != null)
            {
                return;
            }

            if (submenuLocation.prefab == null)
            {
#if UNITY_EDITOR
                Debug.LogError($"UiBrain: No prefab set for {menuTypeName} menu location");
#endif
                return;
            }

            _isTransitioning = true;

            // Start the transition coroutine using the resolved source
            StartCoroutine(TransitionToSettingsMenu(sourceMenu, submenuLocation));
        }

        public void OpenMainGameSettingsMenu() =>
            OpenPrebattleSubmenu(() => uiSettings?.GetGameSettingsMenu(), "game settings");

        public void OpenPreBattleMapOverview() =>
            OpenPrebattleSubmenu(() => uiSettings?.GetPrebattleMapMenu(), "pre-battle map");

        public void OpenPreBattleUnitsMenu() =>
            OpenPrebattleSubmenu(() => uiSettings?.GetPrebattleUnitsMenu(), "pre-battle units");

        #endregion

        #region Settings Menu Event Handlers

        public void HandleGameSettingsMenuNavigate(MenuItemBase item) =>
            // Delegate to the route handler for unified menu handling
            _routeHandler?.HandleMenuNavigate(item);

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

        public void BackToPreBattleMenu() => BackToPreBattleMenuFromActiveSubmenu();

        public void BackToPreBattleMenuFromMap() => BackToPreBattleMenuFromActiveSubmenu();

        private void BackToPreBattleMenuFromActiveSubmenu()
        {
            if (_isTransitioning)
            {
                return;
            }

            // Find which submenu is currently active
            var activeSubmenu = GetActivePrebattleSubmenu();
            if (activeSubmenu == null)
            {
#if UNITY_EDITOR
                Debug.LogError("UiBrain: No active prebattle submenu found");
#endif
                return;
            }

            if (preBattleMenuLocation == null)
            {
#if UNITY_EDITOR
                Debug.LogError("UiBrain: Pre-battle menu location not found");
#endif
                return;
            }

            if (preBattleMenuLocation.prefab == null)
            {
#if UNITY_EDITOR
                Debug.LogError("UiBrain: No prefab set for pre-battle menu location");
#endif
                return;
            }

            _isTransitioning = true;

            // Start the transition coroutine back to prebattle
            StartCoroutine(TransitionBackToPreBattleMenu(activeSubmenu, preBattleMenuLocation));
        }

        private MenuLocation GetActivePrebattleSubmenu()
        {
            // Check all possible prebattle submenus
            var possibleSubmenus = new MenuLocation[]
            {
                settingsMenuLocation,
                uiSettings?.GetPrebattleMapMenu(),
            };

            foreach (var submenu in possibleSubmenus)
            {
                if (submenu?.activeInstance != null)
                {
                    return submenu;
                }
            }

            return null;
        }

        private System.Collections.IEnumerator TransitionToSettingsMenu(
            MenuLocation fromMenuLocation,
            MenuLocation toMenuLocation
        )
        {
            // Track the transition for depth management
            _menuTracker?.TrackTransition(fromMenuLocation, toMenuLocation);

            // Use the transition manager instead of duplicate code
            yield return _transitionManager.TransitionBetween(
                fromMenuLocation,
                toMenuLocation,
                destroyFrom: fromMenuLocation == preBattleMenuLocation
            );

            _isTransitioning = false;
        }

        private System.Collections.IEnumerator TransitionBackToPreBattleMenu(
            MenuLocation settingsMenuLocation,
            MenuLocation preBattleMenuLocation
        )
        {
            // Track the transition for depth management
            _menuTracker?.TrackTransition(settingsMenuLocation, preBattleMenuLocation);

            // Use the transition manager for consistent behavior
            yield return _transitionManager.TransitionToPreBattle(
                settingsMenuLocation,
                preBattleMenuLocation
            );

            _isTransitioning = false;
        }
        #endregion
    }
}
