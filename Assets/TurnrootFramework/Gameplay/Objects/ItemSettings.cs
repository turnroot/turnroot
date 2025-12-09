using UnityEngine;

namespace Turnroot.Gameplay.Objects
{
    /// <summary>
    /// Centralized cache for item-related gameplay settings.
    /// Reduces repeated singleton access and provides safe defaults with automatic cache invalidation.
    /// </summary>
    public static class ItemSettings
    {
        private static bool? _cachedCanBeForged;
        private static bool? _cachedCanBeRepaired;
        private static bool? _cachedHaveDurability;
        private static bool? _cachedUseExperienceAptitudes;

        /// <summary>
        /// Gets whether weapons can be forged. Safe default: false.
        /// </summary>
        public static bool CanBeForged
        {
            get
            {
                if (_cachedCanBeForged.HasValue)
                {
                    return _cachedCanBeForged.Value;
                }

                try
                {
                    _cachedCanBeForged = GameplayGeneralSettings.Instance.GetWeaponsCanBeForged();
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning(
                        $"ItemSettings: Failed to load CanBeForged setting: {ex.Message}. Using default: false"
                    );
                    _cachedCanBeForged = false;
                }

                return _cachedCanBeForged.Value;
            }
        }

        /// <summary>
        /// Gets whether weapons can be repaired. Safe default: false.
        /// </summary>
        public static bool CanBeRepaired
        {
            get
            {
                if (_cachedCanBeRepaired.HasValue)
                {
                    return _cachedCanBeRepaired.Value;
                }

                try
                {
                    _cachedCanBeRepaired = GameplayGeneralSettings.Instance.GetWeaponsCanBeRepaired();
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning(
                        $"ItemSettings: Failed to load CanBeRepaired setting: {ex.Message}. Using default: false"
                    );
                    _cachedCanBeRepaired = false;
                }

                return _cachedCanBeRepaired.Value;
            }
        }

        /// <summary>
        /// Gets whether weapons have durability. Safe default: true.
        /// </summary>
        public static bool HaveDurability
        {
            get
            {
                if (_cachedHaveDurability.HasValue)
                {
                    return _cachedHaveDurability.Value;
                }

                try
                {
                    _cachedHaveDurability = GameplayGeneralSettings.Instance.GetWeaponsHaveDurability();
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning(
                        $"ItemSettings: Failed to load HaveDurability setting: {ex.Message}. Using default: true"
                    );
                    _cachedHaveDurability = true;
                }

                return _cachedHaveDurability.Value;
            }
        }

        /// <summary>
        /// Invalidates the cached settings. Call when settings are reloaded at runtime.
        /// </summary>
        public static void InvalidateCache()
        {
            _cachedCanBeForged = null;
            _cachedCanBeRepaired = null;
            _cachedHaveDurability = null;
            _cachedUseExperienceAptitudes = null;
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void InvalidateCacheOnRecompile() => InvalidateCache();
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InvalidateCacheOnPlayMode() => InvalidateCache();
    }
}
