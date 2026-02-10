using System.Collections;
using System.Linq;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Maps;
using Turnroot.GameSettings;
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

        [SerializeField]
        private float timeBetweenOperations = 0.1f;

        #region Initialization
        public OperationResult Initialize(
            Brain.Brain brain,
            FundamentalComponents.Battles.BattleContext context = null
        )
        {
            var validation = OperationResultGuards.RequireNotNull(brain, nameof(brain));
            if (!validation.Success)
            {
                return validation;
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
            _loadingController?.Initialize(); // Ensure loading UI is initialized before adding progress

            var context = GetBattleContext();

            if (!IsContextValid(context))
            {
                TurnrootLogger.Log(
                    "BattlePrecomputeLoader: Invalid context, completing with minimal progress",
                    TurnrootLogger.LogLevel.Warning
                );

                CompleteWithMinimalProgressAndNotify();
                yield break;
            }

            var appearanceBrain = _brain?.unitAppearanceBrain;

            // Only precompute units that were spawned/selected for this battle
            var units = FilterSpawnedUnits(context?.Participants?.GetAllUnits());

            // Validate and repair unit positions where possible to avoid inconsistent precompute
            if (units != null && units.Count > 0)
            {
                var toRemove = new System.Collections.Generic.List<Characters.CharacterInstance>();
                foreach (
                    var unit in new System.Collections.Generic.List<Characters.CharacterInstance>(
                        units
                    )
                )
                {
                    var gp = context.MapGrid?.GetGridPoint(
                        unit.MapGridPosition.x,
                        unit.MapGridPosition.y
                    );
                    if (gp == null)
                    {
                        var rosterPlacements =
                            _brain?.battleBrain?.PlayerTeamRoster?.GetPlacements();
                        var matching = rosterPlacements?.FirstOrDefault(p =>
                            p.CharacterData == unit.CharacterTemplate
                        );
                        if (matching != null)
                        {
                            unit.MapGridPosition = matching.SpawnPosition;
                            var newGp = context.MapGrid?.GetGridPoint(
                                matching.SpawnPosition.x,
                                matching.SpawnPosition.y
                            );
                            if (newGp != null)
                            {
                                newGp.CurrentInstance = unit;
                                TurnrootLogger.Log(
                                    $"BattlePrecomputeLoader: Repaired unit {unit.Id} position to {matching.SpawnPosition}",
                                    TurnrootLogger.LogLevel.Info
                                );
                                continue;
                            }
                        }
                        TurnrootLogger.Log(
                            $"BattlePrecomputeLoader: Unit {unit.Id} has invalid map position {unit.MapGridPosition}",
                            TurnrootLogger.LogLevel.Warning
                        );
                        toRemove.Add(unit);
                    }
                }
                foreach (var r in toRemove)
                    units.Remove(r);
            }

            int taskCount = CalculateTaskCount(units, appearanceBrain);
            if (taskCount == 0)
            {
                CompleteWithMinimalProgressAndNotify();
                yield break;
            }

            InitializeLoadingProgress(taskCount);

            // Ensure LTM replacements are applied for spawned units
            yield return EnsureLtmUnitsAreUsedRoutine(context);

            // Re-fetch spawned units from context in case replacements occurred
            units = FilterSpawnedUnits(context?.Participants?.GetAllUnits());

            // 1) Precompute movement caches
            yield return PrecomputeMovementCaches(context.MapGrid);

            // 2) Per-unit processing (extracted loop for clarity)
            yield return ProcessUnitsLoop(units, context, appearanceBrain);

            yield return new WaitForSeconds(timeBetweenOperations);

            _brain?.PublishPrecomputeCompleted();
        }

        private System.Collections.Generic.List<Characters.CharacterInstance> FilterSpawnedUnits(
            System.Collections.Generic.List<Characters.CharacterInstance> units
        )
        {
            return units?.FindAll(u => u != null && u.WasSpawnedDuringBattle)
                ?? new System.Collections.Generic.List<Characters.CharacterInstance>();
        }

        private IEnumerator ProcessUnitsLoop(
            System.Collections.Generic.List<Characters.CharacterInstance> units,
            FundamentalComponents.Battles.BattleContext context,
            UnitAppearanceBrain appearanceBrain
        )
        {
            if (units == null)
            {
                yield break;
            }

            foreach (var unit in units)
            {
                if (unit == null)
                {
                    continue;
                }

                yield return ProcessUnit(unit, context, appearanceBrain);
            }
        }

        private void CompleteWithMinimalProgressAndNotify()
        {
            CompleteWithMinimalProgress();
            _brain?.PublishPrecomputeCompleted();
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

            // 3) Spawn model
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

            // 4) Precompute valid tiles
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

        private IEnumerator EnsureLtmUnitsAreUsedRoutine(
            FundamentalComponents.Battles.BattleContext context
        )
        {
            if (context == null || _brain?.gamewideContextBrain == null)
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
                        if (recalled != null && !object.ReferenceEquals(recalled, unit))
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
                            var prep = _brain.battleBrain?.PreparationObject;
                            if (prep != null && prep.placements != null)
                            {
                                var keysToUpdate = prep
                                    .placements.Where(kvp =>
                                        kvp.Value != null && kvp.Value.CharacterTemplate == template
                                    )
                                    .Select(kvp => kvp.Key)
                                    .ToList();
                                foreach (var k in keysToUpdate)
                                {
                                    prep.placements[k] = recalled;
                                }
                                try
                                {
                                    prep.SyncPlacementsToRuntimeRoster(persist: true);
                                }
                                catch (System.Exception ex)
                                {
                                    TurnrootLogger.Log(
                                        "BattlePrecomputeLoader: SyncPlacementsToRuntimeRoster failed: "
                                            + ex.Message,
                                        TurnrootLogger.LogLevel.Warning
                                    );
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
                            current.CharacterTemplate?.StartingClass
                            ?? GameplayGeneralSettings.Instance?.GetDefaultStartingClass();

                        if (classToApply != null)
                        {
                            var classRes = current.ChangeClass(
                                classToApply,
                                applyClassChangeBonuses: false
                            );
                            if (!classRes.Success)
                            {
                                TurnrootLogger.Log(
                                    $"BattlePrecomputeLoader: Failed to assign default class for unit {current.Id}: {classRes.ErrorMessage}",
                                    TurnrootLogger.LogLevel.Warning
                                );
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

            // Tasks per unit:
            // 1) LTM recall/check
            // 2) AI init
            // 3) Pathfinding params
            // 4) Tiles computation
            // 5) Model spawn
            int tasksPerUnit = 5;
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

            _brain?.PublishPrecomputeCompleted();
        }
        #endregion
    }
}
