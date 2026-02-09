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
            var startPos = character.MapGridPosition;
            var startPoint = mapGrid.GetGridPoint(startPos.x, startPos.y);

            if (
                !battleObject.Context.TryGetValidTilesForUnit(
                    character,
                    out var validMoveTiles,
                    out _
                )
            )
            {
                LogWarning(
                    $"BuildPathToDestination: Failed to get valid tiles for {character.CharacterTemplate.DisplayName}"
                );
                return null;
            }

            var pathPoints = new AStarModified().GetPathThroughReachable(
                startPoint,
                destination,
                validMoveTiles
            );
            if (pathPoints == null || pathPoints.Count < 2)
                return null;

            return pathPoints
                .Select(gridPoint =>
                    mapGrid.GetTerrainAdjustedWorldPosition(gridPoint.CoordinatesInt)
                )
                .ToList();
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

            var (modelToMove, animator) = GetModelAndAnimator(character, unitModel);
            Brain.battleBrain.BattleObject.TileHighlighter.ClearAll();

            BlendToWalkAnimation(animator);
            yield return new WaitForSeconds(ANIMATION_BLEND_DURATION);

            yield return AnimateAlongSpline(character, path, modelToMove);

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
                var pos = ApplyRandomOffset(
                    mapGridPointCoordinates[i],
                    i,
                    mapGridPointCoordinates.Count,
                    settings
                );
                var knot = new BezierKnot(pos, float3.zero, float3.zero, quaternion.identity);

                if (i > 0 && i < mapGridPointCoordinates.Count - 1)
                {
                    ApplyTangentsToKnot(ref knot, mapGridPointCoordinates, i, settings);
                }

                spline.Add(knot);
            }

            return spline;
        }

        public float GetSplineLength(Spline spline)
        {
            return spline
                .Knots.ToArray()
                .Take(spline.Count - 1)
                .Select((knot, i) => math.distance(knot.Position, spline[i + 1].Position))
                .Sum();
        }

        private float CalculateSplineArcLength(Spline spline, int samples)
        {
            if (spline == null || spline.Count < 2)
                return 0f;

            float length = 0f;
            float3 previousPos = float3.zero;

            for (int i = 0; i <= samples; i++)
            {
                spline.Evaluate(i / (float)samples, out var pos, out _, out _);
                if (i > 0)
                    length += math.distance(previousPos, pos);
                previousPos = pos;
            }

            return length;
        }

        // ===== Helper Methods =====

        private (GameObject modelToMove, Animator animator) GetModelAndAnimator(
            CharacterInstance character,
            GameObject unitModel
        )
        {
            if (character.IsMounted && character.CurrentMountModel != null)
            {
                var mount = character.CurrentMountModel;
                return (mount, mount.GetComponent<Animator>());
            }
            return (unitModel, unitModel.GetComponent<Animator>());
        }

        private IEnumerator AnimateAlongSpline(
            CharacterInstance character,
            List<Vector3> path,
            GameObject modelToMove
        )
        {
            var spline = CreateSplineFromMapGridCoordinates(path);
            var splineLength = CalculateSplineArcLength(spline, 100);
            var baseSpeed = character.WalkingSpeed;
            var distanceTraveled = 0f;
            var lastTilePos = character.MapGridPosition;

            while (distanceTraveled < splineLength)
            {
                var speedMultiplier = ApplyDecelerationFactor(distanceTraveled, splineLength);
                var t = Mathf.Clamp01(distanceTraveled / splineLength);

                spline.Evaluate(t, out var position, out var tangent, out _);
                modelToMove.transform.position = position;

                if (math.lengthsq(tangent) > 0.001f)
                {
                    modelToMove.transform.rotation = Quaternion.Slerp(
                        modelToMove.transform.rotation,
                        Quaternion.LookRotation(tangent),
                        Time.deltaTime * 10f
                    );
                }

                var currentTile = WorldPositionToGridPosition(position);
                if (currentTile != lastTilePos)
                {
                    Brain.PublishCharacterVisitedTile(character, currentTile);
                    lastTilePos = currentTile;
                }

                distanceTraveled += baseSpeed * speedMultiplier * Time.deltaTime;
                yield return null;
            }

            modelToMove.transform.position = path[^1];
            character.MapGridPosition = WorldPositionToGridPosition(path[^1]);

            var finalTile = WorldPositionToGridPosition(path[^1]);
            if (finalTile != lastTilePos)
            {
                Brain.PublishCharacterVisitedTile(character, finalTile);
            }
        }

        private float3 ApplyRandomOffset(
            Vector3 point,
            int index,
            int totalCount,
            GameplayGeneralSettings settings
        )
        {
            var pos = new float3(point.x, point.y, point.z);

            if (index > 0 && index < totalCount - 1)
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

            return pos;
        }

        private void ApplyTangentsToKnot(
            ref BezierKnot knot,
            List<Vector3> points,
            int index,
            GameplayGeneralSettings settings
        )
        {
            var pos = knot.Position;
            var toNext = new float3(points[index + 1]) - pos;
            var toPrev = pos - new float3(points[index - 1]);
            var nextDist = math.length(toNext);
            var prevDist = math.length(toPrev);

            if (nextDist > 0f && prevDist > 0f)
            {
                var dotProduct = math.dot(math.normalize(toNext), -math.normalize(toPrev));
                if (dotProduct < 0.9f) // Only apply tangents at corners
                {
                    var tangentDir = math.normalize(
                        math.normalize(toNext) + math.normalize(toPrev)
                    );
                    var tangentMagnitude =
                        settings.UnitMovementCurveSmoothing * math.min(nextDist, prevDist) * 0.25f;
                    knot.TangentIn = -tangentDir * tangentMagnitude;
                    knot.TangentOut = tangentDir * tangentMagnitude;
                }
            }
        }
    }
}
