using System.Collections;
using Turnroot.GameSettings;
using Turnroot.UI;
using Turnroot.UI.Components.Menu;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain.Segments
{
    public partial class UiBrain : BrainComponent
    {
        private MenuEntry GetValidatedMenuLocation(System.Func<MenuEntry> getter, string menuName)
        {
            if (uiSettings == null)
            {
                "UiBrain: GamewideUiSettings not found!".LogError();
                return null;
            }

            var entry = getter();
            if (entry == null)
            {
                $"UiBrain: {menuName} menu entry not found!".LogError();
            }

            return entry;
        }

        protected void WarnPrefabs()
        {
            // Validate all menu locations
            GetValidatedMenuLocation(() => settingsMenuLocation, "Game settings");
            GetValidatedMenuLocation(() => gameSettingsGraphicsLocation, "Game settings graphics");
            GetValidatedMenuLocation(() => gameSettingsExploreLocation, "Game settings explore");
            GetValidatedMenuLocation(() => gameSettingsGameplayLocation, "Game settings gameplay");
            GetValidatedMenuLocation(() => gameSettingsAudioLocation, "Game settings audio");

            GetValidatedMenuLocation(() => preBattleMenuLocation, "Pre-battle");
            GetValidatedMenuLocation(() => prebattleMapMenuLocation, "Pre-battle map");
            GetValidatedMenuLocation(() => prebattleUnitsMenuLocation, "Pre-battle units");
        }

        public void SetupMenuInputActions(MenuBase menu)
        {
            // Enable shared UI input actions for menus.
            UIInputActionDefaults.NavigateUp?.Enable();
            UIInputActionDefaults.NavigateDown?.Enable();
            UIInputActionDefaults.Select?.Enable();
        }

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

        public void TransitionToSubmenu(MenuEntry from, MenuEntry to)
        {
            if (_isTransitioning)
            {
                return;
            }

            _isTransitioning = true;
            _menuTracker?.TrackTransition(from, to);
            StartCoroutine(TransitionToSubmenuCoroutine(from, to));
        }

        private IEnumerator TransitionToSubmenuCoroutine(MenuEntry from, MenuEntry to)
        {
            // Depth already tracked at navigation start; do not re-track here to avoid duplicates
            yield return _transitionManager.TransitionBetween(from, to);
            _isTransitioning = false;
        }

        public OperationResult SetPreBattleMenuFadeSpeed(float fadeTime)
        {
            var preBattleMenuLocation = uiSettings?.GetPreBattleMenu();
            var uiFade = UIFadeCache.Get(preBattleMenuLocation?.activeInstance);
            if (uiFade != null)
            {
                uiFade.lerpTime = fadeTime;
                return OperationResult.Successful();
            }
            else
            {
                return OperationResult.Failure(
                    "SetPreBattleMenuFadeSpeed: Pre-battle menu or UIFade component not found."
                );
            }
        }

        public void HandleMenuSelect(UI.Components.MenuItemBase item) =>
            _routeHandler?.HandleMenuSelect(item);
    }
}
