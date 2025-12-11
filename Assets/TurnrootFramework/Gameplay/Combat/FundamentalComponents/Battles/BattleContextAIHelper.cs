using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Locations;
using Turnroot.Gameplay.Objects;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    public struct AITileData
    {
        public Dictionary<MapGridPoint, float> MoveTiles;
        public Dictionary<MapGridPoint, float> AttackTiles;
    }

    /// <summary>
    /// Helper class for AI decision-making and pathfinding in battle contexts.
    /// Handles movement tile calculation, target/ally filtering, and behavioral AI logic.
    /// </summary>
    public class BattleContextAIHelper
    {
        private readonly BattleContext _context;
        private AStarModified _aStarModified;

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
        /// Returns a dictionary of possible tiles that the unit can move to, including the range of its attacks.
        /// The function uses the A* algorithm to find all reachable tiles within the unit's movement range and attack range.
        /// </summary>
        public Dictionary<MapGridPoint, float> GetPossibleTilesIncludingRange(MapGridPoint start)
        {
            if (_context.UnitInstance?.CurrentClass?.ClassData == null)
            {
                return new Dictionary<MapGridPoint, float>();
            }

            var classData = _context.UnitInstance.CurrentClass.ClassData;
            var movementType = classData.movementType;

            var movementStat = _context.UnitInstance.GetUnboundedStat(
                Characters.Stats.UnboundedStatType.Movement
            );
            var movementBonusMod = classData.unboundedStatBonuses?.Find(b =>
                b.unboundedStatType == Characters.Stats.UnboundedStatType.Movement
            );
            var movementBonus = movementBonusMod.HasValue ? movementBonusMod.Value.value : 0f;

            var points = _aStarModified.GetReachable(
                _context.mapGrid,
                start,
                (int)(movementStat + movementBonus),
                movementType == MovementType.Infantry,
                movementType == MovementType.Flying,
                movementType == MovementType.Riding,
                classData.IsMagic,
                movementType == MovementType.Armored,
                0.95f,
                true,
                _context.UnitInstance.GetMaxRange()
            );

            return points ?? new Dictionary<MapGridPoint, float>();
        }

        /// <summary>
        /// Returns all tiles the unit can move to (excluding attack-only range).
        /// </summary>
        public Dictionary<MapGridPoint, float> GetPossibleMoveTiles(MapGridPoint start)
        {
            if (_context.UnitInstance?.CurrentClass?.ClassData == null)
            {
                return new Dictionary<MapGridPoint, float>();
            }

            var classData = _context.UnitInstance.CurrentClass.ClassData;
            var movementType = classData.movementType;

            var points = _aStarModified.GetReachable(
                _context.mapGrid,
                start,
                _context.UnitInstance.GetUnboundedStat(Characters.Stats.UnboundedStatType.Movement),
                movementType == MovementType.Infantry,
                movementType == MovementType.Flying,
                movementType == MovementType.Riding,
                classData.IsMagic,
                movementType == MovementType.Armored
            );

            return points ?? new Dictionary<MapGridPoint, float>();
        }

        /// <summary>
        /// Computes both movement and attack-only tiles for AI decision making.
        /// Attack tiles are those reachable by weapon range but not by movement.
        /// </summary>
        public AITileData GetTilesForAI(MapGridPoint start)
        {
            var moveTiles = GetPossibleMoveTiles(start);
            var allTiles = GetPossibleTilesIncludingRange(start);

            var attackTiles = new Dictionary<MapGridPoint, float>(allTiles.Count - moveTiles.Count);

            foreach (var tile in allTiles)
            {
                if (!moveTiles.ContainsKey(tile.Key))
                {
                    attackTiles[tile.Key] = tile.Value;
                }
            }

            return new AITileData { MoveTiles = moveTiles, AttackTiles = attackTiles };
        }

        /// <summary>
        /// Now that we have all the tiles, we freak it sensitive style with the unit's behavior data
        /// and any battle objectives
        /// This is a custom algorithm that I created, so it may be optimized in future by me or others
        /// </summary>
        public void PickTileAndAction()
        {
            /* ----------------------------- Assemble datas ----------------------------- */
            AITileData tileData = GetTilesForAI(
                _context.UnitInstance.UnitPositionToMapGridPoint(
                    _context.UnitInstance.MapGridPosition,
                    _context.mapGrid
                )
            );
            Dictionary<string, float> behaviorDict =
                _context.UnitInstance.CharacterTemplate.BehaviorSettings.GetBehaviorDictionary();

            // Look at Greedy / Bloodthirsty first, if we are going to go for loot, we don't need to worry
            // about targets or allies
            if (Random.value >= behaviorDict["BloodthirstGreed"])
            {
                HandleCombatBehavior(tileData, behaviorDict);
            }
            else
            {
                HandleGreedyBehavior(tileData);
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
                _ = _context.UnitInstance.MoveToPosition(
                    closestFeature.CoordinatesInt(),
                    _context.mapGrid
                );
                return true;
            }
            return false;
        }

        /// <summary>
        /// Handles AI behavior when unit prioritizes loot over combat.
        /// Move towards the closest treasure chest or, if none are found, towards the closest ally.
        /// </summary>
        private void HandleGreedyBehavior(AITileData tileData)
        {
            if (!MoveToClosestFeatureType(MapGridPointFeature.FeatureType.Treasure))
            {
                // if no treasure chests, move towards the closest ally
                var tryMove = MoveNextToClosestAlly(
                    tileData,
                    _context.UnitInstance.UnitPositionToMapGridPoint(
                        _context.UnitInstance.MapGridPosition,
                        _context.mapGrid
                    )
                );
                if (!tryMove.Success)
                {
                    // If no moveable neighbors are found, try to retreat to a safe position
                    if (!TryToRetreatToSafeTile(tileData))
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
        /// <param name="tileData"></param>
        /// <returns></returns>
        private bool TryToRetreatToSafeTile(AITileData tileData)
        {
            if (_context.Targets == null || _context.Targets.Count == 0)
            {
                return false;
            }

            var (closestEnemyPos, furthestEnemyPos, closestDistance, furthestDistance) =
                FindClosestAndFurthestEnemies(_context.Targets);
            var safeTiles = FilterSafeTiles(
                tileData,
                closestEnemyPos,
                furthestEnemyPos,
                closestDistance,
                furthestDistance
            );

            if (safeTiles.Count > 0)
            {
                var chosenTile = safeTiles[Random.Range(0, safeTiles.Count)];
                _ = _context.UnitInstance.MoveToPosition(
                    chosenTile.CoordinatesInt(),
                    _context.mapGrid
                );
                return true;
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
        /// <param name="tileData"></param>
        /// <param name="start"></param>
        /// <returns></returns>
        private OperationResult MoveNextToClosestAlly(AITileData tileData, MapGridPoint start)
        {
            var AlliesInMoveRange = GetAlliesInMovementRange(tileData);
            if (AlliesInMoveRange == null || AlliesInMoveRange.Count == 0)
            {
                return OperationResult.Failure("No allies found in movement range.");
            }

            var closestAlly = GetClosestAllyPosition(AlliesInMoveRange);
            // filter movable tiles to get the neighbors of the ally
            Dictionary<string, MapGridPoint> neighbors = closestAlly.GetNeighbors();
            var moveableNeighbors = new List<MapGridPoint>();
            foreach (var neighbor in neighbors)
            {
                if (tileData.MoveTiles.ContainsKey(neighbor.Value))
                {
                    moveableNeighbors.Add(neighbor.Value);
                }
            }
            // pick a random neighbor to move to
            if (moveableNeighbors.Count > 0)
            {
                var chosenTile = moveableNeighbors[Random.Range(0, moveableNeighbors.Count)];
                _ = _context.UnitInstance.MoveToPosition(
                    chosenTile.CoordinatesInt(),
                    _context.mapGrid
                );
                return OperationResult.SuccessResult();
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
            AITileData tileData,
            Dictionary<string, float> behaviorDict
        )
        {
            // Not greedy- combat proceeds
            // First, we get the available targets
            var TargetsInTileData = GetTargetsInAttackRange(tileData);

            // Then we get the allies in movement range (not attack range)
            var AlliesInMoveRange = GetAlliesInMovementRange(tileData);

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
                HandleRetreatBehavior(tileData, TargetsInTileData, AlliesInMoveRange, behaviorDict);
            }
            else
            {
                // TODO: Implement attack behavior
                // For now, do nothing until attack logic is implemented
            }
        }

        /// <summary>
        /// Filters the available targets to only those within attack range.
        /// </summary>
        private List<CharacterInstance> GetTargetsInAttackRange(AITileData tileData)
        {
            var targetsInRange = new List<CharacterInstance>();
            foreach (var target in _context.Targets)
            {
                var targetGridPoint = _context.UnitInstance.UnitPositionToMapGridPoint(
                    target.MapGridPosition,
                    _context.mapGrid
                );
                if (tileData.AttackTiles.ContainsKey(targetGridPoint))
                {
                    targetsInRange.Add(target);
                }
            }
            return targetsInRange;
        }

        /// <summary>
        /// Filters allies to only those within movement range (not attack range).
        /// </summary>
        private List<CharacterInstance> GetAlliesInMovementRange(AITileData tileData)
        {
            var alliesInRange = new List<CharacterInstance>();
            foreach (var ally in _context.Allies)
            {
                var allyGridPoint = _context.UnitInstance.UnitPositionToMapGridPoint(
                    ally.MapGridPosition,
                    _context.mapGrid
                );
                if (tileData.MoveTiles.ContainsKey(allyGridPoint))
                {
                    alliesInRange.Add(ally);
                }
            }
            return alliesInRange;
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
            AITileData tileData,
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

            var safeTiles = FilterSafeTiles(
                tileData,
                closestEnemyPos,
                furthestEnemyPos,
                closestDistance,
                furthestDistance
            );

            if (Random.value < behaviorDict["SoldierLoneWolf"])
            {
                safeTiles = ExcludeTilesNearAllies(safeTiles, alliesInMoveRange);
            }

            if (safeTiles.Count > 0)
            {
                var chosenTile = safeTiles[Random.Range(0, safeTiles.Count)];
                _ = _context.UnitInstance.MoveToPosition(
                    chosenTile.CoordinatesInt(),
                    _context.mapGrid
                );
                // TODO: Make sure whatever brain is calling all this fires the needed events
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
        /// Filters movement tiles to only those that increase distance from the closest enemy
        /// while maintaining or increasing distance from the furthest enemy.
        /// </summary>
        private List<MapGridPoint> FilterSafeTiles(
            AITileData tileData,
            Vector2 closestEnemyPos,
            Vector2 furthestEnemyPos,
            float closestDistance,
            float furthestDistance
        )
        {
            var safeTiles = new List<MapGridPoint>();

            foreach (var tile in tileData.MoveTiles)
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

            return safeTiles;
        }

        /// <summary>
        /// Excludes tiles within 2 tile of any ally (for lone wolf behavior).
        /// </summary>
        private List<MapGridPoint> ExcludeTilesNearAllies(
            List<MapGridPoint> tiles,
            List<CharacterInstance> allies
        )
        {
            var filteredTiles = new List<MapGridPoint>();

            foreach (var tile in tiles)
            {
                bool isTooCloseToAlly = false;

                foreach (var ally in allies)
                {
                    if (Vector2.Distance(tile.Coordinates(), ally.MapGridPosition) <= 2)
                    {
                        isTooCloseToAlly = true;
                        break;
                    }
                }

                if (!isTooCloseToAlly)
                {
                    filteredTiles.Add(tile);
                }
            }

            return filteredTiles;
        }
    }
}
