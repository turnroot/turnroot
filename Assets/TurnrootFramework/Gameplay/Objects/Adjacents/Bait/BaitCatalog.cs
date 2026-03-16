using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Gameplay.Objects.Bait
{
    [System.Serializable]
    public enum BaitStrength
    {
        Weak,
        Normal,
        Strong,
        Exceptional,
    }

    [System.Serializable]
    public struct BaitData
    {
        public BaitStrength Strength;
        public ObjectItem BaitItem;
    }

    [CreateAssetMenu(fileName = "BaitCatalog", menuName = "Turnroot/Objects/Bait Catalog")]
    public class BaitCatalog : SingletonScriptableObject<BaitCatalog>
    {
        public BaitData[] AllBaits;
    }
}
