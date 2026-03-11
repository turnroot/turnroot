using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Maps;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    public partial class BattleContextAIHelper
    {
        #region Utility Helpers
        public MapGridPoint FindClosestFromListOfPoints(
            List<MapGridPoint> points,
            MapGridPoint start
        )
        {
            var currentDistance = float.MaxValue;
            var closestPoint = new MapGridPoint();

            foreach (var point in points)
            {
                var distance = Vector2.Distance(start.Coordinates(), point.Coordinates());
                if (distance < currentDistance)
                {
                    currentDistance = distance;
                    closestPoint = point;
                }
            }

            return closestPoint;
        }

        public (
            Vector2 closest,
            Vector2 furthest,
            float closestDist,
            float furthestDist
        ) FindClosestAndFurthestEnemies(List<CharacterInstance> enemies)
        {
            float furthestDistance = 0;
            float closestDistance = float.MaxValue;
            Vector2 closestEnemyPos = Vector2.zero;
            Vector2 furthestEnemyPos = Vector2.zero;

            foreach (var target in enemies)
            {
                var distance = Vector2.Distance(
                    _context.Unit.UnitInstance.MapGridPosition,
                    target.MapGridPosition
                );

                if (distance > furthestDistance)
                {
                    furthestDistance = distance;
                    furthestEnemyPos = target.MapGridPosition;
                }

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemyPos = target.MapGridPosition;
                }
            }

            return (closestEnemyPos, furthestEnemyPos, closestDistance, furthestDistance);
        }

        public void FilterSafeTilesNonAlloc(
            Dictionary<MapGridPoint, float> moveTiles,
            Vector2 closestEnemyPos,
            Vector2 furthestEnemyPos,
            float closestDistance,
            float furthestDistance,
            List<MapGridPoint> safeTiles
        )
        {
            safeTiles.Clear();

            foreach (var tile in moveTiles)
            {
                var tileCoords = tile.Key.Coordinates();
                var distanceToClosest = Vector2.Distance(tileCoords, closestEnemyPos);
                var distanceToFurthest = Vector2.Distance(tileCoords, furthestEnemyPos);

                if (distanceToClosest > closestDistance && distanceToFurthest >= furthestDistance)
                {
                    safeTiles.Add(tile.Key);
                }
            }
        }
        #endregion
    }
}
