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

            // Process each roster in a single pass using shared helpers to keep this method small and DRY.
            if (playerTeamRoster != null)
            {
                SpawnAndOrderRosterPlacements(playerTeamRoster);
            }

            if (thirdPartyRoster != null)
            {
                SpawnAndOrderRosterPlacements(thirdPartyRoster);
            }

            // Final verification for player roster only (keeps original intent of a post-check pass).
            VerifyAndRepairPlayerPlacements(playerTeamRoster);

            BattleObject.Context.InvalidateUnitPositionCache();
        }

        // Spawn each placement for a roster and set ordering. Handles instance creation and basic mismatch repair.
        private void SpawnAndOrderRosterPlacements(RosterInstance<GenericRoster> roster)
        {
            if (roster == null)
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

                if (instance.MapGridPosition == placement.SpawnPosition)
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

        private void SpawnAndOrderRosterPlacements(PlayerTeamRosterInstance roster)
        {
            if (roster == null)
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

                if (instance.MapGridPosition == placement.SpawnPosition)
                {
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

        private void VerifyAndRepairPlayerPlacements(PlayerTeamRosterInstance playerTeamRoster)
        {
            if (playerTeamRoster == null)
            {
                return;
            }

            try
            {
                foreach (var ap in playerTeamRoster.GetPlacements())
                {
                    var inst = playerTeamRoster.GetInstanceFor(ap.CharacterData);
                    if (inst == null)
                    {
                        continue;
                    }

                    if (inst.MapGridPosition != ap.SpawnPosition)
                    {
                        TryRepairPlacementIfMismatch(inst, ap.SpawnPosition);
                    }
                }
            }
            catch (System.Exception ex)
            {
                "SpawnRosterUnitsOntoGrid: Unexpected error during spawn pass: ".LogWarning();
                ex.Message.LogWarning();
            }
        }

        // Helper: ensure a roster has an instance for the given CharacterData. If missing,
        // create or recall an instance and add it to the roster. Returns the instance or null.
        private CharacterInstance EnsureInstanceForPlacement(
            RosterInstance<GenericRoster> rosterInstance,
            CharacterData data
        )
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
                    $"EnsureInstanceForPlacement: Created instance for {data?.DisplayName}".LogInfo();
                    return created;
                }
            }
            catch (System.Exception ex)
            {
                $"EnsureInstanceForPlacement: Failed to create instance for {data?.DisplayName}: {ex.Message}".LogWarning();
            }

            return null;
        }

        private CharacterInstance EnsureInstanceForPlacement(
            PlayerTeamRosterInstance rosterInstance,
            CharacterData data
        )
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
                    $"EnsureInstanceForPlacement: Created instance for {data?.DisplayName}".LogInfo();
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
