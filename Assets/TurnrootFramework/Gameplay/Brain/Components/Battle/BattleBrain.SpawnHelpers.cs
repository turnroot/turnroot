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
                    TurnrootLogger.Log(
                        $"SpawnRosterUnitsOntoGrid: No instance for {characterData?.DisplayName}; skipping spawn",
                        TurnrootLogger.LogLevel.Warning
                    );
                    roster.SetOrder(characterData, placement.Order);
                    continue;
                }

                if (instance.MapGridPosition == placement.SpawnPosition)
                {
                    TurnrootLogger.Log(
                        $"SpawnRosterUnitsOntoGrid: Skipping spawn for {instance.CharacterTemplate.DisplayName} - already spawned at {placement.SpawnPosition}",
                        TurnrootLogger.LogLevel.Info
                    );
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
                    TurnrootLogger.Log(
                        $"SpawnRosterUnitsOntoGrid: SpawnAtPosition failed for {characterData?.DisplayName} at {placement.SpawnPosition}",
                        TurnrootLogger.LogLevel.Warning
                    );
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
                    TurnrootLogger.Log(
                        $"SpawnRosterUnitsOntoGrid: No instance for {characterData?.DisplayName}; skipping spawn",
                        TurnrootLogger.LogLevel.Warning
                    );
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
                    TurnrootLogger.Log(
                        $"SpawnRosterUnitsOntoGrid: SpawnAtPosition failed for {characterData?.DisplayName} at {placement.SpawnPosition}",
                        TurnrootLogger.LogLevel.Warning
                    );
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
            TurnrootLogger.Log(
                $"SpawnRosterUnitsOntoGrid: Repairing {characterInstance.CharacterTemplate.DisplayName} MapGridPosition from {characterInstance.MapGridPosition} to {spawnPosition}",
                TurnrootLogger.LogLevel.Warning
            );

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
                TurnrootLogger.Log(
                    "SpawnRosterUnitsOntoGrid: Failed during RemoveOccupied cleanup: " + ex.Message,
                    TurnrootLogger.LogLevel.Warning
                );
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
                    TurnrootLogger.Log(
                        "SpawnRosterUnitsOntoGrid: Failed to find MapGridPoint for placement during repair.",
                        TurnrootLogger.LogLevel.Error
                    );
                }
            }
            catch (System.Exception ex)
            {
                TurnrootLogger.Log(
                    "SpawnRosterUnitsOntoGrid: Failed to align spawn position: " + ex.Message,
                    TurnrootLogger.LogLevel.Error
                );
            }
#else
            TurnrootLogger.Log(
                $"SpawnRosterUnitsOntoGrid: Detected MapGridPosition mismatch for {characterInstance.CharacterTemplate.DisplayName} from {characterInstance.MapGridPosition} to {spawnPosition}; skipping repair in release build.",
                TurnrootLogger.LogLevel.Warning
            );
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
                TurnrootLogger.Log(
                    "SpawnRosterUnitsOntoGrid: Unexpected error during spawn pass: " + ex.Message,
                    TurnrootLogger.LogLevel.Warning
                );
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
                    TurnrootLogger.Log(
                        $"EnsureInstanceForPlacement: Created instance for {data?.DisplayName}",
                        TurnrootLogger.LogLevel.Info
                    );
                    return created;
                }
            }
            catch (System.Exception ex)
            {
                TurnrootLogger.Log(
                    $"EnsureInstanceForPlacement: Failed to create instance for {data?.DisplayName}: {ex.Message}",
                    TurnrootLogger.LogLevel.Warning
                );
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
                    TurnrootLogger.Log(
                        $"EnsureInstanceForPlacement: Created instance for {data?.DisplayName}",
                        TurnrootLogger.LogLevel.Info
                    );
                    return created;
                }
            }
            catch (System.Exception ex)
            {
                TurnrootLogger.Log(
                    $"EnsureInstanceForPlacement: Failed to create instance for {data?.DisplayName}: {ex.Message}",
                    TurnrootLogger.LogLevel.Warning
                );
            }

            return null;
        }
    }
}
