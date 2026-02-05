using System.Collections.Generic;
using UnityEngine;

namespace Turnroot.Gameplay.Maps
{
    public partial class MapGridPoint
    {
        /* ---------------------------- Neighbor/Direction Methods ---------------------------- */

        public Vector2 Coordinates() => new(_row, _col);

        public Vector2Int CoordinatesInt => new(_row, _col);

        /// <summary>
        /// Get neighboring grid points. Allocates a new dictionary each call.
        /// For performance-critical code (pathfinding), use GetNeighborsNonAlloc instead.
        /// </summary>
        public Dictionary<string, MapGridPoint> GetNeighbors(bool cardinal = false)
        {
            var neighbors = new Dictionary<string, MapGridPoint>();
            var grid = ParentGrid;
            if (grid == null)
            {
                return neighbors;
            }

            var dirs = cardinal ? CardinalDirections : Directions;
            foreach (var (name, dRow, dCol) in dirs)
            {
                var neighbor = grid.GetGridPoint(_row + dRow, _col + dCol);
                if (neighbor != null)
                {
                    neighbors[name] = neighbor;
                }
            }

            return neighbors;
        }

        /// <summary>
        /// Get neighboring grid points without allocation. Fills the provided dictionary.
        /// The dictionary is cleared before filling. Returns the count of neighbors found.
        /// Use this in performance-critical paths like pathfinding.
        /// </summary>
        /// <param name="neighbors">Dictionary to fill with neighbors. Will be cleared first.</param>
        /// <returns>Number of neighbors found.</returns>
        public int GetNeighborsNonAlloc(
            Dictionary<string, MapGridPoint> neighbors,
            bool cardinal = true
        )
        {
            neighbors.Clear();
            var grid = ParentGrid;
            if (grid == null)
            {
                return 0;
            }

            var dirs = cardinal ? CardinalDirections : Directions;
            int count = 0;
            foreach (var (name, dRow, dCol) in dirs)
            {
                var neighbor = grid.GetGridPoint(_row + dRow, _col + dCol);
                if (neighbor != null)
                {
                    neighbors[name] = neighbor;
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Get the movement cost for this terrain type based on movement mode.
        /// Uses cached terrain type lookup for performance.
        /// </summary>
        public float GetTerrainTypeCost(
            bool isWalking = true,
            bool isFlying = false,
            bool isRiding = false,
            bool isMagic = false,
            bool isArmored = false
        )
        {
            var terrainType = GetCachedTerrainType();
            return terrainType == null ? 1f
                : isWalking ? terrainType.CostWalk
                : isFlying ? terrainType.CostFly
                : isRiding ? terrainType.CostRide
                : isMagic ? terrainType.CostMagic
                : isArmored ? terrainType.CostArmor
                : 1f;
        }
    }
}
