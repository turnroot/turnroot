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
            var battleObject = Brain.battleBrain?.BattleObject;
            var mapGrid = battleObject?.MapGrid;
            var context = battleObject?.Context;

            if (mapGrid == null || context == null)
            {
                return null;
            }

            var startPos = character.MapGridPosition;
            var startPoint = mapGrid.GetGridPoint(startPos.x, startPos.y);

            if (startPoint == null || destination == null)
            {
                return null;
            }

            // Get valid move tiles for this character using the same method as BattleInputControllerBrain
            if (!context.TryGetValidTilesForUnit(character, out var validMoveTiles, out _))
            {
                TurnrootLogger.Log(
                    $"BuildPathToDestination: Failed to get valid tiles for {character.CharacterTemplate?.DisplayName}",
                    TurnrootLogger.LogLevel.Warning
                );
                return null;
            }

            // Use GetPathThroughReachable like BicMethods.cs does
            var astar = new AStarModified();
            var pathPoints = astar.GetPathThroughReachable(startPoint, destination, validMoveTiles);

            if (pathPoints == null || pathPoints.Count < 2)
            {
                return null;
            }

            // Convert MapGridPoints to world positions
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
            if (animator != null)
            {
                BlendToWalkAnimation(animator);
                yield return new WaitForSeconds(ANIMATION_BLEND_DURATION);
            }

            // Create and animate along spline
            var spline = CreateSplineFromMapGridCoordinates(path);
            var splineLength = CalculateSplineArcLength(spline, 100);
            var baseSpeed = character.WalkingSpeed;
            var distanceTraveled = 0f;
            var lastTilePos = character.MapGridPosition;

            TurnrootLogger.Log(
                $"Starting movement: splineLength={splineLength}, baseSpeed={baseSpeed}"
            );

            while (distanceTraveled < splineLength)
            {
                // Calculate current speed with deceleration
                var speedMultiplier = ApplyDecelerationFactor(distanceTraveled, splineLength);
                var currentSpeed = baseSpeed * speedMultiplier;

                // Move along the spline based on distance traveled
                var t = Mathf.Clamp01(distanceTraveled / splineLength);

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

                // Increment distance traveled by actual distance moved this frame
                distanceTraveled += currentSpeed * Time.deltaTime;
                yield return null;
            }

            TurnrootLogger.Log($"Movement complete, blending to idle");

            // Ensure final position
            model.transform.position = path[path.Count - 1];
            character.MapGridPosition = WorldPositionToGridPosition(path[path.Count - 1]);

            // Fire final tile visited event
            var finalTile = WorldPositionToGridPosition(path[path.Count - 1]);
            if (finalTile != lastTilePos)
            {
                Brain.PublishCharacterVisitedTile(character, finalTile);
            }

            // Blend back to idle IMMEDIATELY after reaching destination
            if (animator != null)
            {
                BlendToIdleAnimation(animator);
                yield return new WaitForSeconds(ANIMATION_BLEND_DURATION);
            }

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
            {
                return Vector2Int.zero;
            }

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
                        var toNextNorm = math.normalize(toNext);
                        var toPrevNorm = math.normalize(toPrev);
                        var dotProduct = math.dot(toNextNorm, -toPrevNorm);

                        // Only apply tangents at corners (when direction changes significantly)
                        // dotProduct close to 1 = straight line (180 degrees)
                        // dotProduct close to -1 = sharp turn (0 degrees)
                        var isCorner = dotProduct < 0.9f; // Less than ~25 degree angle = corner

                        if (isCorner)
                        {
                            // Use the average direction for smooth corners
                            var tangentDir = math.normalize(toNextNorm + toPrevNorm);
                            var tangentMagnitude =
                                settings.UnitMovementCurveSmoothing
                                * math.min(nextDist, prevDist)
                                * 0.25f;

                            knot.TangentIn = -tangentDir * tangentMagnitude;
                            knot.TangentOut = tangentDir * tangentMagnitude;
                        }
                        // For straight segments, leave tangents at zero for linear interpolation
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
