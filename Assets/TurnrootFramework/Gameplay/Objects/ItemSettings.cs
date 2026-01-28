using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Objects
{
    /// <summary>
    /// Centralized cache for item-related gameplay settings.
    /// Reduces repeated singleton access and provides safe defaults with automatic cache invalidation.
    /// </summary>
    public static class ItemSettings
    {
        private static readonly SingleValueCache<bool> _canBeForgedCache = new();
        private static readonly SingleValueCache<bool> _canBeRepairedCache = new();
        private static readonly SingleValueCache<bool> _haveDurabilityCache = new();
        private static readonly SingleValueCache<bool> _useExperienceAptitudesCache = new();

        /// <summary>
        /// Gets whether weapons can be forged. Safe default: false.
        /// </summary>
        public static bool CanBeForged
        {
            get
            {
                return _canBeForgedCache.GetOrCompute(() =>
                {
                    try
                    {
                        return GameplayGeneralSettings.Instance.WeaponsCanBeForged;
                    }
                    catch (System.Exception ex)
                    {
                        TurnrootLogger.Log(
                            $"ItemSettings: Failed to load CanBeForged setting: {ex.Message}. Using default: false",
                            TurnrootLogger.LogLevel.Warning
                        );
                        return false;
                    }
                });
            }
        }

        /// <summary>
        /// Gets whether weapons can be repaired. Safe default: false.
        /// </summary>
        public static bool CanBeRepaired
        {
            get
            {
                return _canBeRepairedCache.GetOrCompute(() =>
                {
                    try
                    {
                        return GameplayGeneralSettings.Instance.WeaponsCanBeRepaired;
                    }
                    catch (System.Exception ex)
                    {
                        TurnrootLogger.Log(
                            $"ItemSettings: Failed to load CanBeRepaired setting: {ex.Message}. Using default: false"
                        );
                        return false;
                    }
                });
            }
        }

        /// <summary>
        /// Gets whether weapons have durability. Safe default: true.
        /// </summary>
        public static bool HaveDurability
        {
            get
            {
                return _haveDurabilityCache.GetOrCompute(() =>
                {
                    try
                    {
                        return GameplayGeneralSettings.Instance.WeaponsHaveDurability;
                    }
                    catch (System.Exception ex)
                    {
                        TurnrootLogger.Log(
                            $"ItemSettings: Failed to load HaveDurability setting: {ex.Message}. Using default: true",
                            TurnrootLogger.LogLevel.Warning
                        );
                        return true;
                    }
                });
            }
        }

        /// <summary>
        /// Invalidates the cached settings. Call when settings are reloaded at runtime.
        /// </summary>
        public static void InvalidateCache()
        {
            _canBeForgedCache.Invalidate();
            _canBeRepairedCache.Invalidate();
            _haveDurabilityCache.Invalidate();
            _useExperienceAptitudesCache.Invalidate();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void InvalidateCacheOnRecompile() => InvalidateCache();
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InvalidateCacheOnPlayMode() => InvalidateCache();
    }
}
