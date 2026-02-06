using System.Collections.Generic;
using UnityEngine;

namespace Turnroot.Gameplay.Maps
{
    public partial class MapGridPoint
    {
        /* ---------------------------- Neighbor/Direction Methods ---------------------------- */

        public Vector2 Coordinates() => new(Row, Col);

        public Vector2Int CoordinatesInt => new(Row, Col);

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
                var neighbor = grid.GetGridPoint(Row + dRow, Col + dCol);
                if (neighbor != null)
                {
                    neighbors[name] = neighbor;
                    count++;
                }
            }

            return count;
        }

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
