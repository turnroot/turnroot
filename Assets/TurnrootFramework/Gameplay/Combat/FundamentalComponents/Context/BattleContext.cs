using System;
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Commands;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Environment;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Locations;
using Turnroot.Gameplay.Maps;
using Turnroot.Gameplay.Objects;
using Turnroot.Skills;
using Turnroot.Skills.Nodes;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    /// <summary>
    /// Runtime context for the entire battle.
    /// Contains all the dynamic data that skills and other systems need at runtime.
    /// </summary>
    public class BattleContext : MonoBehaviour
    {
        #region Core Properties and Initialization

        /// <summary>
        /// Reference to the Brain for publishing events.
        /// Set this when creating the BattleContext. Use Initialize() to assign.
        /// </summary>
        public Brain.Brain Brain { get; private set; }

        /// <summary>
        /// Active map graph for this battle.
        /// </summary>
        public MapGrid mapGrid { get; private set; }

        public BattleContextAIHelper AIHelper { get; private set; }

        /// <summary>
        /// Initialize the BattleContext with required dependencies. Throws if brain is null.
        /// </summary>
        public void Initialize(Brain.Brain brain, MapGrid mapGrid)
        {
            if (brain == null)
            {
                throw new ArgumentNullException(nameof(brain));
            }

            Brain = brain;
            this.mapGrid = mapGrid;
            AIHelper = new BattleContextAIHelper(this);
        }

        #endregion

        #region Sub-Contexts and State

        // Sub-contexts for clearer separation
        public UnitContext Unit { get; private set; }
        public SkillContext Skill { get; private set; }
        public BattleParticipants Participants { get; private set; }
        public CombatFlags Flags { get; private set; }

        // Cached unit positions so we don't have to query each unit every time
        public Dictionary<Vector2Int, CharacterInstance> currentUnitPositions = new();

        // Cache of computed tiles per unit to avoid duplicate pathfinding work
        private readonly Dictionary<string, CachedTileData> _unitTilesCache = new();

        // Cache precomputed pathfinding parameters (with and without weapon range) to avoid
        // repeated construction and unbounded stat lookups during AI pathfinding queries.
        private readonly Dictionary<
            string,
            Turnroot.Gameplay.Maps.PathfindingParameters
        > _cachedPathfindingParameters = new();
        private readonly Dictionary<
            string,
            Turnroot.Gameplay.Maps.PathfindingParameters
        > _cachedPathfindingParametersWithRange = new();

        private class CachedTileData
        {
            public Dictionary<MapGridPoint, float> MoveTiles;
            public Dictionary<MapGridPoint, float> AttackTiles;
            public int MapStateVersion; // Track map version to invalidate on map changes
            public Vector2Int UnitPosition; // Track position to invalidate on movement

            public CachedTileData(
                Dictionary<MapGridPoint, float> move,
                Dictionary<MapGridPoint, float> attack,
                int mapVersion,
                Vector2Int position
            )
            {
                MoveTiles = move;
                AttackTiles = attack;
                MapStateVersion = mapVersion;
                UnitPosition = position;
            }
        }

        public EnvironmentalConditions EnvironmentalConditions { get; set; }
        public Dictionary<string, object> CustomData { get; private set; }

        // Track last attacker per target for the current battle
        private readonly Dictionary<string, CharacterInstance> _lastAttackerByTarget = new();

        #endregion

        #region Last Attacker Tracking

        /// <summary>
        /// Returns the last attacker who attacked the specified target during this battle, or null.
        /// </summary>
        public CharacterInstance GetLastAttacker(CharacterInstance target) =>
            target != null && _lastAttackerByTarget.TryGetValue(target.Id, out var a) ? a : null;

        /// <summary>
        /// Registers that attacker attacked target for the purposes of per-battle queries.
        /// Passing null removes the entry.
        /// </summary>
        public void RegisterLastAttacker(CharacterInstance target, CharacterInstance attacker)
        {
            if (target == null)
            {
                return;
            }

            if (attacker == null)
            {
                _lastAttackerByTarget.Remove(target.Id);
            }
            else
            {
                _lastAttackerByTarget[target.Id] = attacker;
            }
        }

        /// <summary>
        /// Clears last-attacker tracking for this battle.
        /// </summary>
        public void ClearLastAttackHistory() => _lastAttackerByTarget.Clear();

        #endregion

        #region Combat State and Effectiveness

        // Combat state flags (backward-compatible wrappers)

        public bool AttackIsEffective(CharacterInstance unit, CharacterInstance target)
        {
            if (unit == null || target == null)
            {
                return false;
            }

            var attackerWeapon = unit.GetEquippedWeapon();
            if (attackerWeapon == null || attackerWeapon.Template == null)
            {
                return false;
            }

            var weaponTemplate = attackerWeapon.Template;

            // Check species effectiveness
            var targetSpecies = target.CharacterTemplate?.Species;
            if (targetSpecies != null && weaponTemplate.SpeciesEffectiveAgainst != null)
            {
                foreach (var s in weaponTemplate.SpeciesEffectiveAgainst)
                {
                    if (s == targetSpecies)
                    {
                        return true;
                    }
                }
            }

            // Check weapon-type effectiveness against the target's equipped weapon
            var targetWeapon = target.GetEquippedWeapon();
            if (targetWeapon?.Template != null)
            {
                var targetWeaponType = targetWeapon.Template.WeaponType;
                if (weaponTemplate.WeaponTypesEffectiveAgainst != null)
                {
                    foreach (var wt in weaponTemplate.WeaponTypesEffectiveAgainst)
                    {
                        return wt == targetWeaponType;
                    }
                }
            }

            return false;
        }

        public bool AttackWouldKill(CharacterInstance target)
        {
            if (Unit.UnitInstance == null || target == null)
            {
                return false;
            }

            var weaponItem = Unit.UnitInstance.GetEquippedWeapon();
            return weaponItem != null
                && DamageCalculator.WouldKill(Unit.UnitInstance, target, weaponItem, this);
        }

        public bool TargetCanCounterattack(
            CharacterInstance self,
            CharacterInstance target,
            MapGridPoint projectedDestination
        )
        {
            if (self == null || target == null)
            {
                return false;
            }
            var targetWeapon = target.GetEquippedWeapon();
            if (targetWeapon == null)
            {
                return false;
            }

            var targetAttackRange = targetWeapon.Template.UpperRange;

            var targetGridPoint = target.UnitPositionToMapGridPoint(
                target.MapGridPosition,
                mapGrid
            );
            var parameters = PathfindingParameters.FromCharacter(target, mapGrid, targetGridPoint);

            if (parameters == null || projectedDestination == null)
            {
                return false;
            }

            if (
                !PathfinderHelpers.TryComputePathMovementCost(
                    mapGrid,
                    parameters,
                    projectedDestination,
                    out float totalCost
                )
            )
            {
                return false;
            }

            // Compare path cost to attack range (treating range as movement-cost budget)
            return totalCost <= targetAttackRange;
        }

        #endregion

        #region Constructor and Utilities

        public BattleContext()
        {
            CustomData = new Dictionary<string, object>();
            Unit = new UnitContext();
            Skill = new SkillContext
            {
                ActiveSkills = new List<Skill>(),
                ActiveSkillGraphs = new List<SkillGraph>(),
                SkillUseCount = new Dictionary<Skill, int>(),
            };
            Participants = new BattleParticipants
            {
                Targets = new List<CharacterInstance>(),
                Allies = new List<CharacterInstance>(),
                ThirdParty = new List<CharacterInstance>(),
                AdjacentUnits = new Adjacency(null),
            };
            Flags = new CombatFlags();
        }

        // Get a custom data value, or default if not found
        public T GetCustomData<T>(string key, T defaultValue = default) =>
            CustomData.TryGetValue(key, out object value) && value is T typedValue
                ? typedValue
                : defaultValue;

        // Set a custom data value
        public void SetCustomData(string key, object value) => CustomData[key] = value;

        public Dictionary<Vector2Int, CharacterInstance> GetCurrentUnitPositions(
            bool invalidateCache = false
        )
        {
            if (invalidateCache || currentUnitPositions.Count == 0)
            {
                currentUnitPositions.Clear();
                var allUnits = Participants.GetAllUnits();
                foreach (var unit in allUnits)
                {
                    currentUnitPositions[unit.MapGridPosition] = unit;
                }
            }
            return currentUnitPositions;
        }

        public void InvalidateUnitPositionCache() => currentUnitPositions.Clear();

        /// <summary>
        /// Get or compute valid tiles for a unit. Results are cached and automatically
        /// invalidated when the unit moves or map changes.
        /// </summary>
        public bool TryGetValidTilesForUnit(
            CharacterInstance unit,
            out Dictionary<MapGridPoint, float> moveTiles,
            out Dictionary<MapGridPoint, float> attackTiles,
            bool forceRecompute = false
        )
        {
            moveTiles = new Dictionary<MapGridPoint, float>();
            attackTiles = new Dictionary<MapGridPoint, float>();

            if (unit == null || mapGrid == null)
            {
                return false;
            }

            // Check if we have valid cached data
            if (!forceRecompute && _unitTilesCache.TryGetValue(unit.Id, out var cached))
            {
                bool cacheValid =
                    cached.MapStateVersion == mapGrid.StateVersion
                    && cached.UnitPosition == unit.MapGridPosition;

                if (cacheValid)
                {
                    TurnrootLogger.Log(
                        $"BattleContext: Using cached tiles for {unit.CharacterTemplate.DisplayName}"
                    );
                    moveTiles = cached.MoveTiles;
                    attackTiles = cached.AttackTiles;
                    return true;
                }
                else
                {
                    TurnrootLogger.Log(
                        $"BattleContext: Cache invalidated for {unit.CharacterTemplate.DisplayName} (map version: {cached.MapStateVersion} vs {mapGrid.StateVersion}, position: {cached.UnitPosition} vs {unit.MapGridPosition})"
                    );
                }
            }

            // Compute tiles using AIHelper
            var startPoint = unit.UnitPositionToMapGridPoint(unit.MapGridPosition, mapGrid);
            if (startPoint == null)
            {
                return false;
            }

            TurnrootLogger.Log(
                $"BattleContext: Computing tiles for {unit.CharacterTemplate.DisplayName} at {unit.MapGridPosition}"
            );

            var move = new Dictionary<MapGridPoint, float>();
            var attack = new Dictionary<MapGridPoint, float>();

            // Try to use cached pathfinding parameters when available to reduce allocations
            var parametersWithRange = GetCachedPathfindingParameters(unit, includeRange: true);
            bool success;
            if (parametersWithRange != null)
            {
                // Use the AIHelper variant that accepts precomputed parameters if available
                success = AIHelper.GetTilesForAINonAlloc(startPoint, move, attack);
            }
            else
            {
                success = AIHelper.GetTilesForAINonAlloc(startPoint, move, attack);
            }

            if (success)
            {
                // Cache copies to avoid external mutation
                _unitTilesCache[unit.Id] = new CachedTileData(
                    new Dictionary<MapGridPoint, float>(move),
                    new Dictionary<MapGridPoint, float>(attack),
                    mapGrid.StateVersion,
                    unit.MapGridPosition
                );

                TurnrootLogger.Log(
                    $"BattleContext: Cached {move.Count} move tiles and {attack.Count} attack tiles for {unit.CharacterTemplate.DisplayName}"
                );

                moveTiles = _unitTilesCache[unit.Id].MoveTiles;
                attackTiles = _unitTilesCache[unit.Id].AttackTiles;
            }

            return success;
        }

        /// <summary>
        /// Precompute and cache PathfindingParameters for the provided unit (with and without attack range)
        /// so that subsequent pathfinding queries can avoid re-creating parameter objects.
        /// </summary>
        public bool PrecomputePathfindingParameters(CharacterInstance unit)
        {
            if (unit == null || mapGrid == null)
            {
                return false;
            }

            var startPoint = unit.UnitPositionToMapGridPoint(unit.MapGridPosition, mapGrid);
            if (startPoint == null)
            {
                return false;
            }

            var p = Turnroot.Gameplay.Maps.PathfindingParameters.FromCharacter(
                unit,
                mapGrid,
                startPoint
            );
            if (p != null && p.IsValid())
            {
                _cachedPathfindingParameters[unit.Id] = p;
            }

            // Avoid recomputing base movement parameters a second time (which may re-read stats/log).
            // If we successfully built the base parameters, create the ranged variant by cloning.
            Turnroot.Gameplay.Maps.PathfindingParameters pr = null;
            if (p != null && p.IsValid())
            {
                pr = p.Clone();
                pr.IncludeRange = true;
                pr.MaxRange = unit.GetMaxRange();
            }
            else
            {
                // Fallback: compute ranged parameters directly
                pr = Turnroot.Gameplay.Maps.PathfindingParameters.FromCharacterWithRange(
                    unit,
                    mapGrid,
                    startPoint
                );
            }

            if (pr != null && pr.IsValid())
            {
                _cachedPathfindingParametersWithRange[unit.Id] = pr;
            }

            return _cachedPathfindingParameters.ContainsKey(unit.Id)
                || _cachedPathfindingParametersWithRange.ContainsKey(unit.Id);
        }

        public Turnroot.Gameplay.Maps.PathfindingParameters GetCachedPathfindingParameters(
            CharacterInstance unit,
            bool includeRange = false
        )
        {
            if (unit == null)
            {
                return null;
            }

            if (includeRange)
            {
                if (_cachedPathfindingParametersWithRange.TryGetValue(unit.Id, out var pr))
                {
                    return pr.Clone();
                }
            }
            else
            {
                if (_cachedPathfindingParameters.TryGetValue(unit.Id, out var p))
                {
                    return p.Clone();
                }
            }

            return null;
        }

        public void InvalidatePathfindingParameters(string unitId)
        {
            _cachedPathfindingParameters.Remove(unitId);
            _cachedPathfindingParametersWithRange.Remove(unitId);
        }

        public void InvalidateAllPathfindingParameters()
        {
            _cachedPathfindingParameters.Clear();
            _cachedPathfindingParametersWithRange.Clear();
        }

        /// <summary>
        /// Invalidate cached tiles for a specific unit (call when unit moves/acts/changes state)
        /// </summary>
        public void InvalidateUnitTileCache(string unitId)
        {
            if (_unitTilesCache.Remove(unitId))
            {
                TurnrootLogger.Log($"BattleContext: Invalidated tile cache for unit {unitId}");
            }
        }

        /// <summary>
        /// Invalidate cached tiles for a specific unit by instance
        /// </summary>
        public void InvalidateUnitTileCache(CharacterInstance unit)
        {
            if (unit != null)
            {
                InvalidateUnitTileCache(unit.Id);
            }
        }

        /// <summary>
        /// Clear all cached tiles (call on turn end, unit spawned/defeated, map changes)
        /// </summary>
        public void InvalidateAllTileCaches()
        {
            int count = _unitTilesCache.Count;
            _unitTilesCache.Clear();
            TurnrootLogger.Log($"BattleContext: Cleared all tile caches ({count} units)");
        }

        public bool IsPlayerControlledUnit(CharacterInstance unit) =>
            unit != null && Participants.Allies.Contains(unit);

        #endregion

        #region Command-Based Actions

        /// <summary>
        /// Moves a unit to a position using the command pattern.
        /// </summary>
        /// <param name="unit">The unit to move.</param>
        /// <param name="targetPosition">The target position.</param>
        /// <returns>True if the move succeeded.</returns>
        public OperationResult MoveUnitToPointInt(CharacterInstance unit, Vector2Int CoordinatesInt)
        {
            var command = new MoveCommand(unit.Id, CoordinatesInt, Brain.CurrentTurnNumber);
            return Brain.ExecuteCommand(command)
                ? OperationResult.Successful()
                : OperationResult.Failure("Move command failed to execute");
        }

        public bool SpawnAtPosition(CharacterInstance unit, Vector2Int spawnPosition)
        {
            var command = new SpawnCommand(unit.Id, spawnPosition, Brain.CurrentTurnNumber);
            bool success = Brain.ExecuteCommand(command);
            if (success)
            {
                // Invalidate all tile caches when new unit spawns
                InvalidateAllTileCaches();
            }
            return success;
        }

        public OperationResult MoveUnitToPoint(CharacterInstance unit, MapGridPoint targetPoint)
        {
            var command = new MoveCommand(
                unit.Id,
                targetPoint.CoordinatesInt,
                Brain.CurrentTurnNumber
            );
            return Brain.ExecuteCommand(command)
                ? OperationResult.Successful()
                : OperationResult.Failure("Move command failed to execute");
        }

        public OperationResult AttackTarget(
            CharacterInstance attacker,
            CharacterInstance target,
            ObjectItemInstance weaponItem = null
        )
        {
            // If a weapon was not explicitly provided, use the currently equipped weapon
            if (weaponItem == null)
            {
                weaponItem = attacker.GetEquippedWeapon();
            }

            if (weaponItem == null)
            {
                Debug.LogWarning(
                    $"BattleContext: {attacker.CharacterTemplate.DisplayName} has no weapon to attack with!"
                );
                return OperationResult.Failure("No weapon to attack with");
            }

            return DealDamage(
                attacker,
                target,
                DamageCalculator.CalculatePotentialDamage(attacker, target, weaponItem, this)
            )
                ? OperationResult.Successful()
                : OperationResult.Failure("Attack command failed to execute");
        }

        /// <summary>
        /// Deals damage to a target unit using the command pattern.
        /// </summary>
        /// <param name="attacker">The attacking unit.</param>
        /// <param name="target">The target unit.</param>
        /// <param name="damage">The damage amount to deal.</param>
        /// <returns>True if the damage was applied.</returns>
        public bool DealDamage(CharacterInstance attacker, CharacterInstance target, int damage)
        {
            var command = new DamageCommand(
                attacker.Id,
                target.Id,
                damage,
                Brain.CurrentTurnNumber
            );
            return Brain.ExecuteCommand(command);
        }

        /// <summary>
        /// Uses an item using the command pattern.
        /// </summary>
        /// <param name="user">The unit using the item.</param>
        /// <param name="item">The item to use.</param>
        /// <param name="target">Optional target for the item.</param>
        /// <returns>True if the item use succeeded.</returns>
        public bool UseItem(
            CharacterInstance user,
            ObjectItemInstance item,
            CharacterInstance target = null
        )
        {
            var command = new UseItemCommand(
                user.Id,
                item.InstanceID,
                target?.Id,
                Brain.CurrentTurnNumber
            );
            return Brain.ExecuteCommand(command);
        }

        public bool HealUnit(
            CharacterInstance user,
            CharacterInstance target,
            ObjectItemInstance fromItem = null
        )
        {
            // If healing comes from an item, use UseItem
            if (fromItem != null)
            {
                UseItem(user, fromItem, target);
            }
            else
            {
                var command = new HealCommand(user.Id, target.Id, Brain.CurrentTurnNumber);
                return Brain.ExecuteCommand(command);
            }
            return true;
        }

        /// <summary>
        /// Ends the current unit's turn using the command pattern.
        /// </summary>
        /// <returns>True if ending turn succeeded.</returns>
        public bool EndTurn()
        {
            var command = new EndTurnCommand(Brain.CurrentTurnNumber);
            return Brain.ExecuteCommand(command);
        }

        /// <summary>
        /// Checks if a character is an ally in this context.
        /// </summary>
        public bool IsAlly(CharacterInstance character) =>
            Participants?.Allies?.Contains(character) ?? false;

        /// <summary>
        /// Checks if a character is a target (potential enemy) in this context.
        /// </summary>
        public bool IsTarget(CharacterInstance character) =>
            Participants?.Targets?.Contains(character) ?? false;

        public int CalculatePotentialDamage(
            CharacterInstance unitInstance,
            CharacterInstance target,
            ObjectItemInstance weaponItem
        ) => DamageCalculator.CalculatePotentialDamage(unitInstance, target, weaponItem, this);
        #endregion

        // Input event definitions
        public class BattleInputNavigateEvent
        {
            public Vector2 Direction { get; set; }
        }

        public class BattleInputConfirmEvent { }

        public class BattleInputCancelEvent { }

        public class BattleInputMenuEvent { }
    }
}
