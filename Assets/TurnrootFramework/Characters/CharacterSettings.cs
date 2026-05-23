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

        public static void ClearCache() => _cachedMaxNonWeaponSlots = null;

        /// <summary>
        /// Helper to safely retrieve a setting with a default fallback.
        /// Catches UnityException (Resources.Load during serialization) and general exceptions.
        /// </summary>
        private static T GetOrDefault<T>(
            Func<T> getter,
            T defaultValue,
            string settingName = "Setting"
        )
        {
            try
            {
                return getter();
            }
            catch (UnityException)
            {
                // Resources.Load forbidden during Unity serialization - this is expected during
                // deserialization/constructor calls. Silently use default without logging.
                return defaultValue;
            }
            catch (Exception ex)
            {
                $"{settingName} access failed: {ex.Message}. Using default value.".LogWarning();
                return defaultValue;
            }
        }

        #region Editor Support

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void OnScriptsReloaded() => ClearCache();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnEnterPlayMode() => ClearCache();
#endif

        #endregion
    }
}
