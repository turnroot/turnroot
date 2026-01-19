using System;
using System.Collections.Generic;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    /// <summary>
    /// Condition to protect specific tiles from being captured or lost.
    /// </summary>
    [Serializable]
    public class ProtectTilesBattleCondition : BattleCondition
    {
        [SerializeField]
        public Vector2Int[] TilesToProtect;

        [SerializeField]
        public int MustProtectCount = 0;

        public ProtectTilesBattleCondition(
            string name,
            string description,
            Vector2Int[] tilesToProtect,
            int mustProtectCount = 0
        )
            : base(name, description)
        {
            TilesToProtect = tilesToProtect ?? Array.Empty<Vector2Int>();
            MustProtectCount = mustProtectCount;
        }

        public ProtectTilesBattleCondition()
            : base("Protect Tiles", "Protect the listed tiles")
        {
            TilesToProtect = Array.Empty<Vector2Int>();
        }

        public void CheckCondition(Dictionary<Vector2Int, bool> tileStatus)
        {
            foreach (var tile in TilesToProtect)
            {
                // Use TryGetValue to avoid double lookup
                if (tileStatus.TryGetValue(tile, out var isProtected) && isProtected == false)
                {
                    ConditionFailed();
                }
            }
        }
    }
}
