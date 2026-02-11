using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Locations;
using Turnroot.Skills;
using Turnroot.Skills.Nodes;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    public partial class BattleContext
    {
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

        public T GetCustomData<T>(string key, T defaultValue = default) =>
            CustomData.TryGetValue(key, out object value) && value is T typedValue
                ? typedValue
                : defaultValue;

        public void SetCustomData(string key, object value) => CustomData[key] = value;

        public Dictionary<Vector2Int, CharacterInstance> GetCurrentUnitPositions(
            bool invalidateCache = false
        )
        {
            if (!invalidateCache && currentUnitPositions.Count > 0)
            {
                return currentUnitPositions;
            }

            currentUnitPositions.Clear();
            var allUnits = Participants.GetAllUnits();
            TurnrootLogger.Log(
                $"GetCurrentUnitPositions: Building cache with {allUnits.Count} units"
            );
            foreach (var unit in allUnits)
            {
                var result = ValidateAndRepairUnitPosition(unit);
                if (!result.Success)
                {
                    TurnrootLogger.Log(result.ErrorMessage, TurnrootLogger.LogLevel.Warning);
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
                currentUnitPositions[unit.MapGridPosition] = unit;
            }
            return currentUnitPositions;
        }

        private OperationResult ValidateAndRepairUnitPosition(CharacterInstance unit)
        {
            var notNullResult = OperationResultGuards.RequireNotNull(unit, nameof(unit));
            if (!notNullResult.Success)
            {
                return notNullResult;
            }

            var mgp = unit.UnitPositionToMapGridPoint(unit.MapGridPosition, MapGrid);
            if (mgp != null)
            {
                return OperationResult.Successful();
            }

            var sentinel = new Vector2Int(-9999, -9999);
            if (unit.MapGridPosition == sentinel)
            {
                TurnrootLogger.Log(
                    $"GetCurrentUnitPositions: Unit {unit.CharacterTemplate.DisplayName} has uninitialized MapGridPosition (sentinel). Attempting roster-based repair.",
                    TurnrootLogger.LogLevel.Warning
                );
            }

            var repairResult = TryRepairUnitPositionFromRoster(unit);
            if (!repairResult.Success)
            {
                return OperationResult.Failure(
                    $"GetCurrentUnitPositions: Skipping unit {unit.CharacterTemplate.DisplayName} with invalid MapGridPosition={unit.MapGridPosition}. {repairResult.ErrorMessage}"
                );
            }

            mgp = unit.UnitPositionToMapGridPoint(unit.MapGridPosition, MapGrid);
            return mgp == null
                ? OperationResult.Failure(
                    $"GetCurrentUnitPositions: Skipping unit {unit.CharacterTemplate.DisplayName} with invalid MapGridPosition={unit.MapGridPosition}"
                )
                : OperationResult.Successful();
        }

        private OperationResult TryRepairUnitPositionFromRoster(CharacterInstance unit)
        {
            // Defensive: prefer explicit checks rather than catch-all exceptions here
            var bb = Brain?.battleBrain;
            var battleObj = bb?.BattleObject;
            var roster = battleObj?.PlayerTeamRoster;
            if (roster == null)
            {
                return OperationResult.Failure("No PlayerTeamRoster available");
            }

            var placements = roster.GetPlacements();
            foreach (var p in placements)
            {
                if (p.CharacterData == unit.CharacterTemplate)
                {
                    TurnrootLogger.Log(
                        $"GetCurrentUnitPositions: Repairing {unit.CharacterTemplate.DisplayName} MapGridPosition from {unit.MapGridPosition} to {p.SpawnPosition}",
                        TurnrootLogger.LogLevel.Warning
                    );
                    unit.MapGridPosition = p.SpawnPosition;
                    return OperationResult.Successful();
                }
            }
            return OperationResult.Failure("No matching placement found in roster");
        }

        #endregion
    }
}
