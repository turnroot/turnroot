using System.Collections;
using Turnroot.Gameplay.Brain;
using Turnroot.GameSettings;
using Turnroot.UI.Components.Menu;
using UnityEngine;

namespace TurnrootFramework.Gameplay.Brain.Segments
{
    public partial class UiBrain : BrainComponent
    {
        private MenuLocation GetValidatedMenuLocation(
            System.Func<MenuLocation> getter,
            string menuName
        )
        {
            if (uiSettings == null)
            {
                Debug.LogError("UiBrain: GamewideUiSettings not found!");
                return null;
            }

            var location = getter();
            if (location == null)
            {
                Debug.LogError($"UiBrain: {menuName} menu location not found!");
            }

            return location;
        }

        protected void WarnPrefabs()
        {
            // Validate all menu locations
            GetValidatedMenuLocation(() => settingsMenuLocation, "Game settings");
            GetValidatedMenuLocation(() => gameSettingsGraphicsLocation, "Game settings graphics");
            GetValidatedMenuLocation(() => gameSettingsGameplayLocation, "Game settings gameplay");
            GetValidatedMenuLocation(() => gameSettingsAudioLocation, "Game settings audio");
            GetValidatedMenuLocation(() => gameSettingsControlsLocation, "Game settings controls");
            GetValidatedMenuLocation(() => preBattleMenuLocation, "Pre-battle");
        }

        public void SetupMenuInputActions(MenuBase menu) =>
            InputActionFactory.SetupMenuNavigation(menu);

        public void SetupSettingsUIBindings(GameObject instance) =>
            _settingsBindingManager?.BindSettings(
                instance,
                _brain.GetComponent<GamewideContextBrain>()
            );

        public void ApplyMenuColors(GameObject instance, MenuStyle style)
        {
            if (uiSettings == null)
            {
                return;
            }

            if (style == MenuStyle.Pie)
            {
                // Radial menus pull colors automatically
                return;
            }

            // Apply grid/list/filmstrip colors
            var buttons = instance.GetComponentsInChildren<UnityEngine.UI.Button>();
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

        public void TransitionToSubmenu(MenuLocation from, MenuLocation to) =>
            TransitionToSubmenu(from, to, isBackNavigation: false);

        public void TransitionToSubmenu(MenuLocation from, MenuLocation to, bool isBackNavigation)
        {
            if (_isTransitioning)
            {
                return;
            }

            _isTransitioning = true;
            StartCoroutine(TransitionToSubmenuCoroutine(from, to, isBackNavigation));
        }

        private IEnumerator TransitionToSubmenuCoroutine(
            MenuLocation from,
            MenuLocation to,
            bool isBackNavigation = false
        )
        {
            if (!isBackNavigation)
            {
                _menuTracker?.TrackTransition(from, to);
            }

            // For back navigation, don't destroy the 'from' menu so we can return to it later
            // For forward navigation, we can destroy sub-menus but preserve main menus
            bool destroyFrom =
                !isBackNavigation
                && (
                    from == gameSettingsGraphicsLocation
                    || from == gameSettingsGameplayLocation
                    || from == gameSettingsAudioLocation
                    || from == gameSettingsControlsLocation
                );

            yield return _transitionManager.TransitionBetween(from, to, destroyFrom);
            _isTransitioning = false;
        }

        public void SetPreBattleMenuFadeSpeed(float fadeTime)
        {
            var preBattleMenuLocation = uiSettings?.GetPreBattleMenu();
            if (
                preBattleMenuLocation?.activeInstance != null
                && preBattleMenuLocation.activeInstance.TryGetComponent<UIFade>(out var uiFade)
            )
            {
                uiFade.lerpTime = fadeTime;
            }
        }

        // Menu event handlers for route system
        public void HandleMenuNavigate(Turnroot.UI.Components.MenuItemBase item) =>
            _routeHandler?.HandleMenuNavigate(item);

        public void HandleMenuSelect(Turnroot.UI.Components.MenuItemBase item) =>
            _routeHandler?.HandleMenuSelect(item);
    }
}
