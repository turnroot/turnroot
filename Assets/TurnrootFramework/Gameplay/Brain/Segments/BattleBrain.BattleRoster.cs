using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components.Battle;
using Turnroot.Gameplay.Combat;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.Maps;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Turnroot.Gameplay.Brain
{
    public partial class BattleBrain : BrainComponent
    {
        #region Battle Roster Initialization

        private OperationResult InitializeBattleRosters()
        {
            BattleObject.InitializeBattleRosters();

            // CRITICAL: Ensure units are selected for battle BEFORE creating battle copies
            // This handles cases where battle is started without going through pre-battle UI
            EnsureUnitsSelectedForBattle();

            var result = BattleObject.PopulateBattleRostersFromTemplates();
            if (!result.Success)
            {
                return result;
            }

            // CRITICAL: Set positions BEFORE populating context, so participants have valid positions
            ApplyPlacementsToBattle();

            var populateResult = PopulateBattleContextParticipants();
            if (!populateResult.Success)
            {
                $"Failed to populate battle context: {populateResult.ErrorMessage}".LogError();
                return populateResult;
            }

            SpawnRosterUnitsOntoGrid();

            _aiHelper = new BattleContextAIHelper(BattleObject.Context);
            return OperationResult.Successful();
        }

        /// <summary>
        /// Ensures at least some units are selected for battle before creating battle copies.
        /// Called automatically if battle is started without pre-battle UI.
        /// </summary>
        private void EnsureUnitsSelectedForBattle()
        {
            var gw = Brain.gamewideContextBrain;
            var prep = PreparationObject;

            if (gw == null || prep == null)
            {
                "BattleBrain: Cannot ensure units selected - missing gamewideContextBrain or PreparationObject".LogError();
                return;
            }

            var persistentRoster =
                gw.GamewidePersistentPlayerRoster
                ?? gw.CreateOrRecallGamewidePersistentPlayerRoster();
            if (persistentRoster == null)
            {
                "BattleBrain: Cannot ensure units selected - no persistent roster available".LogError();
                return;
            }

            var rosterInstance = gw.GetOrCreatePlayerTeamRoster(persistentRoster);
            if (rosterInstance == null)
            {
                $"BattleBrain: Cannot ensure units selected - failed to get roster instance for '{persistentRoster.name}'".LogError();
                return;
            }

            if (rosterInstance.Instances == null || rosterInstance.Instances.Count == 0)
            {
                $"BattleBrain: Cannot ensure units selected - roster '{persistentRoster.name}' has no character instances. Make sure the roster template has characters assigned.".LogError();
                return;
            }

            // Check if any units are already selected
            var selectedUnits = gw.GetSelectedForBattlePlayerTeamUnits();
            if (selectedUnits != null && selectedUnits.Count > 0)
            {
                // Units already selected, no need to auto-select
                $"BattleBrain: {selectedUnits.Count} units already selected for battle".LogInfo();
                return;
            }

            // Auto-select default units (required units + auto-fill from roster)
            $"BattleBrain: Auto-selecting default units from roster with {rosterInstance.Instances.Count} available characters".LogInfo();
            PreBattleSelectionHelper.EnsureDefaultPreBattleSelections(
                Brain,
                persistentRoster,
                rosterInstance,
                prep.MaxPlayerTeamUnits,
                prep.RequiredPlayerUnits
            );

            // Initialize placements so ApplyPlacementsToBattle has data
            prep.InitializePlacements();
        }

        /// <summary>
        /// Reads placements from BattlePreparationObject and writes positions to CharacterInstance.MapGridPosition.
        /// This is the single authoritative transfer from Starting Positions phase to Battle phase.
        /// </summary>
        private void ApplyPlacementsToBattle()
        {
            var prep = PreparationObject;
            var playerRoster = BattleObject?.PlayerTeamRoster;

            if (playerRoster == null)
            {
                "ApplyPlacementsToBattle: No player roster".LogWarning();
                return;
            }

            // If we have placements from the pre-battle UI, use them
            if (prep?.placements != null && prep.placements.Count > 0)
            {
                $"ApplyPlacementsToBattle: Applying {prep.placements.Count} placements from prep object".LogInfo();

                foreach (var kvp in prep.placements)
                {
                    var pos = kvp.Key;
                    var data = kvp.Value;
                    if (data == null)
                    {
                        $"ApplyPlacementsToBattle: Null CharacterData at position {pos}".LogWarning();
                        continue;
                    }

                    var inst =
                        playerRoster.GetInstanceFor(data)
                        ?? Brain.gamewideContextBrain?.FindInstanceByTemplate(data);

                    if (inst == null)
                    {
                        $"ApplyPlacementsToBattle: No instance for {data.name} at {pos}".LogWarning();
                        continue;
                    }

                    inst.MapGridPosition = pos;
                }
                return;
            }

            // Fallback: No placements, position units at spawn points directly
            "ApplyPlacementsToBattle: No placements found, using spawn points fallback".LogWarning();

            var spawnPoints = BattleObject?.MapGrid?.PlayerTeamSpawnPoints;
            if (spawnPoints == null || spawnPoints.Count == 0)
            {
                "ApplyPlacementsToBattle: No spawn points available".LogError();
                return;
            }

            var instances = playerRoster.Instances;
            if (instances == null || instances.Count == 0)
            {
                "ApplyPlacementsToBattle: No instances in player roster".LogWarning();
                return;
            }

            $"ApplyPlacementsToBattle: Positioning {instances.Count} units at {spawnPoints.Count} spawn points".LogInfo();

            var spawnIndex = 0;
            foreach (var inst in instances)
            {
                if (inst == null)
                {
                    continue;
                }

                if (spawnIndex >= spawnPoints.Count)
                {
                    $"ApplyPlacementsToBattle: More units ({instances.Count}) than spawn points ({spawnPoints.Count})".LogWarning();
                    break;
                }

                var pos = spawnPoints[spawnIndex];
                var oldPos = inst.MapGridPosition;
                inst.MapGridPosition = pos;
                var newPos = inst.MapGridPosition;
                spawnIndex++;
            }
        }

        private OperationResult PopulateBattleContextParticipants()
        {
            var validation = OperationResultGuards.All(
                OperationResultGuards.RequireNotNull(BattleObject, nameof(BattleObject)),
                OperationResultGuards.RequireNotNull(BattleObject?.Context, "BattleContext")
            );
            if (!validation.Success)
            {
                return validation;
            }

            var context = BattleObject.Context;

            context.Participants.Enemies.Clear();
            context.Participants.Allies.Clear();
            context.Participants.ThirdParty.Clear();
            context.ClearParticipantDynamicData();

            foreach (var unit in PlayerTeamRoster.Instances)
            {
                if (!unit.IsDefeatedInCurrentBattle)
                {
                    $"PopulateBattleContextParticipants: Adding {unit.CharacterTemplate?.DisplayName} at position {unit.MapGridPosition} (id={unit.Id})".LogInfo();
                    context.Participants.Allies.Add(unit);
                }
            }

            if (BattleObject.HasThirdParty)
            {
                foreach (var unit in ThirdPartyTeamRoster.Instances)
                {
                    if (!unit.IsDefeatedInCurrentBattle)
                    {
                        context.Participants.ThirdParty.Add(unit);
                    }
                }
            }

            return OperationResult.Successful();
        }

        #endregion
    }
}