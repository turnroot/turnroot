using System.Collections;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.NPCs;
using Turnroot.Gameplay.Maps;
using Turnroot.Gameplay.PlayerSettings;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.Precompute
{
    public partial class BattlePrecomputeLoader
    {
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
                "BattlePrecomputeLoader: Invalid context, completing with minimal progress".LogWarning();

                CompleteWithMinimalProgressAndNotify();
                yield break;
            }

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
                    "BattlePrecomputeLoader: No PlayerTeamRoster available for EnemySupervisor precompute; skipping.".LogWarning();

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
                        seed = System.BitConverter.ToInt32(System.Guid.NewGuid().ToByteArray(), 0);
                        _brain.ltm?.RememberInt(ltmKey, seed);
                    }

                    // 2) initialize pre-battle enemies and report any problems
                    var initRes = enemySupervisor.InitializePreBattleEnemies();
                    if (!initRes.Success)
                    {
                        $"BattlePrecomputeLoader: EnemySupervisor.InitializePreBattleEnemies failed: {initRes.ErrorMessage}".LogWarning();
                    }
                    IncrementProgress();
                    yield return new WaitForSeconds(timeBetweenOperations);
                }
            }

            // LTM unit replacement removed - placements are now managed by BattlePreparationObject
            // and applied once at battle start via BattleBrain.ApplyPlacementsToBattle()

            // Ensure every spawned unit has a class assigned
            yield return EnsureUnitsHaveClassesRoutine(context);

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
            // Validate unit has a valid position (should be set by ApplyPlacementsToBattle before precompute)
            var sentinel = new Vector2Int(-9999, -9999);
            if (unit == null || unit.MapGridPosition == sentinel)
            {
                if (unit != null)
                {
                    $"BattlePrecomputeLoader: ERROR - Unit {unit.CharacterTemplate?.DisplayName} (id={unit.Id}) has invalid sentinel position! Position should have been set by ApplyPlacementsToBattle before precompute.".LogError();
                }
                yield break;
            }

            $"BattlePrecomputeLoader: Processing unit {unit.CharacterTemplate?.DisplayName} at position {unit.MapGridPosition} (id={unit.Id})".LogInfo();

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
                        $"BattlePrecomputeLoader: Failed to assign default class for unit {unit.Id}: {classRes.ErrorMessage}".LogWarning();
                    }
                    else
                    {
                        unit.NeedsPersist = true;
                        _brain?.gamewideContextBrain?.PersistIfNeeded(unit, updateIndex: false);
                        _brain?.PublishCharacterClassChanged(unit);
                    }
                }
            }

            // compute starting combat rates (hit/avoid/crit)
            unit.RecalculateCombatRates();

            // 1) Initialize AI helper for unit
            if (context.AIHelper != null)
            {
                var initResult = context.AIHelper.InitializeAIControlledUnit(unit);
                if (!initResult.Success)
                {
                    $"BattlePrecomputeLoader: AI init failed for unit {unit.Id}: {initResult.ErrorMessage}".LogWarning();
                }
            }
            IncrementProgress();
            yield return new WaitForSeconds(timeBetweenOperations);

            // 2) Precompute pathfinding parameters
            var paramsOk = context.PrecomputePathfindingParameters(unit);
            if (!paramsOk)
            {
                $"BattlePrecomputeLoader: Pathfinding params failed for unit {unit.Id}".LogWarning();
            }
            IncrementProgress();
            yield return new WaitForSeconds(timeBetweenOperations);

            // 3) Model spawning is NOT precompute's responsibility!
            // SINGLE SOURCE OF TRUTH: BattleBrain.SpawnRosterUnitsOntoGrid() → SpawnCommand → UnitSpawnedEvent → HandleUnitSpawnedEvent
            // Precompute runs AFTER models are already spawned. This step is intentionally removed to maintain
            // architectural clarity and prevent duplicate spawning that corrupts model-to-unit mappings.
            // Precompute should ONLY handle AI initialization, pathfinding, and caching - NOT model spawning.

            // 4) Precompute weapon / inventory summary used by AI evaluations
            var weapResult = context.PrecomputeWeaponInfoForUnit(unit);
            if (!weapResult.Success)
            {
                $"BattlePrecomputeLoader: Failed to precompute weapon info for unit {unit.Id}: {weapResult.ErrorMessage}".LogWarning();
            }
            IncrementProgress();
            yield return new WaitForSeconds(timeBetweenOperations);

            // 5) Precompute valid tiles
            var tilesOk = context.TryGetValidTilesForUnit(unit, out _, out _, forceRecompute: true);

            if (!tilesOk)
            {
                $"BattlePrecomputeLoader: Tile computation failed for unit {unit.Id}".LogWarning();
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
                    $"BattlePrecomputeLoader: Failed to build movement cache for mode {key}: {res.ErrorMessage}".LogWarning();
                }

                // Small delay between cache builds
                yield return new WaitForSeconds(timeBetweenOperations * 0.5f);
            }
        }
        #endregion
    }
}
