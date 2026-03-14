using Turnroot.Gameplay.Objects;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Docks
{
    [System.Serializable]
    public struct SmuggledItem
    {
        public ObjectItem Item;
        public int Price;
        public string Description;
        public int MinimumTrustRequired;

        public readonly bool IsAvailable(int currentTrust) => currentTrust >= MinimumTrustRequired;
    }
}
