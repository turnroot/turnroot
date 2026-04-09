using System.Collections.Generic;
using Turnroot.Gameplay.Objects.Components;
using Turnroot.Utilities;
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

        public void OnValidate()
        {
            var i = 0;
            foreach (var bait in AllBaits)
            {
                i++;
                if (bait.BaitItem.Subtype != ObjectSubtype.Bait)
                {
                    $"BaitCatalog Validate: Bait item {bait.BaitItem.Name} does not have subtype Bait.".LogError();
                    var tempList = new List<BaitData>(AllBaits);
                    tempList.RemoveAt(i - 1);
                    AllBaits = tempList.ToArray();
                }
            }
        }
    }
}
