using UnityEngine;

namespace Turnroot.NonCombatScenes.Abstract
{
    public class FastTravelManager : MonoBehaviour
    {
        public bool IsLocationAvailable(UnlockableWorldLocation location) => location != null && location.fastTravelPoint != null && location.isUnlocked;
    }
}
