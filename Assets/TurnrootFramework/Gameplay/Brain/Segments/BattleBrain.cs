using System.Collections.Generic;
using System.Linq;
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
    /// <summary>
    /// The battle brain manages one battle at a time.
    /// Responsible for initializing battles and managing turn order.
    /// </summary>
    [RequireComponent(typeof(TurnRotisserie))]
    [RequireComponent(typeof(PlayerTurnFlow))]
    public partial class BattleBrain : BrainComponent
    {
        #region Dependencies

        [SerializeField, HideInInspector]
        private PlayerTeamRoster _playerTeamRoster;

        [HideInInspector]
        public TurnRotisserie turnRotisserie;

        [HideInInspector]
        public PlayerTurnFlow playerTurnFlow;

        private BattleContextAIHelper _aiHelper;

        // helper for passive skill execution
        private BattleStartSkillExecutor _skillExecutor;
        #endregion

        #region State

        [HideInInspector]
        public bool IsInputEnabled = true;

        public BattleGameObject BattleObject { get; private set; }
        public Combat.PreBattle.BattlePreparationObject PreparationObject { get; private set; }

        public CharacterInstance ActiveUnit => turnRotisserie.GetActiveUnit();

        public int CurrentTurnNumber { get; private set; } = 0;

        public void IncrementTurnNumber()
        {
            CurrentTurnNumber++;
            BattleObject.Context.InvalidateAllTileCaches();
        }

        #endregion

        #region Roster Accessors
        public PlayerTeamRosterInstance PlayerTeamRoster =>
            BattleObject != null ? BattleObject.PlayerTeamRoster : null;

        public GenericRosterInstance ThirdPartyTeamRoster =>
            BattleObject != null ? BattleObject.ThirdPartyTeamRoster : null;

        #endregion


        #region Initialization

        protected override void Awake()
        {
            base.Awake();

            turnRotisserie = GetComponent<TurnRotisserie>();
            if (turnRotisserie != null)
            {
                turnRotisserie.BindToBattleBrain(this);
            }
            playerTurnFlow = GetComponent<PlayerTurnFlow>();
            playerTurnFlow.Intialize();

            // create and subscribe skill executor
            _skillExecutor = new BattleStartSkillExecutor(this);
            _skillExecutor.SubscribeToEvents();
        }

        private void Start()
        {
            if (
                Brain.gamewideContextBrain != null
                && Brain.gamewideContextBrain.GamewidePersistentPlayerRoster == null
            )
            {
                Brain.gamewideContextBrain.CreateOrRecallGamewidePersistentPlayerRoster();
            }

            _playerTeamRoster =
                Brain.gamewideContextBrain?.GamewidePersistentPlayerRoster ?? _playerTeamRoster;

            Brain.gamewideContextBrain?.GetOrCreatePlayerTeamRoster(_playerTeamRoster);
        }

        protected override void OnDestroy()
        {
            _skillExecutor?.UnsubscribeFromEvents();
            base.OnDestroy();
        }

        #endregion

        #region Battle Lifecycle

        public void HandleStartBattle()
        {
            if (!InitializeBattleObject())
            {
                return;
            }

            InitializeBattleRosters();
            PublishBattleEvents();
            ClearUnitBattleState();
            InitializeAdvancedSystems();
            InitializePrecomputeLoader();
            SaveInitialRosterPlacements();
            StartPlayerTurn();
        }

        private bool InitializeBattleObject()
        {
            BattleObject = FindBattleGameObjectInScene();

            if (BattleObject == null)
            {
                "BattleBrain: No BattleGameObject found in any loaded scene".LogError();
                return false;
            }

            BattleObject.Brain = _brain;
            BattleObject.ConnectToBrainEvents();
            BattleObject.ConnectBattleConditionsToContext();
            BattleObject.Context.InvalidateUnitPositionCache();

            return true;
        }

        private void PublishBattleEvents()
        {
            Brain.PublishBattleObjectSet(BattleObject);
            Brain.PublishBattleStarted();
        }

        private void ClearUnitBattleState()
        {
            var allInstances = GetAllActiveInstances();
            foreach (var inst in allInstances)
            {
                if (inst != null)
                {
                    inst.LastAttackedTarget = null;
                    ClearLastAttacker(BattleObject?.Context, inst);
                }
            }
            BattleObject?.Context?.ClearLastAttackHistory();
        }

        private void InitializeAdvancedSystems()
        {
            // Clear any previous battle's command history
            _brain.Commands?.Clear();
            // Take initial snapshot of battle state
            _brain.TakeSnapshot();
        }

        private void InitializePrecomputeLoader()
        {
            var precomputeLoader =
                FindFirstObjectByType<Combat.Precompute.BattlePrecomputeLoader>();

            if (precomputeLoader != null)
            {
                var initRes = precomputeLoader.Initialize(_brain, BattleObject?.Context);
                if (!initRes.Success)
                {
                    $"BattleBrain: BattlePrecomputeLoader.Initialize failed: {initRes.ErrorMessage}".LogWarning();
                }
                else
                {
                    // The loader is now tied to the current battle context and roster
                    // placements have already been applied by this point (InitializeBattleRosters
                    // is called before we reach here).  Kick off the precompute run to avoid
                    // any race with scene‑flow timings that might start the loader earlier.
                    precomputeLoader.ForceStartPrecomputeIfPossible();
                }
            }
            else
            {
                "BattleBrain: No BattlePrecomputeLoader found in scene; precompute will be skipped if no loader is available".LogWarning();
            }
        }

        private void SaveInitialRosterPlacements()
        {
            var gw = Brain.gamewideContextBrain;
            if (gw != null)
            {
                int lastSaved = gw.GetSavedPlayerRosterLastBattleTurn();
                if (lastSaved <= 1)
                {
                    // Use the brain event to request a save (lastSavedBattleTurn == 1)
                    Brain?.PublishSavePlayerRosterRequested(1);
                }
            }
        }

        private void StartPlayerTurn()
        {
            ProgressTurnOrder();

            if (playerTurnFlow != null)
            {
                playerTurnFlow.StartPlayerTurn();
                $"Battle started. PlayerTurnFlow state: {playerTurnFlow.GetCurrentState()}".LogInfo();
            }
        }

        private void HandleExitBattle(BattleExitType exitType)
        {
            $"BattleBrain: Handling ExitBattle event with type: {exitType}".LogInfo();

            if (exitType != BattleExitType.Bookmark)
            {
                _brain.Commands?.Clear();
                _brain.Snapshots?.Clear();
            }
            _brain.battleBrain.BattleObject.ClearBattleRosters();

            // Clear transient per-battle data on characters
            var allInstances = GetAllActiveInstances();
            foreach (var inst in allInstances)
            {
                if (inst != null)
                {
                    inst.LastAttackedTarget = null;
                    ClearLastAttacker(BattleObject.Context, inst);
                }
            }

            Brain.battleBrain.BattleObject.Context.ClearLastAttackHistory();

            var precomputeLoader =
                FindFirstObjectByType<Combat.Precompute.BattlePrecomputeLoader>();
            if (precomputeLoader != null)
            {
                precomputeLoader.ResetPrecomputeFlag();
            }

            "BattleBrain: Battle cleanup complete".LogInfo();
        }

        private BattleGameObject FindBattleGameObjectInScene()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                {
                    continue;
                }

                foreach (GameObject rootObject in scene.GetRootGameObjects())
                {
                    var battleObj = rootObject.GetComponentInChildren<BattleGameObject>();
                    if (battleObj != null)
                    {
                        $"BattleBrain: Found BattleGameObject in scene '{scene.name}'".LogInfo();
                        return battleObj;
                    }
                }
            }
            "BattleBrain: No BattleGameObject found in loaded scenes".LogError();
            return null;
        }

        #endregion

        #region Battle Roster Initialization

        private OperationResult InitializeBattleRosters()
        {
            // 1. Create empty runtime roster instances
            BattleObject.InitializeBattleRosters();

            // 2. Populate rosters from templates and persistent data
            var result = BattleObject.PopulateBattleRostersFromTemplates();
            if (!result.Success)
            {
                return result;
            }

            // Align pre-battle placement references with the roster's canonical instances so the
            // hand-off from starting positions -> start battle is deterministic and single-sourced.
            try
            {
                var prep = PreparationObject;
                var playerRoster = BattleObject?.PlayerTeamRoster;
                if (prep != null)
                {
                    // Prevent precompute from mutating placements while we align/persist/spawn.
                    prep.PlacementsLocked = true;
                }

                if (prep?.placements != null && playerRoster != null)
                {
                    var keys = prep.placements.Keys.ToList();
                    foreach (var pos in keys)
                    {
                        var data = prep.placements[pos];
                        if (data == null)
                        {
                            continue;
                        }

                        var inst =
                            playerRoster.GetInstanceFor(data)
                            ?? Brain.gamewideContextBrain?.FindInstanceByTemplate(data);
                        if (inst == null)
                        {
                            $"BattleBrain: Placement at {pos} references {data.name} which has no active instance; roster/spawn may create it at start.".LogInfo();
                        }
                    }

                    // Persist the corrected placements to LTM so start-battle reads are authoritative.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    try
                    {
                        var dbg = "";
                        foreach (var kvp in prep.placements)
                        {
                            dbg += $"[{kvp.Key}->{kvp.Value?.name}] ";
                        }
                        $"BattleBrain: placement alignment pre-sync: {dbg}".LogInfo();
                    }
                    catch { }
#endif
                    try
                    {
                        Brain?.PublishPlacementsSyncRequested(
                            persist: true,
                            forceApplyPlacementsOnLoad: false
                        );
                    }
                    catch (System.Exception ex)
                    {
                        "BattleBrain: Failed to PublishPlacementsSyncRequested after alignment: ".LogWarning();
                        ex.Message.LogWarning();
                    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    // Dev assertion: ensure placements reference character templates that exist in runtime roster or active instances.
                    try
                    {
                        foreach (var kvp in prep.placements)
                        {
                            var dataCheck = kvp.Value;
                            if (dataCheck == null)
                            {
                                continue;
                            }
                            var instCheck =
                                playerRoster.GetInstanceFor(dataCheck)
                                ?? Brain.gamewideContextBrain?.FindInstanceByTemplate(dataCheck);
                            if (instCheck == null)
                            {
                                $"BattleBrain Assertion: Placement {dataCheck.name} at {kvp.Key} has no runtime instance after alignment".LogWarning();
                                Debug.Assert(
                                    instCheck != null,
                                    $"Placement {dataCheck.name} at {kvp.Key} has no runtime instance after alignment"
                                );
                            }
                        }
                    }
                    catch { }
#endif
                }
            }
            catch (System.Exception ex)
            {
                "BattleBrain: Placement alignment failed: ".LogWarning();
                ex.Message.LogWarning();
            }

            var populateResult = PopulateBattleContextParticipants();
            if (!populateResult.Success)
            {
                $"Failed to populate battle context during roster initialization: {populateResult.ErrorMessage}".LogError();
                return populateResult;
            }

            SpawnRosterUnitsOntoGrid();

            // Unlock placements after we've completed authoritative roster initialization and spawning.
            try
            {
                var prep = PreparationObject;
                if (prep != null)
                {
                    prep.PlacementsLocked = false;
                }
            }
            catch { }

            _aiHelper = new BattleContextAIHelper(BattleObject.Context);
            return OperationResult.Successful();
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

            context.Participants.Targets.Clear();
            context.Participants.Allies.Clear();
            context.Participants.ThirdParty.Clear();

            foreach (var unit in PlayerTeamRoster.Instances)
            {
                if (!unit.IsDefeatedInCurrentBattle)
                {
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

        #region Turn Management

        public void ProgressTurnOrder()
        {
            if (!turnRotisserie.Progress())
            {
                "BattleBrain: Failed to progress turn order!".LogError();
            }
        }

        #endregion

        #region Roster Management API
        public GenericRosterInstance InstantiateGenericRoster(
            GenericRoster roster,
            bool register = false
        ) => Brain.gamewideContextBrain.GetOrCreateGenericRoster(roster, register);

        public PlayerTeamRosterInstance InstantiatePlayerTeamRoster() =>
            Brain.gamewideContextBrain.GetOrCreatePlayerTeamRoster(_playerTeamRoster);

        public void RecallGenericRosters(List<GenericRoster> rosters) =>
            Brain.gamewideContextBrain.RecallGenericRosters(rosters);

        public CharacterInstance FindInstanceByTemplate(CharacterData template) =>
            Brain.gamewideContextBrain.FindInstanceByTemplate(template);

        public List<CharacterInstance> GetAllActiveInstances() =>
            Brain.gamewideContextBrain.GetAllActiveInstances();

        public void SaveUniqueCharacterProgress(CharacterInstance instance) =>
            Brain.gamewideContextBrain.SaveUniqueCharacterProgress(instance);

        #endregion

        #region Unit Movement

        /// <summary>
        /// Internal helper to move a unit on the grid and publish movement events.
        /// This should only be called by command implementations (e.g., MoveCommand) so that movement is undoable/redoable.
        /// Use <see cref="BattleContext.MoveUnitToPoint"/> to perform a commanded move.
        /// </summary>
        internal bool MoveUnit(CharacterInstance unit, Vector2Int target, MapGrid mapGrid)
        {
            if (unit == null || mapGrid == null)
            {
                return false;
            }

            var from = unit.MapGridPosition;
            var oldPoint = unit.UnitPositionToMapGridPoint(from, mapGrid);
            var result = unit.MoveToPosition(target, mapGrid);
            if (result.Success)
            {
                var newPoint = unit.UnitPositionToMapGridPoint(target, mapGrid);
                mapGrid.RemoveOccupied(oldPoint);
                mapGrid.SetOccupied(newPoint, unit);
                BattleObject.Context.InvalidateUnitTileCache(unit);
                BattleObject.Context.InvalidateUnitPositionCache();

                if (BattleObject.Context.Unit?.UnitInstance == unit)
                {
                    BattleObject.Context.UpdateAdjacentUnits();
                    BattleObject.Context.UpdateTargetsInRange();
                }

                Brain.PublishCharacterMoveCompleted(unit, newPoint);
                Brain.PublishUnitMoved(unit, target);
                Brain.Publish(new Events.UnitMovedEvent(unit, from, target));
                Brain.PublishMoveCompleted(unit, newPoint);
            }
            return result.Success;
        }

        #endregion
    }
}
