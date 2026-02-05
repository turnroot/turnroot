using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components.Battle;
using Turnroot.Gameplay.Maps;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace Turnroot.Gameplay.Brain
{
    public partial class UnitAppearanceBrain
    {
        private const float DECELERATION_RANGE = 1.5f;
        private const float MIN_SPEED_MULTIPLIER = 0.4f;

        private void HandleCharacterMoveStarted(
            CharacterInstance character,
            MapGridPoint destination
        )
        {
            if (character == null || destination == null)
            {
                TurnrootLogger.Log(
                    "HandleCharacterMoveStarted: Invalid parameters",
                    TurnrootLogger.LogLevel.Warning
                );
                return;
            }

            var path = BuildPathToDestination(character, destination);
            if (path == null || path.Count < 2)
            {
                TurnrootLogger.Log(
                    $"Invalid path for {character.CharacterTemplate?.DisplayName}",
                    TurnrootLogger.LogLevel.Warning
                );
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
            var mapGrid = Brain.battleBrain?.BattleObject?.MapGrid;
            if (mapGrid == null)
                return null;

            var startPos = character.MapGridPosition;
            var endPos = destination.CoordinatesInt;

            // Build Manhattan-style path (horizontal first, then vertical)
            var path = new List<Vector3>();
            var current = startPos;
            path.Add(mapGrid.GetTerrainAdjustedWorldPosition(current));

            // Move along X axis
            while (current.x != endPos.x)
            {
                current.x += current.x < endPos.x ? 1 : -1;
                path.Add(mapGrid.GetTerrainAdjustedWorldPosition(current));
            }

            // Move along Y axis
            while (current.y != endPos.y)
            {
                current.y += current.y < endPos.y ? 1 : -1;
                path.Add(mapGrid.GetTerrainAdjustedWorldPosition(current));
            }

            return path.Count > 1 ? path : null;
        }

        private IEnumerator AnimateCharacterMovementCoroutine(
            CharacterInstance character,
            List<Vector3> path
        )
        {
            if (!_unitModels.TryGetValue(character.Id, out var model) || model == null)
            {
                TurnrootLogger.Log($"No model for {character.Id}", TurnrootLogger.LogLevel.Warning);
                Brain.PublishMoveAnimationCompleted(character);
                yield break;
            }

            var animator = model.GetComponent<Animator>();
            var tileHighlighter = Brain.battleBrain?.BattleObject?.GetComponent<TileHighlighter>();

            // Clear highlights and blend to walk
            tileHighlighter?.ClearAll();
            BlendToWalkAnimation(animator);
            yield return new WaitForSeconds(ANIMATION_BLEND_DURATION);

            // Create and animate along spline
            var spline = CreateSplineFromMapGridCoordinates(path);
            var splineLength = GetSplineLength(spline);
            var duration = splineLength / character.WalkingSpeed;
            var elapsed = 0f;
            var lastTilePos = character.MapGridPosition;

            while (elapsed <= duration)
            {
                var t = Mathf.Clamp01(elapsed / duration);
                var distanceTraveled = t * splineLength;
                var speed = ApplyDecelerationFactor(distanceTraveled, splineLength);

                spline.Evaluate(t, out var position, out var tangent, out var up);
                model.transform.position = position;

                // Smooth rotation toward movement direction
                if (math.lengthsq(tangent) > 0.001f)
                {
                    var targetRotation = Quaternion.LookRotation(tangent);
                    model.transform.rotation = Quaternion.Slerp(
                        model.transform.rotation,
                        targetRotation,
                        Time.deltaTime * 10f
                    );
                }

                // Fire tile visited events
                var currentTile = WorldPositionToGridPosition(position);
                if (currentTile != lastTilePos)
                {
                    Brain.PublishCharacterVisitedTile(character, currentTile);
                    lastTilePos = currentTile;
                }

                elapsed += Time.deltaTime * speed;
                yield return null;
            }

            // Ensure final position and rotation
            model.transform.position = path[path.Count - 1];
            character.MapGridPosition = WorldPositionToGridPosition(path[path.Count - 1]);

            // Blend back to idle
            BlendToIdleAnimation(animator);
            yield return new WaitForSeconds(ANIMATION_BLEND_DURATION);

            Brain.PublishMoveAnimationCompleted(character);
        }

        private float ApplyDecelerationFactor(float currentDistance, float totalDistance)
        {
            var remainingDistance = totalDistance - currentDistance;
            if (remainingDistance < DECELERATION_RANGE)
            {
                var factor = remainingDistance / DECELERATION_RANGE;
                return Mathf.Lerp(MIN_SPEED_MULTIPLIER, 1f, factor);
            }
            return 1f;
        }

        private Vector2Int WorldPositionToGridPosition(Vector3 worldPos)
        {
            var mapGrid = Brain.battleBrain?.BattleObject?.MapGrid;
            if (mapGrid == null)
                return Vector2Int.zero;

            // Find closest grid point by checking nearby positions
            var gridSize = 1f; // Default grid size
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
                        // Check if the path is mostly straight (angle close to 180 degrees)
                        var toNextNorm = math.normalize(toNext);
                        var toPrevNorm = math.normalize(toPrev);
                        var dotProduct = math.dot(toNextNorm, -toPrevNorm);

                        // If dot product is close to 1, it's a straight line - use minimal tangents
                        var straightnessFactor = math.clamp(1f - dotProduct, 0.1f, 1f);

                        var tangentMagnitude =
                            settings.UnitMovementCurveSmoothing
                            * math.min(nextDist, prevDist)
                            * 0.3f
                            * straightnessFactor;
                        var tangentDir = math.normalize(toNextNorm - toPrevNorm);

                        knot.TangentIn = -tangentDir * tangentMagnitude;
                        knot.TangentOut = tangentDir * tangentMagnitude;
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

        public OperationResult ApplyMovementSplineToUnit(
            CharacterInstance character,
            Spline movementSpline
        )
        {
            if (character == null || movementSpline == null || movementSpline.Count < 2)
            {
                return OperationResult.Failure("Invalid parameters for ApplyMovementSplineToUnit");
            }

            character.CurrentMovementSpline = movementSpline;
            return OperationResult.Successful();
        }

        public OperationResult AnimateCharacterAlongSpline(CharacterInstance character)
        {
            if (
                character.CurrentMovementSpline == null
                || character.CurrentMovementSpline.Count < 2
            )
            {
                return OperationResult.Failure("CharacterInstance has no valid movement spline");
            }

            // TODO: Implement animation along spline
            return OperationResult.Failure("AnimateCharacterAlongSpline not implemented");
        }

        public void MoveCharacterAlongSpline(
            CharacterInstance character,
            List<Vector3> mapGridPointCoordinates
        )
        {
            var spline = CreateSplineFromMapGridCoordinates(mapGridPointCoordinates);
            var applyResult = ApplyMovementSplineToUnit(character, spline);

            if (!applyResult.Success)
            {
                TurnrootLogger.Log(
                    $"UnitAppearanceBrain: Failed to apply movement spline to character {character.Id}: {applyResult.ErrorMessage}"
                );
                return;
            }

            var animateResult = AnimateCharacterAlongSpline(character);

            if (!animateResult.Success)
            {
                TurnrootLogger.Log(
                    $"UnitAppearanceBrain: Failed to animate character {character.Id} along spline: {animateResult.ErrorMessage}"
                );
            }
        }
    }
}
