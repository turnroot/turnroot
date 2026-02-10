using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.PreBattle
{
    public partial class BattlePreparationObject
    {
        private (
            bool hasSelection,
            System.Collections.Generic.List<CharacterInstance> finalSelected,
            OperationResult failure
        ) ComputeFinalSelectedUnits(
            GamewideContextBrain gw,
            dynamic persistent,
            dynamic runtimeInstance,
            BattlePreparationObject prep
        )
        {
            var selectedUnits = gw?.GetSelectedForBattlePlayerTeamUnits();

            // If pre-battle prep exists, prefer its per-battle selections (these do not mutate persistent roster)
            if (prep != null)
            {
                var prepSelected = prep.GetBattleSelectedInstances();
                if (prepSelected != null && prepSelected.Count > 0)
                {
                    selectedUnits = prepSelected;
                }
            }

            // If still no selections, attempt to compute default selections from roster/templates.
            if (selectedUnits == null || selectedUnits.Count == 0)
            {
                var selectedTemplates = PreBattleSelectionHelper.EnsureDefaultPreBattleSelections(
                    Brain,
                    persistent,
                    runtimeInstance,
                    MaxPlayerTeamUnits,
                    RequiredPlayerUnits
                );

                if (selectedTemplates != null && selectedTemplates.Count > 0)
                {
                    // Build selected units from templates by finding runtime instances.
                    var tempList = new List<CharacterInstance>();
                    var placementsArr =
                        runtimeInstance != null
                            ? runtimeInstance.GetPlacements()
                            : persistent?.characters ?? new Characters.Roster.UnitPlacement[0];
                    foreach (var p in placementsArr)
                    {
                        if (p == null || p.CharacterData == null)
                        {
                            continue;
                        }

                        if (selectedTemplates.Contains(p.CharacterData))
                        {
                            var inst =
                                runtimeInstance != null
                                    ? runtimeInstance.GetInstanceFor(p.CharacterData)
                                    : null;
                            inst ??= gw?.FindInstanceByTemplate(p.CharacterData);
                            if (inst != null)
                            {
                                tempList.Add(inst);
                            }
                        }
                    }

                    selectedUnits = tempList;
                }
            }

            if (selectedUnits == null || selectedUnits.Count == 0)
            {
                // No selected units — clear any existing placements and ensure UI is updated.
                placements = new Dictionary<Vector2Int, CharacterInstance>();
                StartingPositionsComponent?.DespawnAllModels();
                CurrentPlacementState = PlacementState.NonePlaced;
                Brain?.PublishPlacementsInitialized();
                return (false, null, OperationResult.Failure("No units available for positioning"));
            }

            // If a runtime roster exists, prefer runtime instance selection flags
            var runtimeSelected = new System.Collections.Generic.List<CharacterInstance>();
            var runtimeInst = runtimeInstance;
            if (runtimeInst != null)
            {
                foreach (var inst in runtimeInst.Instances)
                {
                    if (inst != null && inst.IsSelectedForBattle)
                    {
                        runtimeSelected.Add(inst);
                    }
                }
            }

            TurnrootLogger.Log(
                $"InitializePlacements: runtimeSelected.Count={runtimeSelected.Count}, selectedUnits.Count={selectedUnits?.Count ?? 0}",
                TurnrootLogger.LogLevel.Info
            );

            if (runtimeSelected.Count == 0 && selectedUnits != null && selectedUnits.Count > 0)
            {
                TurnrootLogger.Log(
                    "InitializePlacements: SelectedUnits from selection helper:",
                    TurnrootLogger.LogLevel.Info
                );
                foreach (var s in selectedUnits)
                {
                    TurnrootLogger.Log(
                        $"  - {s?.CharacterTemplate?.DisplayName ?? "<null>"}",
                        TurnrootLogger.LogLevel.Info
                    );
                }
            }

            // If the player modified selections during this pre-battle session, honor those per-battle
            // selections and do NOT let the runtime roster's IsSelectedForBattle flags override them.
            System.Collections.Generic.List<CharacterInstance> finalSelected;
            if (!_battleSelectionsChanged && runtimeSelected.Count > 0)
            {
                finalSelected = runtimeSelected;
            }
            else
            {
                finalSelected = selectedUnits;
            }

            return (true, finalSelected, OperationResult.Successful());
        }
    }
}
