using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Maps;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    public partial class BattleContext : MonoBehaviour
    {
        public void InvalidateUnitPositionCache() => currentUnitPositions.Clear();

        /// <summary>
        /// Get or compute valid tiles for a unit. Results are cached and automatically
        /// invalidated when the unit moves or map changes.
        /// </summary>
        public bool TryGetValidTilesForUnit(
            CharacterInstance unit,
            out Dictionary<MapGridPoint, float> moveTiles,
            out Dictionary<MapGridPoint, float> attackTiles,
            bool forceRecompute = false
        )
        {
            moveTiles = new Dictionary<MapGridPoint, float>();
            attackTiles = new Dictionary<MapGridPoint, float>();

            if (unit == null || MapGrid == null)
            {
                return false;
            }

            // Check if we have valid cached data
            if (!forceRecompute && _unitTilesCache.TryGetValue(unit.Id, out var cached))
            {
                bool cacheValid =
                    cached.MapStateVersion == MapGrid.StateVersion
                    && cached.UnitPosition == unit.MapGridPosition;

                if (cacheValid)
                {
                    moveTiles = cached.MoveTiles;
                    attackTiles = cached.AttackTiles;
                    return true;
                }
            }

            // Compute tiles using AIHelper
            var startPoint = unit.UnitPositionToMapGridPoint(unit.MapGridPosition, MapGrid);
            if (startPoint == null)
            {
                return false;
            }

            var move = new Dictionary<MapGridPoint, float>();
            var attack = new Dictionary<MapGridPoint, float>();

            // Try to use cached pathfinding parameters when available to reduce allocations
            var parametersWithRange = GetCachedPathfindingParameters(unit, includeRange: true);
            bool success;
            if (parametersWithRange != null)
            {
                // Use the AIHelper variant that accepts precomputed parameters if available
                success = AIHelper.GetTilesForAINonAlloc(startPoint, move, attack);
            }
            else
            {
                success = AIHelper.GetTilesForAINonAlloc(startPoint, move, attack);
            }

            if (success)
            {
                // Cache copies to avoid external mutation
                _unitTilesCache[unit.Id] = new CachedTileData(
                    new Dictionary<MapGridPoint, float>(move),
                    new Dictionary<MapGridPoint, float>(attack),
                    MapGrid.StateVersion,
                    unit.MapGridPosition
                );

                moveTiles = _unitTilesCache[unit.Id].MoveTiles;
                attackTiles = _unitTilesCache[unit.Id].AttackTiles;
            }

            return success;
        }

        /// <summary>
        /// Precompute and cache PathfindingParameters for the provided unit (with and without attack range)
        /// so that subsequent pathfinding queries can avoid re-creating parameter objects.
        /// </summary>
        public bool PrecomputePathfindingParameters(CharacterInstance unit)
        {
            if (unit == null || MapGrid == null)
            {
                return false;
            }

            var startPoint = unit.UnitPositionToMapGridPoint(unit.MapGridPosition, MapGrid);
            if (startPoint == null)
            {
                return false;
            }

            var p = PathfindingParameters.FromCharacter(unit, MapGrid, startPoint);
            if (p != null && p.IsValid())
            {
                _cachedPathfindingParameters[unit.Id] = p;
            }

            // Avoid recomputing base movement parameters a second time (which may re-read stats/log).
            // If we successfully built the base parameters, create the ranged variant by cloning.
            PathfindingParameters pr = null;
            if (p != null && p.IsValid())
            {
                pr = p.Clone();
                pr.IncludeRange = true;
                pr.MaxRange = unit.GetMaxRange();
            }
            else
            {
                // Fallback: compute ranged parameters directly
                pr = PathfindingParameters.FromCharacterWithRange(unit, MapGrid, startPoint);
            }

            if (pr != null && pr.IsValid())
            {
                _cachedPathfindingParametersWithRange[unit.Id] = pr;
            }

            return _cachedPathfindingParameters.ContainsKey(unit.Id)
                || _cachedPathfindingParametersWithRange.ContainsKey(unit.Id);
        }

        public PathfindingParameters GetCachedPathfindingParameters(
            CharacterInstance unit,
            bool includeRange = false
        )
        {
            if (unit == null)
            {
                return null;
            }

            if (includeRange)
            {
                if (_cachedPathfindingParametersWithRange.TryGetValue(unit.Id, out var pr))
                {
                    return pr.Clone();
                }
            }
            else
            {
                if (_cachedPathfindingParameters.TryGetValue(unit.Id, out var p))
                {
                    return p.Clone();
                }
            }

            return null;
        }

        public void InvalidatePathfindingParameters(string unitId)
        {
            _cachedPathfindingParameters.Remove(unitId);
            _cachedPathfindingParametersWithRange.Remove(unitId);
        }

        public void InvalidateAllPathfindingParameters()
        {
            _cachedPathfindingParameters.Clear();
            _cachedPathfindingParametersWithRange.Clear();
        }

        public void InvalidateUnitTileCache(CharacterInstance unit)
        {
            if (unit == null)
            {
                return;
            }

            // Remove cached tiles for this unit (if present). Avoid recursion.
            if (_unitTilesCache.Remove(unit.Id))
            {
                TurnrootLogger.Log(
                    $"BattleContext: Invalidated tile cache for {unit.CharacterTemplate?.DisplayName}"
                );
            }
        }

        public void InvalidateAllTileCaches() => _unitTilesCache.Clear();
    }
}
