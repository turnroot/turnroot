using System;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Characters
{
    /// <summary>
    /// Centralized access to character-related settings with safe defaults.
    /// Eliminates repetitive try-catch blocks for settings access across the codebase.
    /// Provides caching to minimize singleton lookups and Resources.Load calls.
    /// </summary>
    public static class CharacterSettings
    {
        private static int? _cachedMaxNonWeaponSlots;
        private static CharacterPrototypeSettings _cachedPrototypeSettings;
        private static DefaultCharacterStats _cachedDefaultStats;

        /// <summary>
        /// Get the maximum number of non-weapon items that can be equipped.
        /// Defaults to 2 if settings are unavailable.
        /// </summary>
        public static int MaxNonWeaponSlots
        {
            get
            {
                if (_cachedMaxNonWeaponSlots.HasValue)
                {
                    return _cachedMaxNonWeaponSlots.Value;
                }

                _cachedMaxNonWeaponSlots = GetOrDefault(
                    () => GameplayGeneralSettings.Instance.GetMaxEquippedNonWeaponItems(),
                    defaultValue: 2,
                    settingName: "MaxNonWeaponSlots"
                );

                return _cachedMaxNonWeaponSlots.Value;
            }
        }

        /// <summary>
        /// Get the character prototype settings.
        /// Loads from Resources/GameSettings if not cached.
        /// </summary>
        public static CharacterPrototypeSettings PrototypeSettings
        {
            get
            {
                if (_cachedPrototypeSettings != null)
                {
                    return _cachedPrototypeSettings;
                }

                _cachedPrototypeSettings = GameSettingsLoader.LoadFirst<CharacterPrototypeSettings>(
                    "GameSettings"
                );

                if (_cachedPrototypeSettings == null)
                {
#if UNITY_EDITOR
                    Debug.LogError(
                        "CharacterPrototypeSettings not found in Resources/GameSettings. Please create one."
                    );
#endif
                }

                return _cachedPrototypeSettings;
            }
        }

        /// <summary>
        /// Get the default character stats configuration.
        /// Loads from Resources/GameSettings if not cached.
        /// </summary>
        public static DefaultCharacterStats DefaultStats
        {
            get
            {
                if (_cachedDefaultStats != null)
                {
                    return _cachedDefaultStats;
                }

                _cachedDefaultStats = GameSettingsLoader.LoadFirst<DefaultCharacterStats>(
                    "GameSettings"
                );

                if (_cachedDefaultStats == null)
                {
#if UNITY_EDITOR
                    Debug.LogError(
                        "DefaultCharacterStats not found in Resources/GameSettings. Please create one."
                    );
#endif
                }

                return _cachedDefaultStats;
            }
        }

        /// <summary>
        /// Clear all cached settings. Call this when settings are changed in the editor
        /// or when entering/exiting play mode.
        /// </summary>
        public static void ClearCache()
        {
            _cachedMaxNonWeaponSlots = null;
            _cachedPrototypeSettings = null;
            _cachedDefaultStats = null;
        }

        /// <summary>
        /// Helper to safely retrieve a setting with a default fallback.
        /// Catches UnityException (Resources.Load during serialization) and general exceptions.
        /// </summary>
        private static T GetOrDefault<T>(
            System.Func<T> getter,
            T defaultValue,
            string settingName = "Setting"
        )
        {
            try
            {
                return getter();
            }
            catch (UnityEngine.UnityException)
            {
                // Resources.Load forbidden during Unity serialization - this is expected during
                // deserialization/constructor calls. Silently use default without logging.
                return defaultValue;
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogError(
                    $"Error loading {settingName}: {ex.Message}. Using default: {defaultValue}"
                );
#endif
                return defaultValue;
            }
        }

        #region Editor Support

#if UNITY_EDITOR
        /// <summary>
        /// Called when scripts are reloaded in the editor.
        /// Ensures caches are cleared to pick up changes.
        /// </summary>
        [UnityEditor.InitializeOnLoadMethod]
        private static void OnScriptsReloaded() => ClearCache();

        /// <summary>
        /// Called when entering play mode.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnEnterPlayMode() => ClearCache();
#endif

        #endregion
    }
}
