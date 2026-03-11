using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Maps;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    /// <summary>
    /// Static utility methods for pathfinding calculations and cost computations.
    /// </summary>
    public static class PathfinderHelpers
    {
        public static bool TryComputePathMovementCost(
            MapGrid mapGrid,
            PathfindingParameters parameters,
            MapGridPoint destination,
            out float totalCost
        )
        {
            totalCost = 0f;
            if (mapGrid == null || parameters == null)
            {
                return false;
            }

            var astar = new AStarModified();
            return astar.TryComputePathCost(
                mapGrid,
                parameters.Start,
                destination,
                out totalCost,
                parameters.IsWalking,
                parameters.IsFlying,
                parameters.IsRiding,
                parameters.IsMagic,
                parameters.IsArmored,
                parameters.SameDirectionMultiplier
            );
        }

        public static bool TryFindClosestPointPathCost(
            MapGrid mapGrid,
            PathfindingParameters parameters,
            IEnumerable<MapGridPoint> points,
            out float closestCost
        )
        {
            closestCost = float.MaxValue;
            if (points == null)
            {
                return false;
            }

            bool foundAny = false;
            foreach (var p in points)
            {
                if (TryComputePathMovementCost(mapGrid, parameters, p, out float c))
                {
                    foundAny = true;
                    if (c < closestCost)
                    {
                        closestCost = c;
                    }
                }
            }

            return foundAny;
        }

        public static bool TryFindClosestAllyPathCost(
            MapGrid mapGrid,
            CharacterInstance subjectUnit,
            MapGridPoint start,
            IEnumerable<CharacterInstance> allies,
            out float closestCost
        )
        {
            closestCost = float.MaxValue;
            if (mapGrid == null || subjectUnit == null)
            {
                return false;
            }

            var parameters = PathfindingParameters.FromCharacter(subjectUnit, mapGrid, start);
            if (parameters == null)
            {
                return false;
            }

            bool foundAny = false;
            foreach (var ally in allies)
            {
                if (ally == null || ally == subjectUnit)
                {
                    continue;
                }

                var dest = ally.UnitPositionToMapGridPoint(ally.MapGridPosition, mapGrid);
                if (TryComputePathMovementCost(mapGrid, parameters, dest, out float c))
                {
                    foundAny = true;
                    if (c < closestCost)
                    {
                        closestCost = c;
                    }
                }
            }

            return foundAny;
        }

        /// <summary>
        /// Find the shortest straight-line distance from an origin point to a list of units.
        /// Returns float.MaxValue if no valid units were provided.
        /// </summary>
        public static float FindClosestDistanceToUnits(
            Vector2 origin,
            IReadOnlyList<CharacterInstance> units,
            CharacterInstance exclude = null
        )
        {
            float closest = float.MaxValue;
            if (units == null)
            {
                return closest;
            }

            for (int i = 0; i < units.Count; i++)
            {
                var u = units[i];
                if (u == null || u == exclude)
                {
                    continue;
                }

                float dist = Vector2.Distance(origin, u.MapGridPosition);
                if (dist < closest)
                {
                    closest = dist;
                }
            }

            return closest;
        }

        /// <summary>
        /// Find the closest unit (by straight-line distance) from an origin point. Returns null if none found.
        /// </summary>
        public static CharacterInstance FindClosestUnit(
            Vector2 origin,
            IReadOnlyList<CharacterInstance> units,
            CharacterInstance exclude = null
        )
        {
            CharacterInstance closest = null;
            float closestDist = float.MaxValue;
            if (units == null)
            {
                return null;
            }

            for (int i = 0; i < units.Count; i++)
            {
                var u = units[i];
                if (u == null || u == exclude)
                {
                    continue;
                }

                float dist = Vector2.Distance(origin, u.MapGridPosition);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = u;
                }
            }

            return closest;
        }
    }
}