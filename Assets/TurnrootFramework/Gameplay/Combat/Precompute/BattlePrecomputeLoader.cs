using System.Collections;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Maps;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.Precompute
{
    /// <summary>
    /// Precomputes expensive battle startup data during the PreBattleTransitionToBattle
    /// brain state and reports progress to the scene's LoadingController so the
    /// DynamicSceneFlow advances only after precompute tasks complete.
    /// </summary>
    public class BattlePrecomputeLoader : MonoBehaviour
    {
        private Brain.Brain _brain;
        private LoadingController _loadingController;

        /// <summary>
        /// Initialize the loader with required dependencies. Call this from the owner
        /// (for example, StateBrain) instead of relying on FindObjectOfType.
        /// </summary>
        public OperationResult Initialize(Brain.Brain brain)
        {
            if (brain == null)
            {
                return OperationResult.Failure("BattlePrecomputeLoader.Initialize: brain is null");
            }

            _brain = brain;
            // Prefer the LoadingController attached to the Brain for centralized tracking
            _loadingController = brain.GetComponent<LoadingController>();
            _brain.OnStateChanged += HandleStateChanged;
            return OperationResult.Successful();
        }

        private void OnDestroy()
        {
            if (_brain != null)
            {
                _brain.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(BrainState newState)
        {
            if (newState == null)
            {
                return;
            }

            // Target the full pre-battle transition child state
            if (
                newState.Name == BrainStateNames.PreBattleTransitionToBattle
                && newState.Parent != null
                && newState.Parent.Name == BrainStateNames.Combat
            )
            {
                StartCoroutine(RunPrecomputeTasks());
            }
        }

        private IEnumerator RunPrecomputeTasks()
        {
            // Determine if we have a loading controller to report progress to
            bool haveLoader = _loadingController != null;

            var battleBrain = _brain?.battleBrain;
            var context = battleBrain?.BattleObject?.Context;

            if (context == null || context.Participants == null)
            {
                // Nothing to precompute; still advance the flow
                if (haveLoader)
                {
                    _loadingController.Clear();
                    _loadingController.IncreaseLoadTotalBy(1);
                    _loadingController.IncrementLoadedAmountBy(1);
                }
                yield break;
            }

            var units = context.Participants.GetAllUnits();
            if (units == null || units.Count == 0)
            {
                if (haveLoader)
                {
                    _loadingController.Clear();
                    _loadingController.IncreaseLoadTotalBy(1);
                    _loadingController.IncrementLoadedAmountBy(1);
                }
                yield break;
            }

            // Build task list: for each unit we will initialize AI helper and compute tiles
            int tasks = 0;
            foreach (var u in units)
            {
                if (u != null)
                {
                    tasks += 2; // AI init + tile compute
                }
            }

            if (tasks == 0)
            {
                if (haveLoader)
                {
                    _loadingController.Clear();
                    _loadingController.IncreaseLoadTotalBy(1);
                    _loadingController.IncrementLoadedAmountBy(1);
                }
                yield break;
            }

            // Configure loading controller
            if (haveLoader)
            {
                _loadingController.Clear();
                _loadingController.IncreaseLoadTotalBy(tasks);
            }

            // Precompute movement-cost caches for common movement modes to avoid repeated terrain-cost lookups.
            var map = context.mapGrid;
            if (map != null)
            {
                var modes = new (bool w, bool f, bool r, bool m, bool a)[]
                {
                    (true, false, false, false, false), // walking/infantry
                    (false, true, false, false, false), // flying
                    (false, false, true, false, false), // riding
                    (false, false, false, true, false), // magic
                    (false, false, false, false, true), // armored
                };

                foreach (var mode in modes)
                {
                    var key = MapGrid.MakeMovementModeKey(mode.w, mode.f, mode.r, mode.m, mode.a);
                    var res = map.BuildMovementCostCache(
                        key,
                        mode.w,
                        mode.f,
                        mode.r,
                        mode.m,
                        mode.a
                    );
                    if (!res.Success)
                    {
                        TurnrootLogger.Log(
                            $"BattlePrecomputeLoader: Failed to build movement cost cache for mode {key}: {res.ErrorMessage}",
                            TurnrootLogger.LogLevel.Warning
                        );
                    }
                }
            }

            // Iterate units and perform work incrementally to avoid frame freeze
            foreach (var unit in units)
            {
                if (unit == null)
                {
                    continue;
                }

                // 1) Initialize AI helper for unit (may be a light-weight setup)
                if (context.AIHelper == null)
                {
                    TurnrootLogger.Log(
                        "BattlePrecomputeLoader: AIHelper is null, skipping AI initialization",
                        TurnrootLogger.LogLevel.Warning
                    );
                }
                else
                {
                    var initResult = context.AIHelper.InitializeAIControlledUnit(unit);
                    if (!initResult.Success)
                    {
                        TurnrootLogger.Log(
                            $"BattlePrecomputeLoader: AI init failed for unit {unit?.Id}: {initResult.ErrorMessage}",
                            TurnrootLogger.LogLevel.Warning
                        );
                    }
                }

                if (haveLoader)
                {
                    _loadingController.IncrementLoadedAmountBy(1);
                }

                // Allow a frame to update UI
                yield return null;

                // 2) Precompute tiles (force recompute)
                var tilesOk = context.TryGetValidTilesForUnit(
                    unit,
                    out var move,
                    out var attack,
                    forceRecompute: true
                );
                if (!tilesOk)
                {
                    TurnrootLogger.Log(
                        $"BattlePrecomputeLoader: Tile precompute failed for unit {unit?.Id}",
                        TurnrootLogger.LogLevel.Warning
                    );
                }

                if (haveLoader)
                {
                    _loadingController.IncrementLoadedAmountBy(1);
                }

                // Allow a frame to update UI
                yield return null;
            }

            // All tasks completed; LoadingController will call DynamicSceneFlow.Progress() when loaded amount reaches total
        }
    }
}
