using UnityEngine;

namespace Turnroot.NonCombatScenes.Abstract
{
    public class FastTravelManager : MonoBehaviour
    {
        [Tooltip("Unlockable locations that gate whether a fast-travel destination is available.")]
        public UnlockableWorldLocation[] UnlockableLocations;

        public bool IsLocationAvailable(Transform fastTravelPoint)
        {
            if (fastTravelPoint == null || UnlockableLocations == null)
            {
                return false;
            }

            for (int i = 0; i < UnlockableLocations.Length; i++)
            {
                var location = UnlockableLocations[i];
                if (location == null)
                {
                    continue;
                }

                if (location.fastTravelPoint == fastTravelPoint)
                {
                    return location.isUnlocked;
                }
            }

            return false;
        }
    }
}
