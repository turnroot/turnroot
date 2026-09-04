using Turnroot.Gameplay.Brain;
using Turnroot.UI;

namespace Turnroot.Utilities
{
    /// <summary>
    /// Utility class for retrieving and caching the Brain instance in a static context.
    /// </summary>
    public static class GetAndCacheBrain
    {
        private static Brain _cachedBrain;

        private static UiInputProvider _cachedInputProvider;

        public static Brain GetBrain()
        {
            if (_cachedBrain != null)
            {
                return _cachedBrain;
            }
            else
            {
                "GetAndCacheBrain: Brain instance not cached, searching".LogInfo();
            }

            _cachedBrain = UnityEngine.Object.FindFirstObjectByType<Brain>();
            if (_cachedBrain == null)
            {
                "GetAndCacheBrain: No Brain instance found.".LogError();
            }
            return _cachedBrain;
        }

        public static UiInputProvider GetInputProvider()
        {
            if (_cachedInputProvider != null)
            {
                return _cachedInputProvider;
            }
            else
            {
                "GetAndCacheBrain: InputProvider instance not cached, searching".LogInfo();
            }

            _cachedInputProvider = UnityEngine.Object.FindFirstObjectByType<UiInputProvider>();
            if (_cachedInputProvider == null)
            {
                "GetAndCacheBrain: No InputProvider instance found.".LogError();
            }
            return _cachedInputProvider;
        }
    }
}
