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
        private BattleStartSkillExecutor _skillExecutor;
        #endregion

        #region State

        [HideInInspector]
        public bool IsInputEnabled = true;

        /// <summary>
        /// True during battle initialization (before precompute completes).
        /// Used to prevent premature snapshots during initial unit spawn.
        /// </summary>
        public bool IsInitializing { get; private set; } = true;

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

            _skillExecutor = new BattleStartSkillExecutor(this);
            _skillExecutor.SubscribeToEvents();

            Brain.OnPrecomputeCompleted += HandlePrecomputeCompleted;
        }

        private void HandlePrecomputeCompleted()
        {
            "BattleBrain: Precompute completed, taking initial snapshot".LogInfo();

            IsInitializing = false;
            _brain.TakeSnapshot();
        }

        private void Start()
        {
            // Roster initialization moved to InitializeLTMDependentData()
        }

        private void InitializeLTMDependentData()
        {
            // Called after LongTermMemory is initialized
            if (Brain.gamewideContextBrain == null)
            {
                "BattleBrain: gamewideContextBrain is null during InitializeLTMDependentData!".LogError();
                return;
            }

            if (Brain.gamewideContextBrain.GamewidePersistentPlayerRoster == null)
            {
                Brain.gamewideContextBrain.CreateOrRecallGamewidePersistentPlayerRoster();
            }

            _playerTeamRoster = Brain.gamewideContextBrain.GamewidePersistentPlayerRoster;

            if (_playerTeamRoster == null)
            {
                "BattleBrain: No PlayerTeamRoster available! Make sure PersistentPlayerRoster.asset is configured and assigned.".LogError();
                return;
            }

            var rosterInstance = Brain.gamewideContextBrain.GetOrCreatePlayerTeamRoster(
                _playerTeamRoster
            );

            if (rosterInstance == null)
            {
                $"BattleBrain: Failed to get/create runtime instance for roster '{_playerTeamRoster.name}'".LogError();
                return;
            }

            if (rosterInstance.Instances == null || rosterInstance.Instances.Count == 0)
            {
                $"BattleBrain: Roster '{_playerTeamRoster.name}' has no character instances. Check roster template configuration.".LogWarning();
            }
        }

        protected override void OnDestroy()
        {
            _skillExecutor?.UnsubscribeFromEvents();
            Brain.OnPrecomputeCompleted -= HandlePrecomputeCompleted;
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

            // Despawn pre-battle positioning models to avoid confusion with battle models
            PreparationObject?.StartingPositionsComponent?.DespawnAllModels();

            // Start in initializing mode (prevents premature snapshots)
            IsInitializing = true;

            InitializeBattleRosters();
            Brain.PublishBattleObjectSet(BattleObject);
            Brain.PublishBattleStarted();
            ClearUnitBattleState();

            InitializeAdvancedSystems();
            InitializePrecomputeLoader();
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

            return true;
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

        private void InitializeAdvancedSystems() =>
            // Clear any previous battle's command history
            _brain.Commands?.Clear();// Initial snapshot is taken AFTER precompute completes (see HandlePrecomputeCompleted)

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
            BattleObject.InitializeBattleRosters();

            // CRITICAL: Ensure units are selected for battle BEFORE creating battle copies
            // This handles cases where battle is started without going through pre-battle UI
            EnsureUnitsSelectedForBattle();

            var result = BattleObject.PopulateBattleRostersFromTemplates();
            if (!result.Success)
            {
                return result;
            }

            // CRITICAL: Set positions BEFORE populating context, so participants have valid positions
            ApplyPlacementsToBattle();

            var populateResult = PopulateBattleContextParticipants();
            if (!populateResult.Success)
            {
                $"Failed to populate battle context: {populateResult.ErrorMessage}".LogError();
                return populateResult;
            }

            SpawnRosterUnitsOntoGrid();

            _aiHelper = new BattleContextAIHelper(BattleObject.Context);
            return OperationResult.Successful();
        }

        /// <summary>
        /// Ensures at least some units are selected for battle before creating battle copies.
        /// Called automatically if battle is started without pre-battle UI.
        /// </summary>
        private void EnsureUnitsSelectedForBattle()
        {
            var gw = Brain.gamewideContextBrain;
            var prep = PreparationObject;

            if (gw == null || prep == null)
            {
                "BattleBrain: Cannot ensure units selected - missing gamewideContextBrain or PreparationObject".LogError();
                return;
            }

            var persistentRoster =
                gw.GamewidePersistentPlayerRoster
                ?? gw.CreateOrRecallGamewidePersistentPlayerRoster();
            if (persistentRoster == null)
            {
                "BattleBrain: Cannot ensure units selected - no persistent roster available".LogError();
                return;
            }

            var rosterInstance = gw.GetOrCreatePlayerTeamRoster(persistentRoster);
            if (rosterInstance == null)
            {
                $"BattleBrain: Cannot ensure units selected - failed to get roster instance for '{persistentRoster.name}'".LogError();
                return;
            }

            if (rosterInstance.Instances == null || rosterInstance.Instances.Count == 0)
            {
                $"BattleBrain: Cannot ensure units selected - roster '{persistentRoster.name}' has no character instances. Make sure the roster template has characters assigned.".LogError();
                return;
            }

            // Check if any units are already selected
            var selectedUnits = gw.GetSelectedForBattlePlayerTeamUnits();
            if (selectedUnits != null && selectedUnits.Count > 0)
            {
                // Units already selected, no need to auto-select
                $"BattleBrain: {selectedUnits.Count} units already selected for battle".LogInfo();
                return;
            }

            // Auto-select default units (required units + auto-fill from roster)
            $"BattleBrain: Auto-selecting default units from roster with {rosterInstance.Instances.Count} available characters".LogInfo();
            PreBattleSelectionHelper.EnsureDefaultPreBattleSelections(
                Brain,
                persistentRoster,
                rosterInstance,
                prep.MaxPlayerTeamUnits,
                prep.RequiredPlayerUnits
            );

            // Initialize placements so ApplyPlacementsToBattle has data
            prep.InitializePlacements();
        }

        /// <summary>
        /// Reads placements from BattlePreparationObject and writes positions to CharacterInstance.MapGridPosition.
        /// This is the single authoritative transfer from Starting Positions phase to Battle phase.
        /// </summary>
        private void ApplyPlacementsToBattle()
        {
            var prep = PreparationObject;
            var playerRoster = BattleObject?.PlayerTeamRoster;

            if (playerRoster == null)
            {
                "ApplyPlacementsToBattle: No player roster".LogWarning();
                return;
            }

            // If we have placements from the pre-battle UI, use them
            if (prep?.placements != null && prep.placements.Count > 0)
            {
                $"ApplyPlacementsToBattle: Applying {prep.placements.Count} placements from prep object".LogInfo();

                foreach (var kvp in prep.placements)
                {
                    var pos = kvp.Key;
                    var data = kvp.Value;
                    if (data == null)
                    {
                        $"ApplyPlacementsToBattle: Null CharacterData at position {pos}".LogWarning();
                        continue;
                    }

                    var inst =
                        playerRoster.GetInstanceFor(data)
                        ?? Brain.gamewideContextBrain?.FindInstanceByTemplate(data);

                    if (inst == null)
                    {
                        $"ApplyPlacementsToBattle: No instance for {data.name} at {pos}".LogWarning();
                        continue;
                    }

                    inst.MapGridPosition = pos;
                }
                return;
            }

            // Fallback: No placements, position units at spawn points directly
            "ApplyPlacementsToBattle: No placements found, using spawn points fallback".LogWarning();

            var spawnPoints = BattleObject?.MapGrid?.PlayerTeamSpawnPoints;
            if (spawnPoints == null || spawnPoints.Count == 0)
            {
                "ApplyPlacementsToBattle: No spawn points available".LogError();
                return;
            }

            var instances = playerRoster.Instances;
            if (instances == null || instances.Count == 0)
            {
                "ApplyPlacementsToBattle: No instances in player roster".LogWarning();
                return;
            }

            $"ApplyPlacementsToBattle: Positioning {instances.Count} units at {spawnPoints.Count} spawn points".LogInfo();

            var spawnIndex = 0;
            foreach (var inst in instances)
            {
                if (inst == null)
                {
                    continue;
                }

                if (spawnIndex >= spawnPoints.Count)
                {
                    $"ApplyPlacementsToBattle: More units ({instances.Count}) than spawn points ({spawnPoints.Count})".LogWarning();
                    break;
                }

                var pos = spawnPoints[spawnIndex];
                var oldPos = inst.MapGridPosition;
                inst.MapGridPosition = pos;
                var newPos = inst.MapGridPosition;
                spawnIndex++;
            }
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
                    $"PopulateBattleContextParticipants: Adding {unit.CharacterTemplate?.DisplayName} at position {unit.MapGridPosition} (id={unit.Id})".LogInfo();
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

        public PlayerTeamRosterInstance InstantiatePlayerTeamRoster()
        {
            if (_playerTeamRoster == null)
            {
                "BattleBrain.InstantiatePlayerTeamRoster: _playerTeamRoster is null! Make sure roster is initialized before starting battle.".LogError();
                return null;
            }

            if (Brain.gamewideContextBrain == null)
            {
                "BattleBrain.InstantiatePlayerTeamRoster: gamewideContextBrain is null!".LogError();
                return null;
            }

            var instance = Brain.gamewideContextBrain.GetOrCreatePlayerTeamRoster(
                _playerTeamRoster
            );
            if (instance == null)
            {
                $"BattleBrain.InstantiatePlayerTeamRoster: Failed to get/create roster instance for '{_playerTeamRoster.name}'".LogError();
            }

            return instance;
        }

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
