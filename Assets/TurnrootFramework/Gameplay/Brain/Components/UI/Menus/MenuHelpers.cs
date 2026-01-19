using System.Collections;
using Turnroot.GameSettings;
using Turnroot.UI.Components.Menu;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Gameplay.Brain.Segments
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
            GetValidatedMenuLocation(() => prebattleMapMenuLocation, "Pre-battle map");
            GetValidatedMenuLocation(() => prebattleUnitsMenuLocation, "Pre-battle units");
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

        public void TransitionToSubmenu(MenuLocation from, MenuLocation to)
        {
            if (_isTransitioning)
            {
                return;
            }

            _isTransitioning = true;
            _menuTracker?.TrackTransition(from, to);
            StartCoroutine(TransitionToSubmenuCoroutine(from, to));
        }

        private IEnumerator TransitionToSubmenuCoroutine(MenuLocation from, MenuLocation to)
        {
            // Depth already tracked at navigation start; do not re-track here to avoid duplicates
            yield return _transitionManager.TransitionBetween(from, to);
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

        public void HandleMenuSelect(UI.Components.MenuItemBase item) =>
            _routeHandler?.HandleMenuSelect(item);
    }
}
