using System;
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Environment;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Locations;
using Turnroot.Gameplay.Maps;
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
    public partial class BattleContext : MonoBehaviour
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
        public MapGrid MapGrid { get; private set; }

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
            MapGrid = mapGrid;
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
        private readonly Dictionary<string, PathfindingParameters> _cachedPathfindingParameters =
            new();
        private readonly Dictionary<
            string,
            PathfindingParameters
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
                MapGrid
            );
            var parameters = PathfindingParameters.FromCharacter(target, MapGrid, targetGridPoint);

            if (parameters == null || projectedDestination == null)
            {
                return false;
            }

            if (
                !PathfinderHelpers.TryComputePathMovementCost(
                    MapGrid,
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
                TargetsInRange = new List<CharacterInstance>(),
                AlliesInRange = new List<CharacterInstance>(),
                AdjacentUnits = new Adjacency(null),
            };
            Flags = new CombatFlags { ActiveUnitFlags = new UnitFlag() };
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
                TurnrootLogger.Log(
                    $"GetCurrentUnitPositions: Building cache with {allUnits.Count} units"
                );
                foreach (var unit in allUnits)
                {
                    // Validate unit position: ensure the map grid contains the point.
                    var mgp = unit.UnitPositionToMapGridPoint(unit.MapGridPosition, MapGrid);
                    if (mgp == null)
                    {
                        // If the unit has the sentinel uninitialized position, log that explicitly to make
                        // missing placements easy to find in logs and to distinguish from out-of-bounds values.
                        var sentinel = new Vector2Int(-9999, -9999);
                        if (unit.MapGridPosition == sentinel)
                        {
                            TurnrootLogger.Log(
                                $"GetCurrentUnitPositions: Unit {unit.CharacterTemplate.DisplayName} has uninitialized MapGridPosition (sentinel). Attempting roster-based repair.",
                                TurnrootLogger.LogLevel.Warning
                            );
                        }

                        // Attempt to repair from authoritative roster placements if available.
                        try
                        {
                            var bb = Brain?.battleBrain;
                            var battleObj = bb?.BattleObject;
                            if (battleObj?.PlayerTeamRoster != null)
                            {
                                var placements = battleObj.PlayerTeamRoster.GetPlacements();
                                foreach (var p in placements)
                                {
                                    if (p.CharacterData == unit.CharacterTemplate)
                                    {
                                        TurnrootLogger.Log(
                                            $"GetCurrentUnitPositions: Repairing {unit.CharacterTemplate.DisplayName} MapGridPosition from {unit.MapGridPosition} to {p.SpawnPosition}",
                                            TurnrootLogger.LogLevel.Warning
                                        );
                                        unit.MapGridPosition = p.SpawnPosition;
                                        mgp = unit.UnitPositionToMapGridPoint(
                                            unit.MapGridPosition,
                                            MapGrid
                                        );
                                        break;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            TurnrootLogger.Log(
                                "GetCurrentUnitPositions: Roster-based repair failed: "
                                    + ex.Message,
                                TurnrootLogger.LogLevel.Warning
                            );
                        }
                    }

                    if (mgp == null)
                    {
                        TurnrootLogger.Log(
                            $"GetCurrentUnitPositions: Skipping unit {unit.CharacterTemplate.DisplayName} with invalid MapGridPosition={unit.MapGridPosition}",
                            TurnrootLogger.LogLevel.Warning
                        );
                        continue;
                    }

                    if (currentUnitPositions.ContainsKey(unit.MapGridPosition))
                    {
                        TurnrootLogger.Log(
                            $"GetCurrentUnitPositions: Duplicate MapGridPosition detected for {unit.CharacterTemplate.DisplayName} at {unit.MapGridPosition}, skipping duplicate",
                            TurnrootLogger.LogLevel.Warning
                        );
                        continue;
                    }

                    TurnrootLogger.Log(
                        $"  - {unit.CharacterTemplate.DisplayName} id={unit.Id} at MapGridPosition={unit.MapGridPosition}"
                    );
                    currentUnitPositions[unit.MapGridPosition] = unit;
                }
            }
            return currentUnitPositions;
        }

        /// <summary>
        /// Repair unit positions using authoritative roster placements when inconsistencies are detected.
        /// This attempts to fix invalid or duplicate MapGridPosition values by consulting the PlayerTeamRoster.
        /// </summary>
        public void RepairUnitPositionsFromRoster()
        {
            try
            {
                var bb = Brain?.battleBrain;
                var battleObj = bb?.BattleObject;
                var roster = battleObj?.PlayerTeamRoster;
                if (roster == null)
                {
                    return;
                }

                var placements = roster.GetPlacements();
                // Build simple occupancy map to detect duplicates so we only repair invalid or duplicated positions
                var occupancy = new Dictionary<
                    Vector2Int,
                    List<CharacterInstance>
                >();
                foreach (var p2 in placements)
                {
                    var i2 = roster.GetInstanceFor(p2.CharacterData);
                    if (i2 == null)
                    {
                        continue;
                    }

                    if (!occupancy.TryGetValue(i2.MapGridPosition, out var list))
                    {
                        list = new List<CharacterInstance>();
                        occupancy[i2.MapGridPosition] = list;
                    }
                    list.Add(i2);
                }

                foreach (var p in placements)
                {
                    var inst = roster.GetInstanceFor(p.CharacterData);
                    if (inst == null)
                    {
                        continue;
                    }

                    var currentPoint = inst.UnitPositionToMapGridPoint(
                        inst.MapGridPosition,
                        MapGrid
                    );
                    var desiredPoint = MapGrid.GetGridPoint(p.SpawnPosition.x, p.SpawnPosition.y);
                    if (desiredPoint == null)
                    {
                        continue;
                    }

                    var isInvalid = currentPoint == null;
                    var isDuplicate =
                        occupancy.TryGetValue(inst.MapGridPosition, out var owners)
                        && owners.Count > 1;

                    if (!isInvalid && !isDuplicate)
                    {
                        // No problem for this instance: it either has a valid point and is not duplicated.
                        continue;
                    }

                    TurnrootLogger.Log(
                        $"RepairUnitPositionsFromRoster: Repairing {inst.CharacterTemplate.DisplayName} MapGridPosition from {inst.MapGridPosition} to {p.SpawnPosition} (invalid={isInvalid}, duplicate={isDuplicate})",
                        TurnrootLogger.LogLevel.Warning
                    );

                    try
                    {
                        if (currentPoint != null)
                        {
                            MapGrid.RemoveOccupied(currentPoint);
                        }
                    }
                    catch (Exception ex)
                    {
                        TurnrootLogger.Log(
                            "RepairUnitPositionsFromRoster: RemoveOccupied failed: " + ex.Message,
                            TurnrootLogger.LogLevel.Warning
                        );
                    }

                    try
                    {
                        var setResult = MapGrid.SetOccupied(desiredPoint, inst);
                        if (!setResult.Success)
                        {
                            // Fallback: if we couldn't set grid occupancy for some reason, set the instance position directly so roster alignment can continue.
                            inst.MapGridPosition = p.SpawnPosition;
                        }
                    }
                    catch (Exception ex)
                    {
                        TurnrootLogger.Log(
                            "RepairUnitPositionsFromRoster: SetOccupied failed: " + ex.Message,
                            TurnrootLogger.LogLevel.Warning
                        );
                        inst.MapGridPosition = p.SpawnPosition;
                    }
                }
            }
            catch (Exception ex)
            {
                TurnrootLogger.Log(
                    "RepairUnitPositionsFromRoster: Repair pass failed: " + ex.Message,
                    TurnrootLogger.LogLevel.Warning
                );
            }
        }

        public bool IsPlayerControlledUnit(CharacterInstance unit) =>
            unit != null && Participants.Allies.Contains(unit);

#if UNITY_EDITOR
        /// <summary>
        /// Debug helper: verify that for every unit, the MapGrid point at the instance's MapGridPosition
        /// has the same CurrentInstance. If not, attempt to repair by calling SetOccupied.
        /// Only runs in editor builds to avoid impacting runtime performance.
        /// </summary>
        public void DebugVerifyOccupancyAlignment()
        {
            try
            {
                var allUnits = Participants.GetAllUnits();
                foreach (var u in allUnits)
                {
                    if (u == null)
                    {
                        continue;
                    }

                    var mgp = u.UnitPositionToMapGridPoint(u.MapGridPosition, MapGrid);
                    if (mgp == null)
                    {
                        Turnroot.Utilities.TurnrootLogger.Log(
                            $"DebugVerifyOccupancyAlignment: Unit {u.Id} has invalid MapGridPosition {u.MapGridPosition}",
                            Turnroot.Utilities.TurnrootLogger.LogLevel.Warning
                        );
                        continue;
                    }

                    if (mgp.CurrentInstance != u)
                    {
                        Turnroot.Utilities.TurnrootLogger.Log(
                            $"DebugVerifyOccupancyAlignment: Mismatch at {mgp.CoordinatesInt} - grid has {mgp.CurrentInstance?.Id ?? "<none>"} vs unit {u.Id}. Repairing.",
                            Turnroot.Utilities.TurnrootLogger.LogLevel.Warning
                        );
                        try
                        {
                            MapGrid.SetOccupied(mgp, u);
                        }
                        catch (Exception ex)
                        {
                            TurnrootLogger.Log(
                                "DebugVerifyOccupancyAlignment: SetOccupied repair failed: "
                                    + ex.Message,
                                TurnrootLogger.LogLevel.Warning
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TurnrootLogger.Log(
                    "DebugVerifyOccupancyAlignment: Verification failed: " + ex.Message,
                    TurnrootLogger.LogLevel.Warning
                );
            }
        }
#endif

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
