using System.Collections.Generic;
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

        public GenericRosterInstance EnemyTeamRoster =>
            BattleObject != null ? BattleObject.EnemyTeamRoster : null;

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
                TurnrootLogger.Log(
                    "BattleBrain: No BattleGameObject found in any loaded scene",
                    TurnrootLogger.LogLevel.Error
                );
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
                    TurnrootLogger.Log(
                        $"BattleBrain: BattlePrecomputeLoader.Initialize failed: {initRes.ErrorMessage}",
                        TurnrootLogger.LogLevel.Warning
                    );
                }
            }
            else
            {
                TurnrootLogger.Log(
                    "BattleBrain: No BattlePrecomputeLoader found in scene; precompute will be skipped if no loader is available",
                    TurnrootLogger.LogLevel.Warning
                );
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
                    gw.SavePlayerRoster(lastSavedBattleTurn: 1);
                }
            }
        }

        private void StartPlayerTurn()
        {
            ProgressTurnOrder();

            if (playerTurnFlow != null)
            {
                playerTurnFlow.StartPlayerTurn();
                TurnrootLogger.Log(
                    $"Battle started. PlayerTurnFlow state: {playerTurnFlow.GetCurrentState()}"
                );
            }
        }

        private void HandleExitBattle(BattleExitType exitType)
        {
            TurnrootLogger.Log($"BattleBrain: Handling ExitBattle event with type: {exitType}");

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

            TurnrootLogger.Log("BattleBrain: Battle cleanup complete");
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
                        TurnrootLogger.Log(
                            $"BattleBrain: Found BattleGameObject in scene '{scene.name}'"
                        );
                        return battleObj;
                    }
                }
            }
            TurnrootLogger.Log(
                "BattleBrain: No BattleGameObject found in loaded scenes",
                TurnrootLogger.LogLevel.Error
            );
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

            var populateResult = PopulateBattleContextParticipants();
            if (!populateResult.Success)
            {
                TurnrootLogger.Log(
                    $"Failed to populate battle context during roster initialization: {populateResult.ErrorMessage}",
                    TurnrootLogger.LogLevel.Error
                );
                return populateResult;
            }

            SpawnRosterUnitsOntoGrid();

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

            foreach (var unit in EnemyTeamRoster.Instances)
            {
                if (!unit.IsDefeatedInCurrentBattle)
                {
                    context.Participants.Targets.Add(unit);
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

        private void SpawnRosterUnitsOntoGrid()
        {
            var enemyRoster = BattleObject.EnemyTeamRoster;
            if (BattleObject.HasThirdParty)
            {
                var thirdPartyRoster = BattleObject.ThirdPartyTeamRoster;
            }
            var playerTeamRoster = BattleObject.PlayerTeamRoster;

            foreach (var p in enemyRoster.GetPlacements())
            {
                var characterData = p.CharacterData;
                var characterInstance = enemyRoster.GetInstanceFor(characterData);
                var placement = p;
                BattleObject.Context.SpawnAtPosition(characterInstance, placement.SpawnPosition);
                enemyRoster.SetOrder(characterData, placement.Order);
            }

            if (BattleObject.HasThirdParty)
            {
                var thirdPartyRoster = BattleObject.ThirdPartyTeamRoster;
                foreach (var p in thirdPartyRoster.GetPlacements())
                {
                    var characterData = p.CharacterData;
                    var characterInstance = thirdPartyRoster.GetInstanceFor(characterData);
                    var placement = p;
                    BattleObject.Context.SpawnAtPosition(
                        characterInstance,
                        placement.SpawnPosition
                    );
                    thirdPartyRoster.SetOrder(characterData, placement.Order);
                }
            }

            foreach (var p in playerTeamRoster.GetPlacements())
            {
                var characterData = p.CharacterData;
                var characterInstance = playerTeamRoster.GetInstanceFor(characterData);
                var placement = p;

                // Avoid double-spawning: if the unit was already spawned during HandleBattleStarted
                // and its MapGridPosition matches the intended placement, skip spawning here.
                if (characterInstance != null && characterInstance.WasSpawnedDuringBattle)
                {
                    if (characterInstance.MapGridPosition == placement.SpawnPosition)
                    {
                        TurnrootLogger.Log(
                            $"SpawnRosterUnitsOntoGrid: Skipping spawn for {characterInstance.CharacterTemplate.DisplayName} - already spawned at {placement.SpawnPosition}",
                            TurnrootLogger.LogLevel.Info
                        );
                        playerTeamRoster.SetOrder(characterData, placement.Order);
                        continue;
                    }
                    else
                    {
                        // Mismatch detected: log and repair occupying grid so occupancy and instance position align.
                        TurnrootLogger.Log(
                            $"SpawnRosterUnitsOntoGrid: Repairing {characterInstance.CharacterTemplate.DisplayName} MapGridPosition from {characterInstance.MapGridPosition} to {placement.SpawnPosition}",
                            TurnrootLogger.LogLevel.Warning
                        );

                        try
                        {
                            var oldP = characterInstance.UnitPositionToMapGridPoint(
                                characterInstance.MapGridPosition,
                                BattleObject.Context.MapGrid
                            );
                            if (oldP != null)
                            {
                                BattleObject.Context.MapGrid.RemoveOccupied(oldP);
                            }
                        }
                        catch (System.Exception ex)
                        {
                            TurnrootLogger.Log(
                                "SpawnRosterUnitsOntoGrid: Failed during RemoveOccupied cleanup: "
                                    + ex.Message,
                                TurnrootLogger.LogLevel.Warning
                            );
                        }

                        try
                        {
                            var newMgp = BattleObject.Context.MapGrid.GetGridPoint(
                                placement.SpawnPosition.x,
                                placement.SpawnPosition.y
                            );
                            if (newMgp != null)
                            {
                                BattleObject.Context.MapGrid.SetOccupied(newMgp, characterInstance);
                            }
                            else
                            {
                                TurnrootLogger.Log(
                                    "SpawnRosterUnitsOntoGrid: Failed to find MapGridPoint for placement during repair.",
                                    TurnrootLogger.LogLevel.Error
                                );
                            }
                        }
                        catch (System.Exception ex)
                        {
                            TurnrootLogger.Log(
                                "SpawnRosterUnitsOntoGrid: Failed to align spawn position: "
                                    + ex.Message,
                                TurnrootLogger.LogLevel.Error
                            );
                        }
                    }
                }

                var spawned = BattleObject.Context.SpawnAtPosition(
                    characterInstance,
                    placement.SpawnPosition
                );
                if (!spawned)
                {
                    TurnrootLogger.Log(
                        $"SpawnRosterUnitsOntoGrid: SpawnAtPosition failed for {characterData?.DisplayName} at {placement.SpawnPosition}",
                        TurnrootLogger.LogLevel.Warning
                    );
                }

                playerTeamRoster.SetOrder(characterData, placement.Order);
            }

            // Final verification pass: ensure all roster instances have MapGridPosition matching placements.
            try
            {
                var placementsArr = playerTeamRoster.GetPlacements();
                foreach (var ap in placementsArr)
                {
                    var inst = playerTeamRoster.GetInstanceFor(ap.CharacterData);
                    if (inst == null)
                    {
                        continue;
                    }

                    if (inst.MapGridPosition != ap.SpawnPosition)
                    {
                        TurnrootLogger.Log(
                            $"SpawnRosterUnitsOntoGrid: Post-check repair for {inst.CharacterTemplate.DisplayName} from {inst.MapGridPosition} to {ap.SpawnPosition}",
                            TurnrootLogger.LogLevel.Warning
                        );

                        try
                        {
                            var oldP = inst.UnitPositionToMapGridPoint(
                                inst.MapGridPosition,
                                BattleObject.Context.MapGrid
                            );
                            if (oldP != null)
                            {
                                BattleObject.Context.MapGrid.RemoveOccupied(oldP);
                            }
                        }
                        catch (System.Exception ex)
                        {
                            TurnrootLogger.Log(
                                "SpawnRosterUnitsOntoGrid: Post-check RemoveOccupied failed: "
                                    + ex.Message,
                                TurnrootLogger.LogLevel.Warning
                            );
                        }

                        try
                        {
                            var newMgp = BattleObject.Context.MapGrid.GetGridPoint(
                                ap.SpawnPosition.x,
                                ap.SpawnPosition.y
                            );
                            if (newMgp != null)
                            {
                                BattleObject.Context.MapGrid.SetOccupied(newMgp, inst);
                            }
                            else
                            {
                                TurnrootLogger.Log(
                                    "SpawnRosterUnitsOntoGrid: Failed to find MapGridPoint for placement during repair.",
                                    TurnrootLogger.LogLevel.Error
                                );
                            }
                        }
                        catch (System.Exception ex)
                        {
                            TurnrootLogger.Log(
                                "SpawnRosterUnitsOntoGrid: Post-check alignment failed: "
                                    + ex.Message,
                                TurnrootLogger.LogLevel.Error
                            );
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                TurnrootLogger.Log(
                    "SpawnRosterUnitsOntoGrid: Unexpected error during spawn pass: " + ex.Message,
                    TurnrootLogger.LogLevel.Warning
                );
            }

            BattleObject.Context.InvalidateUnitPositionCache();
        }

        #endregion

        #region Turn Management

        public void ProgressTurnOrder()
        {
            if (!turnRotisserie.Progress())
            {
                TurnrootLogger.Log(
                    "BattleBrain: Failed to progress turn order!",
                    TurnrootLogger.LogLevel.Error
                );
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
