using System;
using System.Collections.Generic;
using Turnroot.Gameplay.PlayerSettings;
using Turnroot.UI.Components.Menu.Submenu;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.Gameplay.Brain.Segments
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
                { "contrast", settings => settings.Contrast },
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
                { "preferredinputcontrol", settings => settings.PreferredInputControl },
            };

            RegisterDefaultBinders();
        }

        private void RegisterDefaultBinders()
        {
            RegisterBinder<Slider>("slider", BindSlider);
            RegisterBinder<SimpleToggle>("toggle", BindToggle);
            RegisterBinder<MenuCarousel>("carousel", BindCarousel);
        }

        /// <summary>
        /// Normalizes a setting name by converting to lowercase and removing all non-alphanumeric characters.
        /// This ensures consistent matching with the settings dictionary keys.
        /// </summary>
        private static string NormalizeSettingName(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            var lower = input.ToLower();
            var sb = new System.Text.StringBuilder(lower.Length);
            foreach (var ch in lower)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
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
                // Try to infer the binding name. Sliders are often named "Slider" by prefab, so
                // prefer the panel row name or label text when available.
                string inferredSetting = null;
                string[] candidates = new string[]
                {
                    panelRow.sliderComponent.gameObject.name,
                    panelRow.gameObject.name,
                    panelRow.labelText != null ? panelRow.labelText.text : null,
                };

                foreach (var c in candidates)
                {
                    if (string.IsNullOrEmpty(c))
                    {
                        continue;
                    }

                    var normalized = NormalizeSettingName(c);

                    if (_settingsGetters.ContainsKey(normalized))
                    {
                        inferredSetting = normalized;
                        break;
                    }
                }

                BindSlider(panelRow.sliderComponent, context, inferredSetting);
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

        private void BindSlider(Slider slider, GamewideContextBrain context) =>
            BindSlider(slider, context, null);

        private void BindSlider(
            Slider slider,
            GamewideContextBrain context,
            string overrideSettingName
        )
        {
            if (slider == null || context?.PlayerSettings == null)
            {
                return;
            }

            // Allow callers to pass an inferred/normalized name; otherwise use the slider object name
            string settingName = overrideSettingName ?? slider.gameObject.name;
            // Normalize the setting name (lowercase + alphanumeric only)
            settingName = NormalizeSettingName(settingName);

            if (!_settingsGetters.TryGetValue(settingName, out var getter))
            {
                return; // Unknown setting
            }

            // Set slider min/max depending on the setting.
            // For consistent UI steps we use normalized sliders (0..1) for most settings and map
            // that normalized range to real gameplay values. Quality remains an exception.
            if (settingName == "quality")
            {
                slider.minValue = 0f;
                slider.maxValue = 0.3f;
            }
            else
            {
                // Use normalized 0..1 range for brightness, contrast and all other sliders so
                // panel input steps (0.1) map to a single stored step.
                slider.minValue = 0f;
                slider.maxValue = 1f;
            }

            // Set initial value (quantize for quality to tenths and map normalized sliders to gameplay ranges)
            var currentValue = getter(context.PlayerSettings);
            if (currentValue is float floatValue)
            {
                if (settingName == "quality")
                {
                    slider.value = Mathf.Clamp(Mathf.Round(floatValue * 10f) / 10f, 0f, 0.3f);
                }
                else if (settingName == "brightness")
                {
                    // Map stored brightness (-2..2) to normalized 0..1 and snap to tenths so UI steps match storage
                    var normalized = Mathf.Clamp01((floatValue + 2f) / 4f);
                    slider.value = Mathf.Round(normalized * 10f) / 10f;
                }
                else if (settingName == "contrast")
                {
                    // Map stored contrast (-50..50) to normalized 0..1 and snap to tenths
                    var normalized = Mathf.Clamp01((floatValue + 50f) / 100f);
                    slider.value = Mathf.Round(normalized * 10f) / 10f;
                }
                else
                {
                    slider.value = floatValue;
                }
            }
            else if (currentValue is int intValue)
            {
                slider.value = intValue;
            }

            // Debug: confirm binding for investigation
            TurnrootLogger.Log(
                $"SettingsBindingManager: Bound slider '{slider.gameObject.name}' to setting '{settingName}'"
            );

            // Set up change listener - quantize quality to tenths (max 0.3) and update settings
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(value =>
            {
                // Quantize normalized slider input to tenths so each left/right step maps to one stored step
                var norm = Mathf.Round(value * 10f) / 10f;
                if (Mathf.Abs(slider.value - norm) > 0.0001f)
                {
                    slider.value = norm; // keep UI in sync with quantized step
                }

                float mappedValue = norm;
                if (settingName == "quality")
                {
                    // quality is stored directly in 0..0.3 (already quantized)
                    mappedValue = Mathf.Clamp(norm, 0f, 0.3f);
                }
                else if (settingName == "brightness")
                {
                    // map normalized 0..1 to -2..2
                    mappedValue = norm * 4f - 2f;
                }
                else if (settingName == "contrast")
                {
                    // map normalized 0..1 to -50..50
                    mappedValue = norm * 100f - 50f;
                }

                context.UpdatePlayerSetting(settingName, mappedValue);
            });
        }

        private void BindToggle(SimpleToggle toggle, GamewideContextBrain context)
        {
            if (toggle == null || context?.PlayerSettings == null)
            {
                return;
            }

            string settingName = NormalizeSettingName(toggle.gameObject.name);

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
                context.UpdatePlayerSetting(settingName, value);
            });
        }

        private void BindCarousel(MenuCarousel carousel, GamewideContextBrain context)
        {
            if (carousel == null || context?.PlayerSettings == null)
            {
                return;
            }

            string settingName = NormalizeSettingName(carousel.gameObject.name);

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
                        context.UpdatePlayerSetting(settingName, enumValue);
                    }
                };
            }
        }
    }
}
