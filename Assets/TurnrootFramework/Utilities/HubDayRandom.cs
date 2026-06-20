using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.NonCombatScenes.Hub;

namespace Turnroot.Utilities
{
    /// <summary>
    /// Provides deterministic randomization for "hub day" state.
    ///
    /// When initialized with a seed derived from the current date, all random
    /// choices driven through this helper will be repeatable across play sessions.
    /// </summary>
    public static class HubDayRandom
    {
        private static System.Random _rng;

        /// <summary>
        /// True once <see cref="Initialize"/> has been called.
        /// </summary>
        public static bool IsInitialized => _rng != null;

        private static void EnsureInitialized()
        {
            if (IsInitialized)
            {
                return;
            }

            var brain = GetAndCacheBrain.GetBrain();
            if (brain == null || brain.ltm == null)
            {
                return;
            }

            var date = brain.ltm.GetGameDate();
            if (date == GameDate.Default)
            {
                return;
            }

            HubDayStateStore.Initialize(brain, date);
            Initialize(HubDayStateStore.Seed);
        }

        /// <summary>
        /// Initialize the deterministic RNG for the current hub day.
        /// </summary>
        public static void Initialize(int seed) => _rng = new System.Random(seed);

        /// <summary>
        /// Clears the current deterministic RNG.
        /// </summary>
        public static void Reset() => _rng = null;

        /// <summary>
        /// Gets a random float in [0,1).
        /// </summary>
        public static float Value
        {
            get
            {
                EnsureInitialized();
                return IsInitialized ? (float)_rng.NextDouble() : UnityEngine.Random.value;
            }
        }

        /// <summary>
        /// Returns a random float between min [inclusive] and max [exclusive].
        /// </summary>
        public static float Range(float min, float max)
        {
            EnsureInitialized();
            return IsInitialized ? (float)(_rng.NextDouble() * (max - min) + min) : UnityEngine.Random.Range(min, max);
        }

        /// <summary>
        /// Returns a random int between min [inclusive] and max [exclusive].
        /// </summary>
        public static int Range(int min, int max)
        {
            EnsureInitialized();
            return IsInitialized ? _rng.Next(min, max) : UnityEngine.Random.Range(min, max);
        }

        /// <summary>
        /// Returns a random boolean value.
        /// </summary>
        public static bool Bool() => Value < 0.5f;
    }
}
