using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Locations;
using Turnroot.Skills;
using Turnroot.Skills.Nodes;
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

        /// <summary>
        /// Get current unit positions by directly querying CharacterInstance.MapGridPosition.
        /// No caching, no repair - positions are always current.
        /// </summary>
        public Dictionary<Vector2Int, CharacterInstance> GetCurrentUnitPositions(
            bool invalidateCache = false
        )
        {
            var positions = new Dictionary<Vector2Int, CharacterInstance>();
            var allUnits = Participants.GetAllUnits();

            foreach (var unit in allUnits)
            {
                if (unit == null)
                {
                    continue;
                }

                var pos = unit.MapGridPosition;

                // Skip invalid sentinel positions
                var sentinel = new Vector2Int(-9999, -9999);
                if (pos == sentinel)
                {
                    this.LogWarning(
                        $"GetCurrentUnitPositions: Unit {unit.CharacterTemplate?.DisplayName} has invalid sentinel position"
                    );
                    continue;
                }

                // Validate position is on grid
                var gridPoint = MapGrid?.GetGridPoint(pos.x, pos.y);
                if (gridPoint == null)
                {
                    this.LogWarning(
                        $"GetCurrentUnitPositions: Unit {unit.CharacterTemplate?.DisplayName} has invalid position {pos}"
                    );
                    continue;
                }

                // Skip duplicates (shouldn't happen in correct system)
                if (positions.ContainsKey(pos))
                {
                    this.LogWarning(
                        $"GetCurrentUnitPositions: Duplicate position {pos} detected for {unit.CharacterTemplate?.DisplayName}"
                    );
                    continue;
                }

                positions[pos] = unit;
            }

            return positions;
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
