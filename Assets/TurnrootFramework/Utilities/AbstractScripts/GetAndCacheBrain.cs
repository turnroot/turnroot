using System;
using Turnroot.Gameplay.Brain;
using UnityEngine;

namespace Turnroot.Utilities
{
    /// <summary>
    /// Utility class for retrieving and caching the Brain instance in a static context.
    /// </summary>
    public static class GetAndCacheBrain
    {
        private static Brain _cachedBrain;

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
    }
}
