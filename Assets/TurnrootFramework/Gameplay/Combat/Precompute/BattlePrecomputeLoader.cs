using System;
using System.Collections;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Brain.Components;
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

        #region Initialization
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

            var brain = UnityEngine.Object.FindFirstObjectByType<Brain.Brain>();
            if (brain != null)
            {
                Initialize(brain);
            }
        }

        private void OnDestroy() => _brain = null;
        #endregion

        #region Precompute Control
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
            PrecomputeMovementCaches(context.mapGrid);

            foreach (var unit in units)
            {
                if (unit == null)
                {
                    continue;
                }

                yield return ProcessUnit(unit, context, appearanceBrain);
            }
        }

        private IEnumerator ProcessUnit(
            Characters.CharacterInstance unit,
            FundamentalComponents.Battles.BattleContext context,
            UnitAppearanceBrain appearanceBrain
        )
        {
            // Initialize AI
            context.AIHelper?.InitializeAIControlledUnit(unit);
            IncrementProgress();
            yield return null;

            // Precompute pathfinding
            context.PrecomputePathfindingParameters(unit);
            IncrementProgress();
            yield return null;

            // Spawn model
            if (appearanceBrain != null)
            {
                appearanceBrain.PrecomputeSpawnModelAt(
                    unit,
                    unit.MapGridPosition,
                    prebattle: false
                );
                IncrementProgress();
                yield return null;
            }

            // Precompute tiles
            context.TryGetValidTilesForUnit(unit, out _, out _, forceRecompute: true);
            IncrementProgress();
            yield return null;
        }

        private void PrecomputeMovementCaches(MapGrid map)
        {
            if (map == null)
            {
                return;
            }

            var modes = new (bool w, bool f, bool r, bool m, bool a)[]
            {
                (true, false, false, false, false),
                (false, true, false, false, false),
                (false, false, true, false, false),
                (false, false, false, true, false),
                (false, false, false, false, true),
            };

            foreach (var mode in modes)
            {
                var key = MapGrid.MakeMovementModeKey(mode.w, mode.f, mode.r, mode.m, mode.a);
                map.BuildMovementCostCache(key, mode.w, mode.f, mode.r, mode.m, mode.a);
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
                tasksPerUnit++;
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

        private void IncrementProgress()
        {
            _loadingController?.IncrementLoadedAmountBy(1);
        }

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
