using UnityEngine;

namespace Turnroot.Characters.Modules
{
    /// <summary>
    /// Encapsulates Bloodlines module functionality to reduce conditional compilation scattered throughout.
    /// Provides a consistent interface regardless of whether the module is enabled.
    /// </summary>
    public static class BloodlinesModule
    {
        /// <summary>
        /// Check if the Bloodlines module is enabled at compile time.
        /// </summary>
        public static bool IsEnabled
        {
            get
            {
#if TURNROOT_BLOODLINES_MODULE
                return true;
#else
                return false;
#endif
            }
        }

#if TURNROOT_BLOODLINES_MODULE
        /// <summary>
        /// Container for Bloodlines-specific character data.
        /// Only available when module is enabled.
        /// </summary>
        [System.Serializable]
        public class BloodlinesData
        {
            public Color HairColor;
            public Color EyeColor;
            public HereditaryTraits PassedDownTraits = new();
            public bool HasDesignatedChildUnit = false;
            public CharacterData ChildUnitId;
        }
#else
        /// <summary>
        /// Stub when module is disabled. All operations are no-ops.
        /// </summary>
        public class BloodlinesData
        {
            // Empty stub - all access returns defaults
        }
#endif

        /// <summary>
        /// Get hair color from Bloodlines data, or default if module disabled.
        /// </summary>
        public static Color GetHairColor(BloodlinesData data) =>
#if TURNROOT_BLOODLINES_MODULE
            return data?.HairColor ?? Color.black;
#else
            Color.black;
#endif


        /// <summary>
        /// Get eye color from Bloodlines data, or default if module disabled.
        /// </summary>
        public static Color GetEyeColor(BloodlinesData data) =>
#if TURNROOT_BLOODLINES_MODULE
            return data?.EyeColor ?? Color.blue;
#else
            Color.blue;
#endif


        /// <summary>
        /// Set hair color in Bloodlines data (no-op if module disabled).
        /// </summary>
        public static void SetHairColor(BloodlinesData data, Color color)
        {
#if TURNROOT_BLOODLINES_MODULE
            if (data != null)
                data.HairColor = color;
#endif
        }

        /// <summary>
        /// Set eye color in Bloodlines data (no-op if module disabled).
        /// </summary>
        public static void SetEyeColor(BloodlinesData data, Color color)
        {
#if TURNROOT_BLOODLINES_MODULE
            if (data != null)
                data.EyeColor = color;
#endif
        }
    }
}
