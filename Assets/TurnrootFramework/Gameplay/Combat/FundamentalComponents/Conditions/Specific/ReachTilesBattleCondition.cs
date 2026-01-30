using System;
using System.Collections.Generic;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    /// <summary>
    /// Condition to occupy specific tiles on the battlefield.
    /// Uses List instead of arrays for efficient dynamic collection management.
    /// </summary>
    [Serializable]
    public class ReachTilesBattleCondition : BattleCondition
    {
        [SerializeField]
        public List<Vector2Int> TargetTiles = new();

        [SerializeField]
        private HashSet<Vector2Int> _reachedTilesSet = new();

        /// <summary>
        /// Read-only access to reached tiles for serialization and debugging.
        /// </summary>
        public IReadOnlyCollection<Vector2Int> ReachedTiles => _reachedTilesSet;

        [SerializeField]
        public bool allTiles = true;

        public ReachTilesBattleCondition(
            string name,
            string description,
            List<Vector2Int> targetTiles,
            bool allTiles = true
        )
            : base(name, description)
        {
            TargetTiles = targetTiles ?? new List<Vector2Int>();
            this.allTiles = allTiles;
        }

        public ReachTilesBattleCondition()
            : base("Reach Tiles", "Occupy the listed tiles")
        {
            TargetTiles = new List<Vector2Int>();
            _reachedTilesSet = new HashSet<Vector2Int>();
            allTiles = true;
        }

        public void CheckCondition()
        {
            if (!AreRequirementsMet())
            {
                return;
            }

            if (allTiles)
            {
                // All target tiles must be reached
                foreach (var tile in TargetTiles)
                {
                    if (!_reachedTilesSet.Contains(tile))
                    {
                        return;
                    }
                }
                ConditionMet();
            }
            else
            {
                // Any target tile reached is sufficient
                foreach (var tile in TargetTiles)
                {
                    if (_reachedTilesSet.Contains(tile))
                    {
                        ConditionMet();
                        return;
                    }
                }
            }
        }

        public void OnUnitReachedTile(Vector2Int position)
        {
            if (!AreRequirementsMet())
            {
                return;
            }

            // Validate that this position is actually a target tile before adding
            // This prevents unlimited accumulation of non-target positions
            if (!TargetTiles.Contains(position))
            {
                return;
            }

            if (_reachedTilesSet.Add(position))
            {
                TurnrootLogger.Log(
                    $"ReachTilesBattleCondition: Tile {position} reached ({_reachedTilesSet.Count}/{TargetTiles.Count})"
                );

                CheckCondition();
            }
        }

        /// <summary>
        /// Resets the reached tiles tracking.
        /// </summary>
        public void ResetReachedTiles() => _reachedTilesSet.Clear();
    }
}
