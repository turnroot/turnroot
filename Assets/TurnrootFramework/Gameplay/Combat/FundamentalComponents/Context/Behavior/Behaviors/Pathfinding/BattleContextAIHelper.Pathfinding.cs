using System.Collections.Generic;
using Turnroot.Gameplay.Maps;
using Turnroot.Services;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    public partial class BattleContextAIHelper
    {
        #region Pathfinding
        public bool GetPossibleTilesIncludingRangeNonAlloc(
            MapGridPoint start,
            Dictionary<MapGridPoint, float> result,
            bool includeHealRange = false
        )
        {
            result.Clear();

            var validation = ValidationService.Instance.ValidateCharacter(
                _context.Unit.UnitInstance,
                "GetPossibleTilesIncludingRangeNonAlloc"
            );
            if (!validation.IsValid)
            {
                return false;
            }

            var parameters = PathfindingParameters.FromCharacterWithRange(
                _context.Unit.UnitInstance,
                _context.MapGrid,
                start
            );

            if (includeHealRange)
            {
                parameters.IncludeHealRange = true;
            }

            if (parameters == null || !parameters.IsValid())
            {
                return false;
            }

            ApplyMovementBonuses(parameters);

            var points = _aStarModified.GetReachable(
                parameters.Graph,
                parameters.Start,
                parameters.MovementBudget,
                parameters.IsWalking,
                parameters.IsFlying,
                parameters.IsRiding,
                parameters.IsMagic,
                parameters.IsArmored,
                parameters.SameDirectionMultiplier,
                parameters.IncludeRange,
                parameters.MaxRange
            );

            if (points != null)
            {
                foreach (var kvp in points)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }

            return true;
        }

        public bool GetPossibleMoveTilesNonAlloc(
            MapGridPoint start,
            Dictionary<MapGridPoint, float> result
        )
        {
            result.Clear();

            var validation = ValidationService.Instance.ValidateCharacter(
                _context.Unit.UnitInstance,
                "GetPossibleMoveTilesNonAlloc"
            );
            if (!validation.IsValid)
            {
                return false;
            }

            var parameters = PathfindingParameters.FromCharacter(
                _context.Unit.UnitInstance,
                _context.MapGrid,
                start
            );

            if (parameters == null || !parameters.IsValid())
            {
                return false;
            }

            var points = _aStarModified.GetReachable(
                parameters.Graph,
                parameters.Start,
                parameters.MovementBudget,
                parameters.IsWalking,
                parameters.IsFlying,
                parameters.IsRiding,
                parameters.IsMagic,
                parameters.IsArmored
            );

            if (points != null)
            {
                foreach (var kvp in points)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }

            return true;
        }

        public bool GetTilesForAINonAlloc(
            MapGridPoint start,
            Dictionary<MapGridPoint, float> moveTilesResult,
            Dictionary<MapGridPoint, float> attackTilesResult
        )
        {
            ClearReusableTileDictionaries();
            attackTilesResult.Clear();

            var validation = ValidationService.Instance.ValidateCharacter(
                _context.Unit.UnitInstance,
                "GetTilesForAINonAlloc"
            );
            if (!validation.IsValid)
            {
                return false;
            }

            var parametersWithRange = PathfindingParameters.FromCharacterWithRange(
                _context.Unit.UnitInstance,
                _context.MapGrid,
                start
            );

            if (parametersWithRange == null || !parametersWithRange.IsValid())
            {
                return false;
            }

            var parametersMove = parametersWithRange.Clone();
            parametersMove.IncludeRange = false;
            parametersMove.MaxRange = 0;

            var movePoints = _aStarModified.GetReachable(
                parametersMove.Graph,
                parametersMove.Start,
                parametersMove.MovementBudget,
                parametersMove.IsWalking,
                parametersMove.IsFlying,
                parametersMove.IsRiding,
                parametersMove.IsMagic,
                parametersMove.IsArmored
            );

            if (movePoints != null)
            {
                foreach (var kvp in movePoints)
                {
                    moveTilesResult[kvp.Key] = kvp.Value;
                }
            }

            var allPoints = _aStarModified.GetReachable(
                parametersWithRange.Graph,
                parametersWithRange.Start,
                parametersWithRange.MovementBudget,
                parametersWithRange.IsWalking,
                parametersWithRange.IsFlying,
                parametersWithRange.IsRiding,
                parametersWithRange.IsMagic,
                parametersWithRange.IsArmored,
                parametersWithRange.SameDirectionMultiplier,
                parametersWithRange.IncludeRange,
                parametersWithRange.MaxRange
            );

            if (allPoints != null)
            {
                foreach (var tile in allPoints)
                {
                    if (!moveTilesResult.ContainsKey(tile.Key))
                    {
                        // Exclude tiles occupied by allied units from attack tile list
                        var occupant = tile.Key.CurrentInstance;
                        if (occupant != null && _context.IsAlly(occupant))
                        {
                            continue;
                        }
                        attackTilesResult[tile.Key] = tile.Value;
                    }
                }
            }

            return true;
        }

        public bool GetTilesForAIWithHealNonAlloc(
            MapGridPoint start,
            Dictionary<MapGridPoint, float> moveTilesResult,
            Dictionary<MapGridPoint, float> attackTilesResult,
            Dictionary<MapGridPoint, float> healTilesResult
        )
        {
            ClearReusableTileDictionaries();
            healTilesResult.Clear();

            if (!GetTilesForAINonAlloc(start, moveTilesResult, attackTilesResult))
            {
                return false;
            }

            using var allTilesPooled = PooledDictionary<MapGridPoint, float>.Get();
            var allTiles = allTilesPooled.Dictionary;

            if (!GetPossibleTilesIncludingRangeNonAlloc(start, allTiles, includeHealRange: true))
            {
                return false;
            }

            foreach (var tile in allTiles)
            {
                if (
                    !moveTilesResult.ContainsKey(tile.Key)
                    && !attackTilesResult.ContainsKey(tile.Key)
                )
                {
                    healTilesResult[tile.Key] = tile.Value;
                }
            }

            return true;
        }

        private void ApplyMovementBonuses(PathfindingParameters parameters)
        {
            var classData = _context.Unit.UnitInstance.CurrentClass?.ClassData;
            if (classData.Stats.UnboundedStatBonuses == null)
            {
                return;
            }

            var bonuses = classData.Stats.UnboundedStatBonuses;
            if (bonuses != null)
            {
                var idx = bonuses.FindIndex(b =>
                    b.unboundedStatType == Characters.Stats.UnboundedStatType.Movement
                );
                if (idx >= 0)
                {
                    parameters.MovementBudget += (int)bonuses[idx].value;
                }
            }
        }
        #endregion
    }
}
