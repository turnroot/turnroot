using Turnroot.Gameplay.Brain;
using UnityEngine.SceneManagement;

namespace Turnroot.Utilities
{
    /// <summary>
    /// Helper to get the Brain component from an additive scene.
    /// Caches the result for performance. Call InvalidateCache() if the Brain might have changed.
    /// </summary>
    public static class GetBrain
    {
        private static Brain _cachedBrain;
        private static bool _cacheValid;

        static GetBrain()
        {
            // Invalidate cache when scenes change
            SceneManager.sceneLoaded += (_, _) => InvalidateCache();
            SceneManager.sceneUnloaded += _ => InvalidateCache();
        }

        /// <summary>
        /// Gets the Brain component, using cached value if available.
        /// </summary>
        public static Brain Get()
        {
            if (_cacheValid && _cachedBrain != null)
            {
                return _cachedBrain;
            }

            _cachedBrain = FindBrainInScenes();
            _cacheValid = _cachedBrain != null;
            return _cachedBrain;
        }

        /// <summary>
        /// Invalidates the cached Brain reference.
        /// Call this if you know the Brain has been destroyed or recreated.
        /// </summary>
        public static void InvalidateCache()
        {
            _cachedBrain = null;
            _cacheValid = false;
        }

        private static Brain FindBrainInScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                {
                    continue;
                }

                foreach (var rootObj in scene.GetRootGameObjects())
                {
                    var brain = rootObj.GetComponentInChildren<Brain>();
                    if (brain != null)
                    {
                        return brain;
                    }
                }
            }
            return null;
        }
    }
}
