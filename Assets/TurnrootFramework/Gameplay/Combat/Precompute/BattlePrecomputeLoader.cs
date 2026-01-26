using System.Collections;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Maps;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.Precompute
{
    /// <summary>
    /// Precomputes expensive battle startup data during the PreBattleTransitionToBattle
    /// brain state and reports progress to the scene's LoadingController.
    /// </summary>
    public class BattlePrecomputeLoader : MonoBehaviour
    {
        private Brain.Brain _brain;
        private LoadingController _loadingController;
        private FundamentalComponents.Battles.BattleContext _battleContext;
        private bool _initialized = false;
        private bool _precomputeStarted = false;
        private bool _forceStartRetryScheduled = false;

        // Control frame pacing for smooth loading bar progression
        [SerializeField]
        private float timeBetweenOperations = 0.15f;

        #region Initialization
        /// <summary>
        /// Initialize the loader with required dependencies. Call this from the owner
        /// (for example, StateBrain) instead of relying on FindObjectOfType.
        /// </summary>
        public OperationResult Initialize(
            Brain.Brain brain,
            FundamentalComponents.Battles.BattleContext context = null
        )
        {
            if (brain == null)
            {
                return OperationResult.Failure("BattlePrecomputeLoader.Initialize: brain is null");
            }

            _brain = brain;
            _loadingController = brain.GetComponent<LoadingController>();
            _battleContext = context ?? _battleContext;
            _initialized = true;

            return OperationResult.Successful();
        }

        private void Start()
        {
            if (_initialized)
            {
                return;
            }

            // Attempt auto-initialize using the scene Brain if available
            var brain = FindFirstObjectByType<Brain.Brain>();
            if (brain != null)
            {
                var res = Initialize(brain);
                if (!res.Success)
                {
                    TurnrootLogger.Log(
                        $"BattlePrecomputeLoader: Auto-initialize failed: {res.ErrorMessage}",
                        TurnrootLogger.LogLevel.Warning
                    );
                }
            }
        }

        private void OnDestroy() => _brain = null;
        #endregion

        #region Precompute Control
        /// <summary>
        /// Attempts to start precompute if battle context is ready.
        /// If not ready, schedules a retry on the next frame.
        /// </summary>
        public void ForceStartPrecomputeIfPossible()
        {
            if (_precomputeStarted)
            {
                return;
            }

            var context = GetBattleContext();
            if (!IsContextValid(context))
            {
                if (!_forceStartRetryScheduled)
                {
                    StartCoroutine(RetryForceStartNextFrame());
                }
                return;
            }

            StartCoroutine(RunPrecomputeTasks());
        }

        private IEnumerator RetryForceStartNextFrame()
        {
            _forceStartRetryScheduled = true;
            yield return null;
            _forceStartRetryScheduled = false;

            if (_precomputeStarted)
            {
                yield break;
            }

            var context = GetBattleContext();
            if (IsContextValid(context))
            {
                StartCoroutine(RunPrecomputeTasks());
            }
            else
            {
                TurnrootLogger.Log(
                    "BattlePrecomputeLoader: Retry failed, context still invalid",
                    TurnrootLogger.LogLevel.Warning
                );
            }
        }

        public void ResetPrecomputeFlag() => _precomputeStarted = false;
        #endregion

        #region Precompute Tasks
        private IEnumerator RunPrecomputeTasks()
        {
            if (_precomputeStarted)
            {
                yield break;
            }

            _precomputeStarted = true;

            var context = GetBattleContext();
            var units = context?.Participants?.GetAllUnits();

            if (!IsContextValid(context))
            {
                TurnrootLogger.Log(
                    "BattlePrecomputeLoader: Invalid context, completing with minimal progress",
                    TurnrootLogger.LogLevel.Warning
                );

                CompleteWithMinimalProgress();
                yield break;
            }

            var appearanceBrain = _brain?.unitAppearanceBrain;
            int taskCount = CalculateTaskCount(units, appearanceBrain);

            if (taskCount == 0)
            {
                CompleteWithMinimalProgress();
                yield break;
            }

            InitializeLoadingProgress(taskCount);

            // Precompute movement caches with delays between each
            yield return PrecomputeMovementCaches(context.mapGrid);

            // Process each unit with visible delays for smooth loading bar
            foreach (var unit in units)
            {
                if (unit == null)
                {
                    continue;
                }

                yield return ProcessUnit(unit, context, appearanceBrain);
            }

            yield return new WaitForSeconds(timeBetweenOperations);
        }

        private IEnumerator ProcessUnit(
            Characters.CharacterInstance unit,
            FundamentalComponents.Battles.BattleContext context,
            UnitAppearanceBrain appearanceBrain
        )
        {
            // 1) Initialize AI helper for unit
            if (context.AIHelper != null)
            {
                var initResult = context.AIHelper.InitializeAIControlledUnit(unit);
                if (!initResult.Success)
                {
                    TurnrootLogger.Log(
                        $"BattlePrecomputeLoader: AI init failed for unit {unit.Id}: {initResult.ErrorMessage}",
                        TurnrootLogger.LogLevel.Warning
                    );
                }
            }
            IncrementProgress();
            yield return new WaitForSeconds(timeBetweenOperations);

            // 2) Precompute pathfinding parameters
            var paramsOk = context.PrecomputePathfindingParameters(unit);
            if (!paramsOk)
            {
                TurnrootLogger.Log(
                    $"BattlePrecomputeLoader: Pathfinding params failed for unit {unit.Id}",
                    TurnrootLogger.LogLevel.Warning
                );
            }
            IncrementProgress();
            yield return new WaitForSeconds(timeBetweenOperations);

            // 3) Spawn model (usually most expensive operation)
            if (appearanceBrain != null)
            {
                var spawnResult = appearanceBrain.PrecomputeSpawnModelAt(
                    unit,
                    unit.MapGridPosition,
                    prebattle: false
                );

                if (!spawnResult.Success)
                {
                    TurnrootLogger.Log(
                        $"BattlePrecomputeLoader: Model spawn failed for unit {unit.Id}: {spawnResult.ErrorMessage}",
                        TurnrootLogger.LogLevel.Warning
                    );
                }
                IncrementProgress();
                yield return new WaitForSeconds(timeBetweenOperations);
            }

            // 4) Precompute valid tiles (can be expensive for large maps)
            var tilesOk = context.TryGetValidTilesForUnit(unit, out _, out _, forceRecompute: true);

            if (!tilesOk)
            {
                TurnrootLogger.Log(
                    $"BattlePrecomputeLoader: Tile computation failed for unit {unit.Id}",
                    TurnrootLogger.LogLevel.Warning
                );
            }
            IncrementProgress();
            yield return new WaitForSeconds(timeBetweenOperations);
        }

        private IEnumerator PrecomputeMovementCaches(MapGrid map)
        {
            if (map == null)
            {
                yield break;
            }

            // Precompute movement-cost caches for common movement modes
            var modes = new (bool w, bool f, bool r, bool m, bool a)[]
            {
                (true, false, false, false, false), // Walking/Infantry
                (false, true, false, false, false), // Flying
                (false, false, true, false, false), // Riding/Cavalry
                (false, false, false, true, false), // Magic
                (false, false, false, false, true), // Armored
            };

            foreach (var (w, f, r, m, a) in modes)
            {
                var key = MapGrid.MakeMovementModeKey(w, f, r, m, a);
                var res = map.BuildMovementCostCache(key, w, f, r, m, a);

                if (!res.Success)
                {
                    TurnrootLogger.Log(
                        $"BattlePrecomputeLoader: Failed to build movement cache for mode {key}: {res.ErrorMessage}",
                        TurnrootLogger.LogLevel.Warning
                    );
                }

                // Small delay between cache builds
                yield return new WaitForSeconds(timeBetweenOperations * 0.5f);
            }
        }
        #endregion

        #region Helper Methods
        private FundamentalComponents.Battles.BattleContext GetBattleContext() =>
            _battleContext ?? _brain?.battleBrain?.BattleObject?.Context;

        private bool IsContextValid(FundamentalComponents.Battles.BattleContext context)
        {
            var units = context?.Participants?.GetAllUnits();
            return context != null && units != null && units.Count > 0;
        }

        private int CalculateTaskCount(
            System.Collections.Generic.List<Characters.CharacterInstance> units,
            UnitAppearanceBrain appearanceBrain
        )
        {
            if (units == null)
            {
                return 0;
            }

            int tasksPerUnit = 3; // AI init + pathfinding + tiles
            if (appearanceBrain != null)
            {
                tasksPerUnit++; // + model spawn
            }

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
        }
        #endregion
    }
}
