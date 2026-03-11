using System.Collections;
using Turnroot.Gameplay.Brain;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.Precompute
{
    public partial class BattlePrecomputeLoader
    {
        #region Helper Methods

        private IEnumerator EnsureUnitsHaveClassesRoutine(
            FundamentalComponents.Battles.BattleContext context
        )
        {
            if (context == null)
            {
                yield break;
            }

            var gw = _brain?.gamewideContextBrain;

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

                foreach (var unit in list)
                {
                    if (unit == null || !unit.WasSpawnedDuringBattle)
                    {
                        continue;
                    }

                    // If the unit has no class assigned, apply the starting class.
                    if (unit.CurrentClass == null)
                    {
                        var classToApply =
                            unit.CharacterTemplate?.GetPreferredStartingClass()
                            ?? GameplayGeneralSettings.Instance?.GetDefaultStartingClass();

                        if (classToApply != null)
                        {
                            var classRes = unit.ChangeClass(
                                classToApply,
                                applyClassChangeBonuses: false
                            );
                            if (!classRes.Success)
                            {
                                $"BattlePrecomputeLoader: Failed to assign default class for unit {unit.Id}: {classRes.ErrorMessage}".LogWarning();
                            }
                            else if (gw != null)
                            {
                                unit.NeedsPersist = true;
                                gw.PersistIfNeeded(unit, updateIndex: false);
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
            // 1) AI init
            // 2) Pathfinding params
            // 3) Weapon & inventory summary (cached for AI evaluations)
            // 4) Tiles computation
            // NOTE: Model spawning is NOT a precompute task - models are spawned by SpawnRosterUnitsOntoGrid
            int tasksPerUnit = 4;
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
