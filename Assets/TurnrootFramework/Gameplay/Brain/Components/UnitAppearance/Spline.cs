using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Maps;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Handles spline-based unit movement animation and pathfinding visualization.
    /// </summary>
    public partial class UnitAppearanceBrain
    {
        private void HandleCharacterMoveStarted(
            CharacterInstance character,
            MapGridPoint destination
        )
        {
            var path = BuildPathToDestination(character, destination);
            if (path == null || path.Count < 2)
            {
                LogWarning($"Invalid path for {character.CharacterTemplate.DisplayName}");
                Brain.PublishMoveAnimationCompleted(character);
                return;
            }

            StartCoroutine(AnimateCharacterMovementCoroutine(character, path));
        }

        private List<Vector3> BuildPathToDestination(
            CharacterInstance character,
            MapGridPoint destination
        )
        {
            var battleObject = Brain.battleBrain.BattleObject;
            var mapGrid = battleObject.MapGrid;
            var context = battleObject.Context;

            var startPos = character.MapGridPosition;
            var startPoint = mapGrid.GetGridPoint(startPos.x, startPos.y);

            if (!context.TryGetValidTilesForUnit(character, out var validMoveTiles, out _))
            {
                LogWarning($"BuildPathToDestination: Failed to get valid tiles for {character.CharacterTemplate.DisplayName}");
                return null;
            }

            var astar = new AStarModified();
            var pathPoints = astar.GetPathThroughReachable(startPoint, destination, validMoveTiles);

            if (pathPoints == null || pathPoints.Count < 2)
            {
                return null;
            }

            var path = new List<Vector3>();
            foreach (var gridPoint in pathPoints)
            {
                path.Add(mapGrid.GetTerrainAdjustedWorldPosition(gridPoint.CoordinatesInt));
            }

            return path;
        }

        private IEnumerator AnimateCharacterMovementCoroutine(
            CharacterInstance character,
            List<Vector3> path
        )
        {
            if (!_unitModels.TryGetValue(character.Id, out var unitModel) || unitModel == null)
            {
                LogWarning($"No model for {character.Id}");
                Brain.PublishMoveAnimationCompleted(character);
                yield break;
            }

            // Determine which model to animate and move - mount if mounted, unit otherwise
            GameObject modelToMove;
            Animator animator;

            if (character.IsMounted && character.CurrentMountModel != null)
            {
                modelToMove = character.CurrentMountModel;
                animator = modelToMove.GetComponent<Animator>();
            }
            else
            {
                modelToMove = unitModel;
                animator = modelToMove.GetComponent<Animator>();
            }

            var tileHighlighter = Brain.battleBrain.BattleObject.TileHighlighter;

            tileHighlighter.ClearAll();

            BlendToWalkAnimation(animator);
            yield return new WaitForSeconds(ANIMATION_BLEND_DURATION);

            var spline = CreateSplineFromMapGridCoordinates(path);
            var splineLength = CalculateSplineArcLength(spline, 100);
            var baseSpeed = character.WalkingSpeed;
            var distanceTraveled = 0f;
            var lastTilePos = character.MapGridPosition;

            while (distanceTraveled < splineLength)
            {
                var speedMultiplier = ApplyDecelerationFactor(distanceTraveled, splineLength);
                var currentSpeed = baseSpeed * speedMultiplier;

                var t = Mathf.Clamp01(distanceTraveled / splineLength);

                spline.Evaluate(t, out var position, out var tangent, out var up);
                modelToMove.transform.position = position;

                if (math.lengthsq(tangent) > 0.001f)
                {
                    var targetRotation = Quaternion.LookRotation(tangent);
                    modelToMove.transform.rotation = Quaternion.Slerp(
                        modelToMove.transform.rotation,
                        targetRotation,
                        Time.deltaTime * 10f
                    );
                }

                var currentTile = WorldPositionToGridPosition(position);
                if (currentTile != lastTilePos)
                {
                    Brain.PublishCharacterVisitedTile(character, currentTile);
                    lastTilePos = currentTile;
                }

                distanceTraveled += currentSpeed * Time.deltaTime;
                yield return null;
            }

            modelToMove.transform.position = path[^1];
            // Do NOT set `character.MapGridPosition` here — movement visuals must not mutate authoritative state.
            // The `MapGrid` (via MoveCommand/SetOccupied) is responsible for updating instance positions.

            var finalTile = WorldPositionToGridPosition(path[^1]);
            if (finalTile != lastTilePos)
            {
                Brain.PublishCharacterVisitedTile(character, finalTile);
            }

            if (animator != null)
            {
                BlendToIdleAnimation(animator);
                yield return new WaitForSeconds(ANIMATION_BLEND_DURATION);
            }

            Brain.PublishMoveAnimationCompleted(character);
        }

        private float ApplyDecelerationFactor(float currentDistance, float totalDistance)
        {
            var settings = GameplayGeneralSettings.Instance;
            var remainingDistance = totalDistance - currentDistance;
            if (remainingDistance < settings.UnitMovementDecelerationRange)
            {
                var factor = remainingDistance / settings.UnitMovementDecelerationRange;
                return Mathf.Lerp(settings.UnitMovementMinSpeedMultiplier, 1f, factor);
            }
            return 1f;
        }

        private Vector2Int WorldPositionToGridPosition(Vector3 worldPos)
        {
            var mapGrid = Brain.battleBrain.BattleObject.MapGrid;
            if (mapGrid == null)
            {
                return Vector2Int.zero;
            }

            // Grid size is implicitly 1f based on grid point positioning
            var gridSize = 1f;
            var estimatedGridPos = new Vector2Int(
                Mathf.RoundToInt(worldPos.x / gridSize),
                Mathf.RoundToInt(worldPos.z / gridSize)
            );

            var gridPoint = mapGrid.GetGridPoint(estimatedGridPos.x, estimatedGridPos.y);
            return gridPoint != null ? gridPoint.CoordinatesInt : estimatedGridPos;
        }

        public Spline CreateSplineFromMapGridCoordinates(List<Vector3> mapGridPointCoordinates)
        {
            var spline = new Spline();
            var settings = GameplayGeneralSettings.Instance;

            for (int i = 0; i < mapGridPointCoordinates.Count; i++)
            {
                var pos = new float3(
                    mapGridPointCoordinates[i].x,
                    mapGridPointCoordinates[i].y,
                    mapGridPointCoordinates[i].z
                );

                if (i > 0 && i < mapGridPointCoordinates.Count - 1)
                {
                    pos += new float3(
                        UnityEngine.Random.Range(
                            -settings.UnitMovementCurveRandomness,
                            settings.UnitMovementCurveRandomness
                        ),
                        0f,
                        UnityEngine.Random.Range(
                            -settings.UnitMovementCurveRandomness,
                            settings.UnitMovementCurveRandomness
                        )
                    );
                }

                var knot = new BezierKnot(pos, float3.zero, float3.zero, quaternion.identity);

                if (i > 0 && i < mapGridPointCoordinates.Count - 1)
                {
                    var toNext = new float3(mapGridPointCoordinates[i + 1]) - pos;
                    var toPrev = pos - new float3(mapGridPointCoordinates[i - 1]);
                    var nextDist = math.length(toNext);
                    var prevDist = math.length(toPrev);

                    if (nextDist > 0f && prevDist > 0f)
                    {
                        var toNextNorm = math.normalize(toNext);
                        var toPrevNorm = math.normalize(toPrev);
                        var dotProduct = math.dot(toNextNorm, -toPrevNorm);

                        // Only apply tangents at corners
                        var isCorner = dotProduct < 0.9f;

                        if (isCorner)
                        {
                            var tangentDir = math.normalize(toNextNorm + toPrevNorm);
                            var tangentMagnitude =
                                settings.UnitMovementCurveSmoothing
                                * math.min(nextDist, prevDist)
                                * 0.25f;

                            knot.TangentIn = -tangentDir * tangentMagnitude;
                            knot.TangentOut = tangentDir * tangentMagnitude;
                        }
                    }
                }

                spline.Add(knot);
            }

            return spline;
        }

        public float GetSplineLength(Spline spline)
        {
            float length = 0f;
            var knots = spline.Knots.ToArray();

            for (int i = 0; i < knots.Length - 1; i++)
            {
                length += math.distance(knots[i].Position, knots[i + 1].Position);
            }

            return length;
        }

        private float CalculateSplineArcLength(Spline spline, int samples)
        {
            if (spline == null || spline.Count < 2)
            {
                return 0f;
            }

            float length = 0f;
            float3 previousPos = float3.zero;

            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;
                spline.Evaluate(t, out var pos, out _, out _);

                if (i > 0)
                {
                    length += math.distance(previousPos, pos);
                }

                previousPos = pos;
            }

            return length;
        }
    }
}
