using UnityEngine;

namespace Turnroot.NonCombatScenes.Abstract
{
    public class FastTravelManager : MonoBehaviour
    {
        public bool IsLocationAvailable(UnlockableWorldLocation location)
        {
            if (location == null || location.fastTravelPoint == null)
            {
                return false;
            }

            return location.isUnlocked;
        }
    }
}
