using System.Collections.Generic;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.PlayerSettings;
using Turnroot.UI.Components.Menu;
using Turnroot.UI.Components.Menu.Submenu;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TurnrootFramework.Gameplay.Brain.Segments
{
    public partial class UiBrain : BrainComponent
    {
        #region Menu Input Setup

        private void SetupMenuInputActions(MenuBase menu)
        {
            // Force refresh menu items to make sure they're properly detected
            menu.RefreshMenuItems();

            // Create new InputActions with proper bindings for keyboard navigation
            if (menu.navigateUpAction == null || menu.navigateUpAction.bindings.Count == 0)
            {
                menu.navigateUpAction = new UnityEngine.InputSystem.InputAction(
                    "NavigateUp",
                    UnityEngine.InputSystem.InputActionType.Button
                );
                menu.navigateUpAction.AddBinding("<Keyboard>/w");
                menu.navigateUpAction.AddBinding("<Keyboard>/upArrow");
            }

            if (menu.navigateDownAction == null || menu.navigateDownAction.bindings.Count == 0)
            {
                menu.navigateDownAction = new UnityEngine.InputSystem.InputAction(
                    "NavigateDown",
                    UnityEngine.InputSystem.InputActionType.Button
                );
                menu.navigateDownAction.AddBinding("<Keyboard>/s");
                menu.navigateDownAction.AddBinding("<Keyboard>/downArrow");
            }

            if (menu.selectAction == null || menu.selectAction.bindings.Count == 0)
            {
                menu.selectAction = new UnityEngine.InputSystem.InputAction(
                    "Select",
                    UnityEngine.InputSystem.InputActionType.Button
                );
                menu.selectAction.AddBinding("<Keyboard>/enter");
                menu.selectAction.AddBinding("<Keyboard>/space");
            }

            // Enable the actions
            menu.navigateUpAction?.Enable();
            menu.navigateDownAction?.Enable();
            menu.selectAction?.Enable();
        }

        #endregion

        #region Settings UI Bindings

        private static readonly Dictionary<
            string,
            System.Func<GameplayPlayerSettings, object>
        > SettingsGetters = new()
        {
            { "brightness", settings => settings.Brightness },
            { "gamma", settings => settings.Gamma },
            { "quality", settings => settings.Quality },
            { "musicvolume", settings => settings.MusicVolume },
            { "sfxvolume", settings => settings.SfxVolume },
            { "voicevolume", settings => settings.VoiceVolume },
            { "tutorialprompts", settings => settings.TutorialPrompts },
            { "subtitles", settings => settings.Subtitles },
            { "bloom", settings => settings.Bloom },
            { "depthoffield", settings => settings.DepthOfField },
            { "lensflare", settings => settings.LensFlare },
            { "animatedcameramovement", settings => settings.AnimatedCameraMovement },
            { "autoendturn", settings => settings.AutoEndTurn },
            { "permadeath", settings => settings.Permadeath },
            { "disableturnwheel", settings => settings.DisableTurnwheel },
            { "disabletacticallens", settings => settings.DisableTacticalLens },
            { "musicwhenpaused", settings => settings.MusicWhenPaused },
            { "gamedifficulty", settings => settings.GameDifficulty },
            { "speedsetting", settings => settings.SpeedSetting },
            { "skipenemyturnanimations", settings => settings.SkipEnemyTurnAnimations },
            { "battlegridsetting", settings => settings.BattleGridSetting },
            { "battlegridstyle", settings => settings.BattleGridStyle },
            { "startunitsetting", settings => settings.StartUnitSetting },
            { "preferredbattlemusic", settings => settings.PreferredBattleMusic },
        };

        private void SetupSettingsUIBindings(GameObject menuInstance)
        {
            if (menuInstance == null)
            {
                return;
            }

            var gamewideContext = _brain.GetComponent<GamewideContextBrain>();
            if (gamewideContext?.PlayerSettings == null)
            {
                return;
            }

            // Find all PanelRow components and set up their events
            var panelRows = menuInstance.GetComponentsInChildren<PanelRow>();
            foreach (var panelRow in panelRows)
            {
                SetupPanelRowSettingsBinding(panelRow, gamewideContext);
            }

            // Also look for SimpleToggle components directly (not in PanelRows)
            var directToggles = menuInstance.GetComponentsInChildren<SimpleToggle>();
            foreach (var toggle in directToggles)
            {
                SetupGenericToggleBinding(toggle, gamewideContext);
            }

            // Also look for Slider components directly (not in PanelRows)
            var directSliders = menuInstance.GetComponentsInChildren<Slider>();
            foreach (var slider in directSliders)
            {
                SetupGenericSliderBinding(slider, gamewideContext);
            }
        }

        private void SetupPanelRowSettingsBinding(
            PanelRow panelRow,
            GamewideContextBrain gamewideContext
        )
        {
            if (panelRow == null)
            {
                return;
            }

            // Handle sliders in the panel row
            if (panelRow.rowType == PanelRow.RowType.Slider && panelRow.sliderComponent != null)
            {
                SetupGenericSliderBinding(panelRow.sliderComponent, gamewideContext);
            }

            // Handle ALL toggles in the panel row independently
            if (panelRow.rowType == PanelRow.RowType.Toggles && panelRow.toggleComponents != null)
            {
                foreach (var toggle in panelRow.toggleComponents)
                {
                    if (toggle != null)
                    {
                        SetupGenericToggleBinding(toggle, gamewideContext);
                    }
                }
            }

            // Handle carousels in the panel row
            if (panelRow.rowType == PanelRow.RowType.Carousel && panelRow.carouselComponent != null)
            {
                SetupGenericCarouselBinding(panelRow.carouselComponent, gamewideContext);
            }
        }

        private void SetupGenericSliderBinding(Slider slider, GamewideContextBrain gamewideContext)
        {
            if (slider == null || gamewideContext?.PlayerSettings == null)
            {
                return;
            }

            string settingName = slider.gameObject.name.ToLower();

            if (!SettingsGetters.TryGetValue(settingName, out var getter))
            {
                return; // Unknown setting
            }

            // Set initial value
            if (getter(gamewideContext.PlayerSettings) is float floatValue)
            {
                slider.value = floatValue;
            }
            else if (getter(gamewideContext.PlayerSettings) is int intValue)
            {
                slider.value = intValue;
            }

            // Set up change listener
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(value =>
            {
                gamewideContext.UpdatePlayerSetting(slider.gameObject.name, value);
            });
        }

        private void SetupGenericToggleBinding(
            SimpleToggle toggle,
            GamewideContextBrain gamewideContext
        )
        {
            if (toggle == null || gamewideContext?.PlayerSettings == null)
            {
                return;
            }

            string settingName = toggle.gameObject.name.ToLower();

            if (!SettingsGetters.TryGetValue(settingName, out var getter))
            {
                return; // Unknown setting
            }

            // Set initial value
            if (getter(gamewideContext.PlayerSettings) is bool boolValue)
            {
                toggle.isOn = boolValue;
            }

            // Set up change listener
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(value =>
            {
                gamewideContext.UpdatePlayerSetting(toggle.gameObject.name, value);
            });
        }

        private void SetupGenericCarouselBinding(
            MenuCarousel carousel,
            GamewideContextBrain gamewideContext
        )
        {
            if (carousel == null || gamewideContext?.PlayerSettings == null)
            {
                return;
            }

            string settingName = carousel.gameObject.name.ToLower();

            if (!SettingsGetters.TryGetValue(settingName, out var getter))
            {
                return; // Unknown setting
            }

            // Set initial value
            var currentValue = getter(gamewideContext.PlayerSettings);
            if (currentValue is System.Enum enumValue)
            {
                carousel.InitializeCarousel(enumValue);
                carousel.UpdateDisplay();
            }

            // Set up change listener
            carousel.onValueChanged += index =>
            {
                if (
                    carousel.OptionStringToEnumValue.TryGetValue(
                        carousel.Options[index],
                        out var enumValue
                    )
                )
                {
                    gamewideContext.UpdatePlayerSetting(carousel.gameObject.name, enumValue);
                }
            };
        }

        #endregion
    }
}
