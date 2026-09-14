using System;
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Maps
{
    public partial class MapGrid : MonoBehaviour
    {
        #region Movement Cost Caching
        private readonly Dictionary<
            string,
            (int MapStateVersion, Dictionary<MapGridPoint, float> Costs)
        > _movementCostCaches = new();

        public static (
            bool Walking,
            bool Flying,
            bool Riding,
            bool Magic,
            bool Armored
        ) NormalizeMovementMode(
            bool isWalking,
            bool isFlying,
            bool isRiding,
            bool isMagic,
            bool isArmored
        )
        {
            // The editor and callers are supposed to be single-mode, but stale state can still cause
            // multiple bools to be true. Normalize to one active mode so the cost cache and traversal
            // logic remain consistent across all movement types.
            if (isFlying)
            {
                return (false, true, false, false, false);
            }

            if (isRiding)
            {
                return (false, false, true, false, false);
            }

            if (isArmored)
            {
                return (false, false, false, false, true);
            }

            if (isMagic)
            {
                return (false, false, false, true, false);
            }

            return (true, false, false, false, false);
        }

        public static string MakeMovementModeKey(
            bool isWalking,
            bool isFlying,
            bool isRiding,
            bool isMagic,
            bool isArmored
        )
        {
            var (Walking, Flying, Riding, Magic, Armored) = NormalizeMovementMode(
                isWalking,
                isFlying,
                isRiding,
                isMagic,
                isArmored
            );

            return (Walking ? "W" : "w")
                + (Flying ? "F" : "f")
                + (Riding ? "R" : "r")
                + (Magic ? "M" : "m")
                + (Armored ? "A" : "a");
        }

        public bool TryGetMovementCostCache(string key, out Dictionary<MapGridPoint, float> cache)
        {
            cache = null;
            if (
                _movementCostCaches.TryGetValue(key, out var entry)
                && entry.MapStateVersion == StateVersion
            )
            {
                cache = entry.Costs;
                return true;
            }
            return false;
        }

        public OperationResult BuildMovementCostCache(
            string key,
            bool isWalking,
            bool isFlying,
            bool isRiding,
            bool isMagic,
            bool isArmored
        )
        {
            try
            {
                EnsureCachedGridPoints();
                var costs = new Dictionary<MapGridPoint, float>(_cachedGridPoints.Count);

                foreach (var mgp in _cachedGridPoints.Values)
                {
                    if (mgp != null)
                    {
                        costs[mgp] = mgp.GetTerrainTypeCost(
                            isWalking,
                            isFlying,
                            isRiding,
                            isMagic,
                            isArmored
                        );
                    }
                }

                _movementCostCaches[key] = (StateVersion, costs);
                return OperationResult.Successful();
            }
            catch (Exception ex)
            {
                return OperationResult.Failure($"BuildMovementCostCache failed: {ex.Message}");
            }
        }
        #endregion
        #region Occupancy Management
        public OperationResult SetOccupied(MapGridPoint point, CharacterInstance occupier)
        {
            EnsureCachedGridPoints();
            var key = new Vector2Int(point.Row, point.Col);

            if (_cachedGridPoints?.TryGetValue(key, out var mgp) == true)
            {
                // If we are overwriting an existing occupant log it to help diagnose conflicting writes.
                if (mgp.CurrentInstance != null && mgp.CurrentInstance != occupier)
                {
                    $"MapGrid: Overwriting occupant at ({mgp.Row}, {mgp.Col}) - {mgp.CurrentInstance.Id} -> {occupier?.Id}".LogWarning();
                }

                mgp.CurrentInstance = occupier;

                // Ensure the occupier's logical position matches the grid point. Some callers relied only
                // on SetOccupied and forgot to set the instance MapGridPosition, causing inconsistencies
                // where the grid reported a unit present but the instance had a default/incorrect position.
                try
                {
                    var newPos = new Vector2Int(mgp.Row, mgp.Col);
                    if (occupier != null && occupier.MapGridPosition != newPos)
                    {
                        occupier.MapGridPosition = newPos;
                    }
                }
                catch (Exception ex)
                {
                    $"MapGrid: Failed to align MapGridPosition for {occupier?.Id ?? "<null>"}: {ex.Message}".LogWarning();
                }

                IncrementStateVersion();
                return OperationResult.Successful();
            }

            return OperationResult.Failure(
                $"Set occupied for point ({point.Row}, {point.Col}) failed"
            );
        }

        public OperationResult RemoveOccupied(MapGridPoint point)
        {
            EnsureCachedGridPoints();
            var key = new Vector2Int(point.Row, point.Col);

            if (_cachedGridPoints?.TryGetValue(key, out var mgp) == true)
            {
                var prev = mgp.CurrentInstance;
                mgp.CurrentInstance = null;

                $"MapGrid: Removed occupant at ({mgp.Row}, {mgp.Col}) - prev={prev?.Id ?? "<none>"}".LogInfo();
                IncrementStateVersion();
                return OperationResult.Successful();
            }

            return OperationResult.Failure(
                $"Remove occupied for point ({point.Row}, {point.Col}) failed"
            );
        }

        public void GetAllOccupiedPoints() => EnsureCachedGridPoints();

        public void IncrementStateVersion()
        {
            StateVersion++;
            OnStateVersionChanged?.Invoke();
        }
        #endregion
    }
}
