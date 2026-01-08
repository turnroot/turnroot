using System;
using System.Collections.Generic;
using System.Reflection;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.PlayerSettings;
using Turnroot.UI.Components.Menu.Submenu;
using UnityEngine;
using UnityEngine.UI;

namespace TurnrootFramework.Gameplay.Brain.Segments
{
    public class SettingsBindingManager
    {
        private readonly Dictionary<string, Func<GameplayPlayerSettings, object>> _settingsGetters;
        private readonly Dictionary<string, Action<GameObject, GamewideContextBrain>> _binders =
            new();

        public SettingsBindingManager()
        {
            _settingsGetters = new Dictionary<string, Func<GameplayPlayerSettings, object>>
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

            RegisterDefaultBinders();
        }

        private void RegisterDefaultBinders()
        {
            RegisterBinder<Slider>("slider", BindSlider);
            RegisterBinder<SimpleToggle>("toggle", BindToggle);
            RegisterBinder<MenuCarousel>("carousel", BindCarousel);
        }

        public void RegisterBinder<T>(string componentType, Action<T, GamewideContextBrain> binder)
            where T : Component
        {
            _binders[componentType.ToLower()] = (obj, context) =>
            {
                if (obj.GetComponent<T>() is T component)
                {
                    binder(component, context);
                }
            };
        }

        public void BindSettings(GameObject menuInstance, GamewideContextBrain context)
        {
            if (menuInstance == null || context?.PlayerSettings == null)
            {
                return;
            }

            // Find all PanelRow components and set up their events
            var panelRows = menuInstance.GetComponentsInChildren<PanelRow>();
            foreach (var panelRow in panelRows)
            {
                SetupPanelRowSettingsBinding(panelRow, context);
            }

            // Also look for direct components (not in PanelRows)
            var directToggles = menuInstance.GetComponentsInChildren<SimpleToggle>();
            foreach (var toggle in directToggles)
            {
                BindToggle(toggle, context);
            }

            var directSliders = menuInstance.GetComponentsInChildren<Slider>();
            foreach (var slider in directSliders)
            {
                BindSlider(slider, context);
            }

            var directCarousels = menuInstance.GetComponentsInChildren<MenuCarousel>();
            foreach (var carousel in directCarousels)
            {
                BindCarousel(carousel, context);
            }
        }

        private void SetupPanelRowSettingsBinding(PanelRow panelRow, GamewideContextBrain context)
        {
            if (panelRow == null)
            {
                return;
            }

            // Handle sliders in the panel row
            if (panelRow.rowType == PanelRow.RowType.Slider && panelRow.sliderComponent != null)
            {
                BindSlider(panelRow.sliderComponent, context);
            }

            // Handle ALL toggles in the panel row independently
            if (panelRow.rowType == PanelRow.RowType.Toggles && panelRow.toggleComponents != null)
            {
                foreach (var toggle in panelRow.toggleComponents)
                {
                    if (toggle != null)
                    {
                        BindToggle(toggle, context);
                    }
                }
            }

            // Handle carousels in the panel row
            if (panelRow.rowType == PanelRow.RowType.Carousel && panelRow.carouselComponent != null)
            {
                BindCarousel(panelRow.carouselComponent, context);
            }
        }

        private void BindSlider(Slider slider, GamewideContextBrain context)
        {
            if (slider == null || context?.PlayerSettings == null)
            {
                return;
            }

            string settingName = slider.gameObject.name.ToLower();

            if (!_settingsGetters.TryGetValue(settingName, out var getter))
            {
                return; // Unknown setting
            }

            // Set initial value
            var currentValue = getter(context.PlayerSettings);
            if (currentValue is float floatValue)
            {
                slider.value = floatValue;
            }
            else if (currentValue is int intValue)
            {
                slider.value = intValue;
            }

            // Set up change listener
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(value =>
            {
                context.UpdatePlayerSetting(slider.gameObject.name, value);
            });
        }

        private void BindToggle(SimpleToggle toggle, GamewideContextBrain context)
        {
            if (toggle == null || context?.PlayerSettings == null)
            {
                return;
            }

            string settingName = toggle.gameObject.name.ToLower();

            if (!_settingsGetters.TryGetValue(settingName, out var getter))
            {
                return; // Unknown setting
            }

            // Set initial value
            var currentValue = getter(context.PlayerSettings);
            if (currentValue is bool boolValue)
            {
                toggle.isOn = boolValue;
            }

            // Set up change listener
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(value =>
            {
                context.UpdatePlayerSetting(toggle.gameObject.name, value);
            });
        }

        private void BindCarousel(MenuCarousel carousel, GamewideContextBrain context)
        {
            if (carousel == null || context?.PlayerSettings == null)
            {
                return;
            }

            string settingName = carousel.gameObject.name.ToLower();

            if (!_settingsGetters.TryGetValue(settingName, out var getter))
            {
                return; // Unknown setting
            }

            // Set initial value - must verify it's an enum type before calling InitializeCarousel
            var currentValue = getter(context.PlayerSettings);
            if (currentValue != null && currentValue.GetType().IsEnum)
            {
                // Use reflection to call the generic method with the correct enum type
                var carouselType = typeof(MenuCarousel);
                var initMethod = carouselType.GetMethod("InitializeCarousel");
                var genericMethod = initMethod.MakeGenericMethod(currentValue.GetType());
                genericMethod.Invoke(carousel, new object[] { currentValue });
                carousel.UpdateDisplay();

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
                        context.UpdatePlayerSetting(carousel.gameObject.name, enumValue);
                    }
                };
            }
#if UNITY_EDITOR
            else
            {
                Debug.LogWarning(
                    $"SettingsBindingManager: Setting '{settingName}' is not an enum type. Carousel binding skipped."
                );
            }
#endif
        }
    }
}
