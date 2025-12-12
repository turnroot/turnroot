namespace Turnroot.Constants
{
    /// <summary>
    /// Game-wide constants for default values, limits, and thresholds.
    /// </summary>
    public static class GameConstants
    {
        /// <summary>
        /// Default stat values for character initialization.
        /// </summary>
        public static class DefaultStats
        {
            // Bounded stat defaults
            public const float DefaultMaxHealth = 100f;
            public const float DefaultCurrentHealth = 100f;
            public const float DefaultMinHealth = 0f;

            public const float DefaultMaxLevel = 99f;
            public const float DefaultStartingLevel = 1f;
            public const float DefaultMinLevel = 1f;

            public const float DefaultMaxExperience = 100f;
            public const float DefaultStartingExperience = 0f;
            public const float DefaultMinExperience = 0f;

            // Unbounded stat defaults
            public const float DefaultCoreStatValue = 10f;
            public const float DefaultLuckValue = 5f;
            public const float DefaultAuthorityValue = 5f;
            public const float DefaultCriticalAvoidanceValue = 0f;
        }

        /// <summary>
        /// Combat-related constants.
        /// </summary>
        public static class Combat
        {
            public const float DefaultCriticalMultiplier = 3f;
            public const int DefaultWeaponTriangleAdvantage = 20;
            public const int DefaultWeaponTriangleDisadvantage = -20;
            public const int DefaultMagicTriangleAdvantage = 20;
            public const int DefaultMagicTriangleDisadvantage = -20;
            public const int DefaultCombatArtLimit = 3;
            public const int DefaultMaxEquippedSkills = 0;
            public const int DefaultBattalionLimit = 1;
        }

        /// <summary>
        /// Indexing and collection constants.
        /// </summary>
        public static class Collections
        {
            public const int FirstElementIndex = 0;
            public const int InvalidIndex = -1;
            public const int EmptyCount = 0;
        }

        /// <summary>
        /// Range and distance constants.
        /// </summary>
        public static class Range
        {
            public const int UnlimitedRange = 0;
            public const int DefaultMinRange = 0;
            public const int DefaultMaxRange = 0;
            public const int MaxWarpDistance = 20;
        }
    }
}
