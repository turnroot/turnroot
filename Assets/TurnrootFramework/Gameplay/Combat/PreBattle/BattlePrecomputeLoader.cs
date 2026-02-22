using System.Collections;
using System.Linq;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.NPCs;
using Turnroot.Gameplay.Maps;
using Turnroot.Gameplay.PlayerSettings;
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
                // context null or empty unit list; nothing to precompute.
                return;
            }

            StartCoroutine(RunPrecomputeTasks());
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

            // ensure positions are up-to-date (should already be correct)
            context.GetCurrentUnitPositions(invalidateCache: true);

            var appearanceBrain = _brain.unitAppearanceBrain;

            // Only precompute units that were spawned/selected for this battle
            var units = FilterSpawnedUnits(context?.Participants?.GetAllUnits());

            int taskCount = CalculateTaskCount(units, appearanceBrain);

            var enemySupervisor = _brain.battleBrain?.BattleObject?.GetComponent<EnemySupervisor>();
            // If an EnemySupervisor exists we'll reserve the two precompute steps for it —
            // but the loader will wait for the runtime PlayerTeamRoster to be initialized before executing them.
            var hasEnemySupervisorWork = enemySupervisor != null;
            if (hasEnemySupervisorWork)
            {
                taskCount += 2;
            }

            if (taskCount == 0)
            {
                CompleteWithMinimalProgressAndNotify();
                yield break;
            }

            // Initialize TerrainTypeOverlay
            _brain.battleBrain.BattleObject.TerrainTypeOverlay.Initialize();

            InitializeLoadingProgress(taskCount);

            // Run EnemySupervisor precompute steps (if present) and update progress for the two reserved tasks
            if (hasEnemySupervisorWork)
            {
                // Wait (with timeout) for a suitable PlayerTeamRoster to be available.
                // Accept either the per-battle roster (`BattleBrain.PlayerTeamRoster`) or the
                // gamewide runtime roster (persistent). If the persistent roster asset exists
                // we will attempt to create/recall its runtime instance so precompute can proceed.
                const float rosterWaitTimeout = 2.0f; // seconds
                float waited = 0f;
                while (
                    _brain.battleBrain?.PlayerTeamRoster == null
                    && (
                        _brain.gamewideContextBrain?.GetPersistentPlayerTeamRosterInstance() == null
                    )
                    && waited < rosterWaitTimeout
                )
                {
                    yield return null;
                    waited += Time.deltaTime;
                }

                // Prefer the per-battle roster; otherwise use the gamewide runtime roster (create/recall if needed)
                var playerRoster =
                    _brain.battleBrain?.PlayerTeamRoster
                    ?? _brain.gamewideContextBrain?.GetPersistentPlayerTeamRosterInstance();

                if (
                    playerRoster == null
                    && _brain.gamewideContextBrain?.GamewidePersistentPlayerRoster != null
                )
                {
                    // Try to instantiate/recall the persistent runtime roster now so supervisor can compute details.
                    playerRoster = _brain.gamewideContextBrain.GetOrCreatePlayerTeamRoster(
                        _brain.gamewideContextBrain.GamewidePersistentPlayerRoster
                    );
                }

                if (playerRoster == null)
                {
                    TurnrootLogger.Log(
                        "BattlePrecomputeLoader: No PlayerTeamRoster available for EnemySupervisor precompute; skipping.",
                        TurnrootLogger.LogLevel.Warning
                    );

                    // Consume the two reserved progress slots so progress stays consistent.
                    IncrementProgress();
                    yield return new WaitForSeconds(timeBetweenOperations);
                    IncrementProgress();
                    yield return new WaitForSeconds(timeBetweenOperations);
                }
                else
                {
                    // 1) compute player-team details (prefer BattleBrain roster; otherwise use BattleContext participants)
                    EnemySupervisor.PlayerTeamDetails details;

                    if (_brain.battleBrain?.PlayerTeamRoster != null)
                    {
                        details = enemySupervisor.ComputeCurrentPlayerTeamDetails(
                            _brain.battleBrain.PlayerTeamRoster
                        );
                    }
                    else
                    {
                        var allies =
                            context?.Participants?.Allies?.FindAll(u =>
                                u != null && u.WasSpawnedDuringBattle
                            )
                            ?? new System.Collections.Generic.List<Characters.CharacterInstance>();
                        if (allies.Count > 0)
                        {
                            details = enemySupervisor.ComputeCurrentPlayerTeamDetails(allies);
                        }
                        else
                        {
                            // Fallback to gamewide runtime roster if available
                            var gwRoster =
                                _brain.gamewideContextBrain?.GetPersistentPlayerTeamRosterInstance();
                            details = enemySupervisor.ComputeCurrentPlayerTeamDetails(gwRoster);
                        }
                    }

                    IncrementProgress();
                    yield return new WaitForSeconds(timeBetweenOperations);

                    // ensure supervisor internal state
                    enemySupervisor.CurrentDifficulty = GameplayPlayerSettings
                        .Instance
                        .GameDifficulty;
                    enemySupervisor.EnemyInstancesByStartingPlacement =
                        new System.Collections.Generic.Dictionary<
                            EnemySupervisor.GenericEnemyStartingPlacement,
                            Characters.CharacterInstance
                        >();

                    // Ensure a deterministic per-battle seed exists in LTM and log it. This seed will be
                    // used by EnemySupervisor to make deterministic 'random' choices for the battle.
                    try
                    {
                        var prep = _brain.battleBrain?.PreparationObject;
                        var mapName =
                            prep?.MapGrid?.MapName
                            ?? _brain.battleBrain?.BattleObject?.MapGrid?.MapName
                            ?? "<unknown>";
                        var battleKey =
                            prep != null
                                ? $"{prep.name}.{mapName}"
                                : _brain.battleBrain?.BattleObject?.name ?? mapName;
                        var ltmKey = LtmKeys.BattleSeedKey(battleKey);

                        int seed = _brain.ltm?.RecallInt(ltmKey) ?? -1;
                        if (seed <= 0)
                        {
                            seed = System.BitConverter.ToInt32(
                                System.Guid.NewGuid().ToByteArray(),
                                0
                            );
                            _brain.ltm?.RememberInt(ltmKey, seed);
                            TurnrootLogger.Log(
                                $"BattlePrecomputeLoader: Generated battle seed {seed} for '{battleKey}'",
                                TurnrootLogger.LogLevel.Info
                            );
                        }
                        else
                        {
                            TurnrootLogger.Log(
                                $"BattlePrecomputeLoader: Loaded existing battle seed {seed} for '{battleKey}'",
                                TurnrootLogger.LogLevel.Info
                            );
                        }
                    }
                    catch { }

                    // 2) initialize pre-battle enemies and report any problems
                    var initRes = enemySupervisor.InitializePreBattleEnemies();
                    if (!initRes.Success)
                    {
                        TurnrootLogger.Log(
                            $"BattlePrecomputeLoader: EnemySupervisor.InitializePreBattleEnemies failed: {initRes.ErrorMessage}",
                            TurnrootLogger.LogLevel.Warning
                        );
                    }
                    IncrementProgress();
                    yield return new WaitForSeconds(timeBetweenOperations);
                }
            }

            // Ensure LTM replacements are applied for spawned units
            yield return EnsureLtmUnitsAreUsedRoutine(context);

            // Re-fetch spawned units from context in case replacements occurred
            units = FilterSpawnedUnits(context?.Participants?.GetAllUnits());

            // 1) Precompute movement caches
            yield return PrecomputeMovementCaches(context.MapGrid);

            // 2) Per-unit processing (extracted loop for clarity)
            yield return ProcessUnitsLoop(units, context, appearanceBrain);

            yield return new WaitForSeconds(timeBetweenOperations);

            _brain.PublishPrecomputeCompleted();
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
            _brain.PublishPrecomputeCompleted();
        }

        private IEnumerator ProcessUnit(
            Characters.CharacterInstance unit,
            FundamentalComponents.Battles.BattleContext context,
            UnitAppearanceBrain appearanceBrain
        )
        {
            // ensure unit has a class before we attempt pathfinding/tiles; the roster
            // initialization flow may not have assigned one yet when the loader starts.
            if (unit.CurrentClass == null)
            {
                var classToApply =
                    unit.CharacterTemplate?.StartingClass
                    ?? GameplayGeneralSettings.Instance?.GetDefaultStartingClass();
                if (classToApply != null)
                {
                    var classRes = unit.ChangeClass(classToApply, applyClassChangeBonuses: false);
                    if (!classRes.Success)
                    {
                        TurnrootLogger.Log(
                            $"BattlePrecomputeLoader: Failed to assign default class for unit {unit.Id}: {classRes.ErrorMessage}",
                            TurnrootLogger.LogLevel.Warning
                        );
                    }
                    else
                    {
                        unit.NeedsPersist = true;
                        _brain?.gamewideContextBrain?.PersistIfNeeded(unit, updateIndex: false);
                    }
                }
            }

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

            // 3) Spawn model (skip visuals for units managed by EnemySupervisor; supervisor will notify UnitAppearanceBrain)
            if (appearanceBrain != null)
            {
                var enemySupervisor =
                    _brain.battleBrain.BattleObject.GetComponent<EnemySupervisor>();
                var isSupervisorUnit =
                    enemySupervisor != null
                    && enemySupervisor.EnemyInstancesByStartingPlacement != null
                    && enemySupervisor.EnemyInstancesByStartingPlacement.Values.Contains(unit);

                if (!isSupervisorUnit)
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
                }

                IncrementProgress();
                yield return new WaitForSeconds(timeBetweenOperations);
            }

            // 4) Precompute weapon / inventory summary used by AI evaluations
            try
            {
                context.PrecomputeWeaponInfoForUnit(unit);
            }
            catch (System.Exception ex)
            {
                TurnrootLogger.Log(
                    $"BattlePrecomputeLoader: Failed to precompute weapon info for unit {unit.Id}: {ex.Message}",
                    TurnrootLogger.LogLevel.Warning
                );
            }
            IncrementProgress();
            yield return new WaitForSeconds(timeBetweenOperations);

            // 5) Precompute valid tiles
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
                                        TurnrootLogger.Log(
                                            "BattlePrecomputeLoader: Placements are locked for battle initialization; skipping placement update for recalled unit.",
                                            TurnrootLogger.LogLevel.Warning
                                        );
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
                                    TurnrootLogger.Log(
                                        "BattlePrecomputeLoader: PublishPlacementsSyncRequested failed: "
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
