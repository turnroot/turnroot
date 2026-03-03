using Turnroot.Characters;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    // Partial file to host spawn & placement helpers extracted from BattleBrain
    public partial class BattleBrain
    {
        private void SpawnRosterUnitsOntoGrid()
        {
            var playerTeamRoster = BattleObject.PlayerTeamRoster;
            var thirdPartyRoster = BattleObject.HasThirdParty
                ? BattleObject.ThirdPartyTeamRoster
                : null;

            if (playerTeamRoster != null)
            {
                SpawnAndOrderRosterPlacements(playerTeamRoster);
            }

            if (thirdPartyRoster != null)
            {
                SpawnAndOrderRosterPlacements(thirdPartyRoster);
            }

            BattleObject.Context.InvalidateUnitPositionCache();
        }

        /// <summary>
        /// Spawns and orders all placements for any roster type.
        /// Works for both <see cref="PlayerTeamRosterInstance"/> and <see cref="GenericRosterInstance"/>
        /// because all required methods (GetPlacements, SetOrder, GetInstanceFor, AddInstance)
        /// are defined on the shared <see cref="RosterInstance{T}"/> base class.
        /// </summary>
        private void SpawnAndOrderRosterPlacements<T>(RosterInstance<T> roster)
            where T : Turnroot.Characters.Roster
        {
            if (!ValidationHelper.ValidateNotNull(roster, nameof(roster)))
            {
                return;
            }

            foreach (var placement in roster.GetPlacements())
            {
                var characterData = placement.CharacterData;
                var instance = EnsureInstanceForPlacement(roster, characterData);
                if (instance == null)
                {
                    $"SpawnRosterUnitsOntoGrid: No instance for {characterData?.DisplayName}; skipping spawn".LogWarning();
                    roster.SetOrder(characterData, placement.Order);
                    continue;
                }

                // Only skip if the unit was already formally spawned via SpawnCommand during this
                // battle (WasSpawnedDuringBattle == true). Pre-battle spawning (prebattle: true)
                // sets MapGridPosition but does NOT call MapGrid.SetOccupied, so the unit is not
                // registered in the battle grid and must still go through SpawnAtPosition.
                if (
                    instance.MapGridPosition == placement.SpawnPosition
                    && instance.WasSpawnedDuringBattle
                )
                {
                    $"SpawnRosterUnitsOntoGrid: Skipping spawn for {instance.CharacterTemplate.DisplayName} - already spawned at {placement.SpawnPosition}".LogInfo();
                    roster.SetOrder(characterData, placement.Order);
                    continue;
                }

                TryRepairPlacementIfMismatch(instance, placement.SpawnPosition);

                var spawned = BattleObject.Context.SpawnAtPosition(
                    instance,
                    placement.SpawnPosition
                );
                if (!spawned)
                {
                    $"SpawnRosterUnitsOntoGrid: SpawnAtPosition failed for {characterData?.DisplayName} at {placement.SpawnPosition}".LogWarning();
                }

                roster.SetOrder(characterData, placement.Order);
            }
        }

        private void TryRepairPlacementIfMismatch(
            CharacterInstance characterInstance,
            Vector2Int spawnPosition
        )
        {
            if (characterInstance == null)
            {
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            $"SpawnRosterUnitsOntoGrid: Repairing {characterInstance.CharacterTemplate.DisplayName} MapGridPosition from {characterInstance.MapGridPosition} to {spawnPosition}".LogWarning();

            try
            {
                var oldP = characterInstance.UnitPositionToMapGridPoint(
                    characterInstance.MapGridPosition,
                    BattleObject.Context.MapGrid
                );
                if (oldP != null)
                {
                    BattleObject.Context.MapGrid.RemoveOccupied(oldP);
                }
            }
            catch (System.Exception ex)
            {
                "SpawnRosterUnitsOntoGrid: Failed during RemoveOccupied cleanup: ".LogWarning();
                ex.Message.LogWarning();
            }

            try
            {
                var newMgp = BattleObject.Context.MapGrid.GetGridPoint(
                    spawnPosition.x,
                    spawnPosition.y
                );
                if (newMgp != null)
                {
                    BattleObject.Context.MapGrid.SetOccupied(newMgp, characterInstance);
                }
                else
                {
                    "SpawnRosterUnitsOntoGrid: Failed to find MapGridPoint for placement during repair.".LogError();
                }
            }
            catch (System.Exception ex)
            {
                "SpawnRosterUnitsOntoGrid: Failed to align spawn position: ".LogError();
                ex.Message.LogError();
            }
#else
            $"SpawnRosterUnitsOntoGrid: Detected MapGridPosition mismatch for {characterInstance.CharacterTemplate.DisplayName} from {characterInstance.MapGridPosition} to {spawnPosition}; skipping repair in release build.".LogWarning();
#endif
        }

        /// <summary>
        /// Ensures a roster has a <see cref="CharacterInstance"/> for the given data.
        /// If missing, creates or recalls one via <see cref="CharacterFactory"/> and adds it to the roster.
        /// Generic so the same implementation serves both player-team and generic enemy rosters.
        /// </summary>
        private CharacterInstance EnsureInstanceForPlacement<T>(
            RosterInstance<T> rosterInstance,
            CharacterData data
        )
            where T : Turnroot.Characters.Roster
        {
            if (data == null || rosterInstance == null)
            {
                return null;
            }

            var inst = rosterInstance.GetInstanceFor(data);
            if (inst != null)
            {
                return inst;
            }

            try
            {
                var factory = new CharacterFactory(Brain.ltm);
                var created = factory.CreateOrRecall(data);
                if (created != null)
                {
                    rosterInstance.AddInstance(created);
                    $"EnsureInstanceForPlacement: Created instance for {data.DisplayName}".LogInfo();
                    return created;
                }
            }
            catch (System.Exception ex)
            {
                $"EnsureInstanceForPlacement: Failed to create instance for {data?.DisplayName}: {ex.Message}".LogWarning();
            }

            return null;
        }
    }
}
