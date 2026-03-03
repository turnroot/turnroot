using System.Linq;
using Turnroot.Characters;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    // Partial file to host spawn & placement helpers extracted from BattleBrain
    public partial class BattleBrain
    {
        /// <summary>
        /// Spawn roster units onto the grid at their assigned MapGridPosition.
        /// Positions are already set by ApplyPlacementsToBattle.
        /// </summary>
        private void SpawnRosterUnitsOntoGrid()
        {
            var playerTeamRoster = BattleObject.PlayerTeamRoster;
            var thirdPartyRoster = BattleObject.HasThirdParty
                ? BattleObject.ThirdPartyTeamRoster
                : null;

            if (playerTeamRoster != null)
            {
                SpawnRosterUnits(playerTeamRoster);
            }

            if (thirdPartyRoster != null)
            {
                SpawnRosterUnits(thirdPartyRoster);
            }
        }

        /// <summary>
        /// Spawn units from a roster. Positions are already set on CharacterInstance.MapGridPosition.
        /// Battle roster already contains only selected units (as battle copies), so no filtering needed.
        /// </summary>
        private void SpawnRosterUnits<T>(RosterInstance<T> roster)
            where T : Characters.Roster
        {
            if (!ValidationHelper.ValidateNotNull(roster, nameof(roster)))
            {
                return;
            }

            foreach (var instance in roster.Instances)
            {
                if (instance == null)
                {
                    continue;
                }

                var position = instance.MapGridPosition;
                var characterData = instance.CharacterTemplate;

                $"[SPAWN TRACKING] SpawnRosterUnits: Processing {characterData?.DisplayName}, instance.MapGridPosition={position}, WasSpawnedDuringBattle={instance.WasSpawnedDuringBattle}".LogInfo();

                // Skip if already spawned during battle (via SpawnCommand)
                if (instance.WasSpawnedDuringBattle)
                {
                    $"SpawnRosterUnits: Skipping {characterData?.DisplayName} - already spawned at {position}".LogInfo();
                    continue;
                }

                // Validate position is on the grid
                var gridPoint = BattleObject.Context.MapGrid?.GetGridPoint(position.x, position.y);
                if (gridPoint == null)
                {
                    $"SpawnRosterUnits: Invalid position {position} for {characterData?.DisplayName}, skipping".LogWarning();
                    continue;
                }

                // Spawn the unit at its position
                var spawned = BattleObject.Context.SpawnAtPosition(instance, position);
                if (!spawned)
                {
                    $"SpawnRosterUnits: SpawnAtPosition failed for {characterData?.DisplayName} at {position}".LogWarning();
                }
                else
                {
                    $"SpawnRosterUnits: Spawned {characterData?.DisplayName} at {position}".LogInfo();
                }
            }
        }
    }
}
