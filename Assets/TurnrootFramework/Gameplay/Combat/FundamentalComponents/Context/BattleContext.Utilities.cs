using System.Collections.Generic;
using System.Linq;
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
            this.LogInfo($"GetCurrentUnitPositions: Building cache with {allUnits.Count} units");
            foreach (var unit in allUnits)
            {
                var result = ValidateAndRepairUnitPosition(unit);
                if (!result.Success)
                {
                    this.LogWarning(
                        $"GetCurrentUnitPositions: Skipping {unit.CharacterTemplate.DisplayName}; {result.ErrorMessage}"
                    );
                    continue;
                }

                if (currentUnitPositions.ContainsKey(unit.MapGridPosition))
                {
                    this.LogWarning(
                        $"GetCurrentUnitPositions: Duplicate MapGridPosition detected for {unit.CharacterTemplate.DisplayName} at {unit.MapGridPosition}, skipping duplicate"
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
                this.LogWarning(
                    $"GetCurrentUnitPositions: Unit {unit.CharacterTemplate.DisplayName} has uninitialized MapGridPosition (sentinel). Attempting roster-based repair."
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
                    this.LogWarning(
                        $"GetCurrentUnitPositions: Repairing {unit.CharacterTemplate.DisplayName} MapGridPosition from {unit.MapGridPosition} to {p.SpawnPosition}"
                    );
                    unit.MapGridPosition = p.SpawnPosition;
                    return OperationResult.Successful();
                }
            }
            return OperationResult.Failure("No matching placement found in roster");
        }

        /// <summary>
        /// Ensures that a given character instance is registered in the battle participants list.
        /// This is required so that command lookups (eg. <see cref="SpawnCommand"/>) can find
        /// the unit by id and so the context knows the unit's allegiance for targeting logic.
        /// If the unit is already present no changes are made. We try to infer the correct
        /// team (allies/third‑party/targets) based on any available rosters and fall back to
        /// treating the unit as an enemy (Targets) when unsure.
        /// </summary>
        public void EnsureUnitIsParticipant(CharacterInstance unit)
        {
            if (unit == null)
            {
                return;
            }

            var parts = Participants;
            if (parts.GetAllUnits().Contains(unit))
            {
                return; // already registered
            }

            bool added = false;
            var battleObj = Brain?.battleBrain?.BattleObject;
            if (battleObj != null)
            {
                var playerRoster = battleObj.PlayerTeamRoster;
                if (playerRoster != null && playerRoster.Instances.Contains(unit))
                {
                    parts.Allies.Add(unit);
                    added = true;
                }
                else if (
                    battleObj.HasThirdParty
                    && battleObj.ThirdPartyTeamRoster != null
                    && battleObj.ThirdPartyTeamRoster.Instances.Contains(unit)
                )
                {
                    parts.ThirdParty.Add(unit);
                    added = true;
                }
            }

            if (!added)
            {
                // default to treating as an enemy
                parts.Targets.Add(unit);
            }
        }

        #endregion
    }
}
