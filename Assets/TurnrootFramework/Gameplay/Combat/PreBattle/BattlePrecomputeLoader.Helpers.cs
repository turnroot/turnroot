using System.Collections;
using System.Linq;
using Turnroot.Gameplay.Brain;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.Precompute
{
    public partial class BattlePrecomputeLoader
    {
        #region Helper Methods

        private IEnumerator EnsureLtmUnitsAreUsedRoutine(
            FundamentalComponents.Battles.BattleContext context
        )
        {
            if (context == null || _brain.gamewideContextBrain == null)
            {
                yield break;
            }

            var gw = _brain.gamewideContextBrain;

            var lists = new[]
            {
                context.Participants.Allies,
                context.Participants.Targets,
                context.Participants.ThirdParty,
            };

            foreach (var list in lists)
            {
                if (list == null)
                {
                    continue;
                }

                for (int i = 0; i < list.Count; i++)
                {
                    var unit = list[i];
                    if (unit == null)
                    {
                        continue;
                    }

                    // Skip units that were not spawned for this battle (only precompute selected units)
                    if (!unit.WasSpawnedDuringBattle)
                    {
                        continue;
                    }

                    var template = unit.CharacterTemplate;
                    if (template != null && template.IsUnique)
                    {
                        var recalled = gw.RecallCharacter(template);
                        if (recalled != null && !ReferenceEquals(recalled, unit))
                        {
                            // Copy position from old unit to recalled unit
                            recalled.MapGridPosition = unit.MapGridPosition;
                            recalled.WasSpawnedDuringBattle = unit.WasSpawnedDuringBattle;

                            // Update MapGrid to reference the new unit
                            var gridPoint = context.MapGrid.GetGridPoint(
                                unit.MapGridPosition.x,
                                unit.MapGridPosition.y
                            );
                            if (gridPoint != null)
                            {
                                gridPoint.CurrentInstance = recalled;
                            }

                            list[i] = recalled;

                            // If there is a BattlePreparationObject, update its placements to reference
                            // the recalled instance so prebattle/battle references stay consistent.
                            var prep = _brain.battleBrain.PreparationObject;
                            if (prep != null && prep.placements != null)
                            {
                                var keysToUpdate = prep
                                    .placements.Where(kvp =>
                                        kvp.Value != null && kvp.Value == template
                                    )
                                    .Select(kvp => kvp.Key)
                                    .ToList();
                                foreach (var k in keysToUpdate)
                                {
                                    // placements now store CharacterData; ensure they reference the recalled template (no change)
                                    if (prep != null && prep.PlacementsLocked)
                                    {
                                        "BattlePrecomputeLoader: Placements are locked for battle initialization; skipping placement update for recalled unit.".LogWarning();
                                        continue;
                                    }

                                    prep.placements[k] = recalled.CharacterTemplate;
                                }
                                try
                                {
                                    // Do not persist during precompute; final persisted placement should occur during roster initialization.
                                    _brain?.PublishPlacementsSyncRequested(
                                        persist: false,
                                        forceApplyPlacementsOnLoad: false
                                    );
                                }
                                catch (System.Exception ex)
                                {
                                    "BattlePrecomputeLoader: PublishPlacementsSyncRequested failed: ".LogWarning();
                                    ex.Message.LogWarning();
                                }
                            }
                        }

                        var currentAfterRecall = list[i];
                        if (currentAfterRecall != null && currentAfterRecall.NeedsPersist)
                        {
                            gw.PersistIfNeeded(currentAfterRecall, updateIndex: false);
                        }
                    }

                    // After possible replacement, ensure unit has a class assigned. If not, assign
                    // the character template's starting class if present, otherwise use the game's
                    // default starting class
                    var current = list[i];
                    if (current != null && current.CurrentClass == null)
                    {
                        var classToApply =
                            current.CharacterTemplate?.GetPreferredStartingClass()
                            ?? GameplayGeneralSettings.Instance?.GetDefaultStartingClass();

                        if (classToApply != null)
                        {
                            var classRes = current.ChangeClass(
                                classToApply,
                                applyClassChangeBonuses: false
                            );
                            if (!classRes.Success)
                            {
                                $"BattlePrecomputeLoader: Failed to assign default class for unit {current.Id}: {classRes.ErrorMessage}".LogWarning();
                            }
                            else
                            {
                                current.NeedsPersist = true;
                                gw.PersistIfNeeded(current, updateIndex: false);
                            }
                        }
                    }

                    IncrementProgress();
                    yield return new WaitForSeconds(timeBetweenOperations);
                }
            }
        }

        private FundamentalComponents.Battles.BattleContext GetBattleContext() =>
            _battleContext ?? _brain?.battleBrain?.BattleObject?.Context;

        private bool IsContextValid(FundamentalComponents.Battles.BattleContext context)
        {
            return context != null
                && context.MapGrid != null
                && context.Participants?.GetAllUnits()?.Count > 0;
        }

        // Returns true when a PlayerTeamRosterInstance exists and has populated CharacterInstance
        // objects whose class metadata is valid for precompute consumption.

        private int CalculateTaskCount(
            System.Collections.Generic.List<Characters.CharacterInstance> units,
            UnitAppearanceBrain appearanceBrain
        )
        {
            if (units == null)
            {
                return 0;
            }

            // Tasks per unit:
            // 1) LTM recall/check
            // 2) AI init
            // 3) Pathfinding params
            // 4) Tiles computation
            // 5) Model spawn
            // 6) Weapon & inventory summary (cached for AI evaluations)
            int tasksPerUnit = 6;
            return units.Count * tasksPerUnit;
        }

        private void InitializeLoadingProgress(int taskCount)
        {
            if (_loadingController == null)
            {
                return;
            }

            _loadingController.Clear();
            _loadingController.IncreaseLoadTotalBy(taskCount);
        }

        private void IncrementProgress() => _loadingController?.IncrementLoadedAmountBy(1);

        private void CompleteWithMinimalProgress()
        {
            if (_loadingController == null)
            {
                return;
            }

            _loadingController.Clear();
            _loadingController.IncreaseLoadTotalBy(1);
            _loadingController.IncrementLoadedAmountBy(1);

            _brain.PublishPrecomputeCompleted();
        }
        #endregion
    }
}
