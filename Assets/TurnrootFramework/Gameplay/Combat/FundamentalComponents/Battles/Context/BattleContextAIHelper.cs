using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Maps;
using Turnroot.Services;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    /// <summary>
    /// Helper class for AI decision-making and pathfinding in battle contexts.
    /// Handles movement tile calculation, target/ally filtering, and behavioral AI logic.
    /// Uses non-allocating patterns with reusable dictionaries to avoid GC allocations.
    /// </summary>
    public class BattleContextAIHelper
    {
        private readonly BattleContext _context;
        private AStarModified _aStarModified;

        // Reusable dictionaries to avoid allocations during AI decision-making
        private readonly Dictionary<MapGridPoint, float> _reusableMoveTiles = new();
        private readonly Dictionary<MapGridPoint, float> _reusableAttackTiles = new();

        public BattleContextAIHelper(BattleContext context)
        {
            _context = context;
        }

        public void InitializeAIControlledUnit(CharacterInstance unitInstance)
        {
            _context.UnitInstance = unitInstance;
            _aStarModified = new AStarModified();
        }

        /// <summary>
        /// Gets references to the reusable tile dictionaries for callers that need them.
        /// WARNING: These dictionaries are reused between calls. Copy values if you need to persist them.
        /// </summary>
        public (
            Dictionary<MapGridPoint, float> MoveTiles,
            Dictionary<MapGridPoint, float> AttackTiles
        ) GetReusableTileDictionaries()
        {
            return (_reusableMoveTiles, _reusableAttackTiles);
        }

        /// <summary>
        /// Populates the provided dictionary with possible tiles that the unit can move to, including the range of its attacks.
        /// The function uses the A* algorithm to find all reachable tiles within the unit's movement range and attack range.
        /// </summary>
        /// <param name="start">The starting position for pathfinding.</param>
        /// <param name="result">The dictionary to populate with reachable tiles. Will be cleared before use.</param>
        /// <returns>True if the operation succeeded; false if validation failed.</returns>
        public bool GetPossibleTilesIncludingRangeNonAlloc(
            MapGridPoint start,
            Dictionary<MapGridPoint, float> result
        )
        {
            result.Clear();

            var validation = ValidationService.Instance.ValidateCharacter(
                _context.UnitInstance,
                "GetPossibleTilesIncludingRangeNonAlloc"
            );
            if (!validation.IsValid)
            {
                return false;
            }

            var parameters = PathfindingParameters.FromCharacterWithRange(
                _context.UnitInstance,
                _context.mapGrid,
                start
            );

            if (parameters == null || !parameters.IsValid())
            {
                return false;
            }

            // Apply movement bonuses
            var classData = _context.UnitInstance.CurrentClass.ClassData;
            var movementBonusMod = classData.Stats.UnboundedStatBonuses?.Find(b =>
                b.unboundedStatType == Characters.Stats.UnboundedStatType.Movement
            );
            if (movementBonusMod.HasValue)
            {
                parameters.MovementBudget += (int)movementBonusMod.Value.value;
            }

            var points = _aStarModified.GetReachable(
                parameters.Graph,
                parameters.Start,
                parameters.MovementBudget,
                parameters.IsWalking,
                parameters.IsFlying,
                parameters.IsRiding,
                parameters.IsMagic,
                parameters.IsArmored,
                parameters.SameDirectionMultiplier,
                parameters.IncludeRange,
                parameters.MaxRange
            );

            if (points != null)
            {
                foreach (var kvp in points)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }

            return true;
        }

        /// <summary>
        /// Populates the provided dictionary with all tiles the unit can move to (excluding attack-only range).
        /// </summary>
        /// <param name="start">The starting position for pathfinding.</param>
        /// <param name="result">The dictionary to populate with reachable tiles. Will be cleared before use.</param>
        /// <returns>True if the operation succeeded; false if validation failed.</returns>
        public bool GetPossibleMoveTilesNonAlloc(
            MapGridPoint start,
            Dictionary<MapGridPoint, float> result
        )
        {
            result.Clear();

            var validation = ValidationService.Instance.ValidateCharacter(
                _context.UnitInstance,
                "GetPossibleMoveTilesNonAlloc"
            );
            if (!validation.IsValid)
            {
                return false;
            }

            var parameters = PathfindingParameters.FromCharacter(
                _context.UnitInstance,
                _context.mapGrid,
                start
            );

            if (parameters == null || !parameters.IsValid())
            {
                return false;
            }

            var points = _aStarModified.GetReachable(
                parameters.Graph,
                parameters.Start,
                parameters.MovementBudget,
                parameters.IsWalking,
                parameters.IsFlying,
                parameters.IsRiding,
                parameters.IsMagic,
                parameters.IsArmored
            );

            if (points != null)
            {
                foreach (var kvp in points)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }

            return true;
        }

        /// <summary>
        /// Computes both movement and attack-only tiles for AI decision making.
        /// Attack tiles are those reachable by weapon range but not by movement.
        /// </summary>
        /// <param name="start">The starting position for pathfinding.</param>
        /// <param name="moveTilesResult">Dictionary to populate with movement tiles. Will be cleared before use.</param>
        /// <param name="attackTilesResult">Dictionary to populate with attack-only tiles. Will be cleared before use.</param>
        /// <returns>True if both calculations succeeded; false otherwise.</returns>
        public bool GetTilesForAINonAlloc(
            MapGridPoint start,
            Dictionary<MapGridPoint, float> moveTilesResult,
            Dictionary<MapGridPoint, float> attackTilesResult
        )
        {
            attackTilesResult.Clear();

            if (!GetPossibleMoveTilesNonAlloc(start, moveTilesResult))
            {
                return false;
            }

            // We need a temporary dictionary for all tiles including range
            // Use pooled dictionary for the intermediate calculation
            using var allTilesPooled = PooledDictionary<MapGridPoint, float>.Get();
            var allTiles = allTilesPooled.Dictionary;

            if (!GetPossibleTilesIncludingRangeNonAlloc(start, allTiles))
            {
                return false;
            }

            // Attack tiles are those reachable by weapon range but not by movement
            foreach (var tile in allTiles)
            {
                if (!moveTilesResult.TryGetValue(tile.Key, out _))
                {
                    attackTilesResult[tile.Key] = tile.Value;
                }
            }

            return true;
        }

        /// <summary>
        /// Now that we have all the tiles, we freak it sensitive style with the unit's behavior data
        /// and any battle objectives
        /// This is a custom algorithm that I created, so it may be optimized in future by me or others
        /// </summary>
        public void PickTileAndAction()
        {
            /* ----------------------------- Assemble datas ----------------------------- */
            // Use reusable dictionaries to avoid allocations
            GetTilesForAINonAlloc(
                _context.UnitInstance.UnitPositionToMapGridPoint(
                    _context.UnitInstance.MapGridPosition,
                    _context.mapGrid
                ),
                _reusableMoveTiles,
                _reusableAttackTiles
            );

            Dictionary<string, float> behaviorDict =
                _context.UnitInstance.CharacterTemplate.BehaviorSettings.GetBehaviorDictionary();

            // Look at Greedy / Bloodthirsty first, if we are going to go for loot, we don't need to worry
            // about targets or allies
            if (Random.value >= behaviorDict["BloodthirstGreed"])
            {
                HandleCombatBehavior(_reusableMoveTiles, _reusableAttackTiles, behaviorDict);
            }
            else
            {
                HandleGreedyBehavior(_reusableMoveTiles, _reusableAttackTiles);
            }
        }

        /// <summary>
        /// Moves the unit to the closest feature of a specified type (e.g., treasure chest).
        /// </summary>
        /// <param name="featureType"></param>
        /// <returns>
        /// True if the unit moved to a feature, false otherwise.
        /// </returns>
        private bool MoveToClosestFeatureType(MapGridPointFeature.FeatureType featureType)
        {
            var features = _context.mapGrid.GetAllGridPointsByFeatureType(featureType);
            // move towards the closest treasure chest
            if (features.Count > 0)
            {
                var closestFeature = FindClosestFromListOfPoints(
                    features,
                    _context.UnitInstance.UnitPositionToMapGridPoint(
                        _context.UnitInstance.MapGridPosition,
                        _context.mapGrid
                    )
                );
                return _context.MoveUnit(_context.UnitInstance, closestFeature.CoordinatesInt());
            }
            return false;
        }

        /// <summary>
        /// Handles AI behavior when unit prioritizes loot over combat.
        /// Move towards the closest treasure chest or, if none are found, towards the closest ally.
        /// </summary>
        private void HandleGreedyBehavior(
            Dictionary<MapGridPoint, float> moveTiles,
            Dictionary<MapGridPoint, float> attackTiles
        )
        {
            if (!MoveToClosestFeatureType(MapGridPointFeature.FeatureType.Treasure))
            {
                // if no treasure chests, move towards the closest ally
                var tryMove = MoveNextToClosestAlly(
                    moveTiles,
                    _context.UnitInstance.UnitPositionToMapGridPoint(
                        _context.UnitInstance.MapGridPosition,
                        _context.mapGrid
                    )
                );
                if (!tryMove.Success)
                {
                    // If no moveable neighbors are found, try to retreat to a safe position
                    if (!TryToRetreatToSafeTile(moveTiles))
                    {
                        // If there's no treasure chests, no allies, and no enemies in range...
                        // faff around until the next turn I guess
                        // TODO: End turn without moving
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to move the unit to a safe tile that increases distance from enemies.
        /// </summary>
        /// <param name="moveTiles">Dictionary of tiles the unit can move to.</param>
        /// <returns>True if the unit moved to a safe tile, false otherwise.</returns>
        private bool TryToRetreatToSafeTile(Dictionary<MapGridPoint, float> moveTiles)
        {
            if (_context.Targets == null || _context.Targets.Count == 0)
            {
                return false;
            }

            var (closestEnemyPos, furthestEnemyPos, closestDistance, furthestDistance) =
                FindClosestAndFurthestEnemies(_context.Targets);

            using var safeTilesPooled = PooledList<MapGridPoint>.Get();
            var safeTiles = safeTilesPooled.List;

            FilterSafeTilesNonAlloc(
                moveTiles,
                closestEnemyPos,
                furthestEnemyPos,
                closestDistance,
                furthestDistance,
                safeTiles
            );

            if (safeTiles.Count > 0)
            {
                var chosenTile = safeTiles[Random.Range(0, safeTiles.Count)];
                return _context.MoveUnit(_context.UnitInstance, chosenTile.CoordinatesInt());
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Moves the unit next to the closest ally in its movement range.
        /// If no moveable neighbors are found, returns a failure result.
        /// </summary>
        /// <param name="moveTiles">Dictionary of tiles the unit can move to.</param>
        /// <param name="start">The starting position.</param>
        /// <returns>OperationResult indicating success or failure.</returns>
        private OperationResult MoveNextToClosestAlly(
            Dictionary<MapGridPoint, float> moveTiles,
            MapGridPoint start
        )
        {
            using var alliesPooled = PooledList<CharacterInstance>.Get();
            var alliesInMoveRange = alliesPooled.List;
            GetAlliesInMovementRangeNonAlloc(moveTiles, alliesInMoveRange);

            if (alliesInMoveRange.Count == 0)
            {
                return OperationResult.Failure("No allies found in movement range.");
            }

            var closestAlly = GetClosestAllyPosition(alliesInMoveRange);
            // filter movable tiles to get the neighbors of the ally
            Dictionary<string, MapGridPoint> neighbors = closestAlly.GetNeighbors();

            // Use pooled list to reduce GC allocations
            using var moveableNeighborsPooled = PooledList<MapGridPoint>.Get();
            var moveableNeighbors = moveableNeighborsPooled.List;

            foreach (var neighbor in neighbors)
            {
                // Use TryGetValue to avoid double lookup
                if (moveTiles.TryGetValue(neighbor.Value, out _))
                {
                    moveableNeighbors.Add(neighbor.Value);
                }
            }
            // pick a random neighbor to move to
            if (moveableNeighbors.Count > 0)
            {
                var chosenTile = moveableNeighbors[Random.Range(0, moveableNeighbors.Count)];
                bool moved = _context.MoveUnit(_context.UnitInstance, chosenTile.CoordinatesInt());
                return moved
                    ? OperationResult.SuccessResult()
                    : OperationResult.Failure("Failed to move to neighbor.");
            }
            else
            {
                return OperationResult.Failure("No moveable neighbors found for the closest ally.");
            }
        }

        /// <summary>
        /// Finds the closest point from a list of points relative to a starting point.
        /// </summary>
        /// <param name="points">List of points to search.</param>
        /// <param name="start">Starting point for distance comparison.</param>
        /// <returns>The closest point from the list.</returns>
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

        /// <summary>
        /// Handles AI combat decision-making based on behavior settings.
        /// Determines whether to engage or retreat based on BrashWary behavior.
        /// </summary>
        private void HandleCombatBehavior(
            Dictionary<MapGridPoint, float> moveTiles,
            Dictionary<MapGridPoint, float> attackTiles,
            Dictionary<string, float> behaviorDict
        )
        {
            // Use pooled lists for temporary AI calculations
            using var targetsPooled = PooledList<CharacterInstance>.Get();
            using var alliesPooled = PooledList<CharacterInstance>.Get();
            var TargetsInTileData = targetsPooled.List;
            var AlliesInMoveRange = alliesPooled.List;

            // Not greedy- combat proceeds
            // First, we get the available targets
            GetTargetsInAttackRangeNonAlloc(attackTiles, TargetsInTileData);

            // Then we get the allies in movement range (not attack range)
            GetAlliesInMovementRangeNonAlloc(moveTiles, AlliesInMoveRange);

            // TODO: Integrate third party team
            // Now we have the tiles, targets, and allies, we can start making decisions
            // The first check is against BrashWary- the higher BrashWary, the more likely the unit is to run away
            // from danger and move as far from enemy units as possible
            // If BrashWary <= .8, we divide it by 3. Otherwise, we leave it alone.
            var brashWary =
                behaviorDict["BrashWary"] <= 0.8f
                    ? behaviorDict["BrashWary"] / 3
                    : behaviorDict["BrashWary"];
            var RunAway = Random.value < brashWary;

            if (RunAway)
            {
                HandleRetreatBehavior(
                    moveTiles,
                    TargetsInTileData,
                    AlliesInMoveRange,
                    behaviorDict
                );
            }
            else
            {
                // TODO: Implement attack behavior
                // For now, do nothing until attack logic is implemented
            }
        }

        /// <summary>
        /// Fills the provided list with targets in attack range (non-allocating).
        /// </summary>
        private void GetTargetsInAttackRangeNonAlloc(
            Dictionary<MapGridPoint, float> attackTiles,
            List<CharacterInstance> targetsInRange
        )
        {
            targetsInRange.Clear();
            foreach (var target in _context.Targets)
            {
                var targetGridPoint = _context.UnitInstance.UnitPositionToMapGridPoint(
                    target.MapGridPosition,
                    _context.mapGrid
                );
                // Use TryGetValue to avoid double lookup
                if (attackTiles.TryGetValue(targetGridPoint, out _))
                {
                    targetsInRange.Add(target);
                }
            }
        }

        /// <summary>
        /// Fills the provided list with allies in movement range (non-allocating).
        /// </summary>
        private void GetAlliesInMovementRangeNonAlloc(
            Dictionary<MapGridPoint, float> moveTiles,
            List<CharacterInstance> alliesInRange
        )
        {
            alliesInRange.Clear();
            foreach (var ally in _context.Allies)
            {
                var allyGridPoint = _context.UnitInstance.UnitPositionToMapGridPoint(
                    ally.MapGridPosition,
                    _context.mapGrid
                );
                // Use TryGetValue to avoid double lookup
                if (moveTiles.TryGetValue(allyGridPoint, out _))
                {
                    alliesInRange.Add(ally);
                }
            }
        }

        /// <summary>
        /// Finds the closest ally's position to the unit.
        /// </summary>
        private MapGridPoint GetClosestAllyPosition(List<CharacterInstance> allies)
        {
            var closestAllyPos = new Vector2Int();
            var closestDistance = float.MaxValue;

            foreach (var ally in allies)
            {
                var distance = Vector2.Distance(
                    _context.UnitInstance.MapGridPosition,
                    ally.MapGridPosition
                );

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestAllyPos = ally.MapGridPosition;
                }
            }

            return _context.UnitInstance.UnitPositionToMapGridPoint(
                closestAllyPos,
                _context.mapGrid
            );
        }

        /// <summary>
        /// Handles defensive AI behavior, finding tiles that increase distance from threats.
        /// Uses SoldierLoneWolf behavior to determine if unit should avoid allies.
        /// </summary>
        private void HandleRetreatBehavior(
            Dictionary<MapGridPoint, float> moveTiles,
            List<CharacterInstance> targetsInRange,
            List<CharacterInstance> alliesInMoveRange,
            Dictionary<string, float> behaviorDict
        )
        {
            if (targetsInRange == null || targetsInRange.Count == 0)
            {
                return;
            }

            var (closestEnemyPos, furthestEnemyPos, closestDistance, furthestDistance) =
                FindClosestAndFurthestEnemies(targetsInRange);

            // Use pooled lists for safe tile calculations
            using var safeTilesPooled = PooledList<MapGridPoint>.Get();
            var safeTiles = safeTilesPooled.List;

            FilterSafeTilesNonAlloc(
                moveTiles,
                closestEnemyPos,
                furthestEnemyPos,
                closestDistance,
                furthestDistance,
                safeTiles
            );

            if (Random.value < behaviorDict["SoldierLoneWolf"])
            {
                ExcludeTilesNearAlliesInPlace(safeTiles, alliesInMoveRange);
            }

            if (safeTiles.Count > 0)
            {
                var chosenTile = safeTiles[Random.Range(0, safeTiles.Count)];
                _context.MoveUnit(_context.UnitInstance, chosenTile.CoordinatesInt());
            }
            else
            {
                // TODO: End turn without moving (or pick a random tile?)
            }
        }

        /// <summary>
        /// Finds the closest and furthest enemies from the unit's current position.
        /// Returns positions and distances for both extremes.
        /// </summary>
        private (
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
                var targetPosition = target.MapGridPosition;
                var distance = Vector2.Distance(
                    _context.UnitInstance.MapGridPosition,
                    targetPosition
                );

                if (distance > furthestDistance)
                {
                    furthestDistance = distance;
                    furthestEnemyPos = targetPosition;
                }

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemyPos = targetPosition;
                }
            }

            return (closestEnemyPos, furthestEnemyPos, closestDistance, furthestDistance);
        }

        /// <summary>
        /// Fills the provided list with tiles that increase distance from enemies (non-allocating).
        /// </summary>
        private void FilterSafeTilesNonAlloc(
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
                var tilePosition = tile.Key;
                var tileCoords = tilePosition.Coordinates();
                var distanceToClosest = Vector2.Distance(tileCoords, closestEnemyPos);
                var distanceToFurthest = Vector2.Distance(tileCoords, furthestEnemyPos);

                if (distanceToClosest > closestDistance && distanceToFurthest >= furthestDistance)
                {
                    safeTiles.Add(tilePosition);
                }
            }
        }

        /// <summary>
        /// Removes tiles within 2 tiles of any ally (for lone wolf behavior, in-place).
        /// </summary>
        private void ExcludeTilesNearAlliesInPlace(
            List<MapGridPoint> tiles,
            List<CharacterInstance> allies
        )
        {
            for (int i = tiles.Count - 1; i >= 0; i--)
            {
                var tile = tiles[i];
                foreach (var ally in allies)
                {
                    if (Vector2.Distance(tile.Coordinates(), ally.MapGridPosition) <= 2)
                    {
                        tiles.RemoveAt(i);
                        break;
                    }
                }
            }
        }
    }
}
