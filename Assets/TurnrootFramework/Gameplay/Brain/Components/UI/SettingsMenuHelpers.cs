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

            if (preBattleMenuLocation?.activeInstance == null)
            {
#if UNITY_EDITOR
                Debug.LogError("UiBrain: Pre-battle menu instance not found");
#endif
                return;
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

            // Start the transition coroutine
            StartCoroutine(TransitionToSettingsMenu(preBattleMenuLocation, submenuLocation));
        }

        public void OpenMainGameSettingsMenu() =>
            OpenPrebattleSubmenu(() => uiSettings?.GetGameSettingsMenu(), "game settings");

        public void OpenPreBattleMapOverview() =>
            OpenPrebattleSubmenu(() => uiSettings?.GetPrebattleMapMenu(), "pre-battle map");

        #endregion

        #region Settings Menu Event Handlers

        public void HandleGameSettingsMenuNavigate(MenuItemBase item) =>
            // Delegate to the route handler for unified menu handling
            _routeHandler?.HandleMenuNavigate(item);

        public void HandleGameSettingsMenuSelect(MenuItemBase item) =>
            // Delegate to the route handler for unified menu handling
            _routeHandler?.HandleMenuSelect(item);

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

        protected System.Collections.IEnumerator TransitionBackToSettingsMenu(
            MenuLocation currentMenuLocation,
            MenuLocation parentMenuLocation
        )
        {
            // Track the transition for depth management
            _menuTracker?.TrackTransition(currentMenuLocation, parentMenuLocation);

            // Use the transition manager for consistent behavior
            yield return _transitionManager.TransitionBetween(
                currentMenuLocation,
                parentMenuLocation,
                destroyFrom: true
            );

            _isTransitioning = false;
        }

        #endregion

        #region Menu Cleanup and Styling

        private void CleanupPreBattleMenu(GameObject preBattleInstance)
        {
            if (preBattleInstance.TryGetComponent<RadialMenu>(out var radialMenu))
            {
                radialMenu.OnNavigate -= HandlePreBattleMenuNavigate;
                radialMenu.OnItemSelected -= HandlePreBattleMenuSelect;
            }

            if (preBattleInstance.TryGetComponent<MenuBase>(out var menu))
            {
                menu.OnNavigate -= HandlePreBattleMenuNavigate;
                menu.OnItemSelected -= HandlePreBattleMenuSelect;
            }
        }

        private void ApplyGridListFilmstripColors(GameObject menuInstance)
        {
            // Apply grid/list/filmstrip colors to button components
            var buttons = menuInstance.GetComponentsInChildren<UnityEngine.UI.Button>();
            foreach (var button in buttons)
            {
                var colorBlock = button.colors;
                colorBlock.normalColor = uiSettings.GridListFilmstripButtonNormalColor;
                colorBlock.highlightedColor = uiSettings.GridListFilmstripButtonHoveredColor;
                colorBlock.selectedColor = uiSettings.GridListFilmstripButtonSelectedColor;
                colorBlock.fadeDuration = uiSettings.ButtonTransitionDuration;
                button.colors = colorBlock;
            }
        }

        #endregion
    }
}
