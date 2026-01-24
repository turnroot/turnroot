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

        private TurnRotisserie _turnRotisserie;

        [HideInInspector]
        public PlayerTurnFlow playerTurnFlow;

        private BattleContextAIHelper _aiHelper;

        #endregion

        #region State

        public BattleGameObject BattleObject { get; private set; }
        public Combat.PreBattle.BattlePreparationObject PreparationObject { get; private set; }

        public CharacterInstance ActiveUnit => _turnRotisserie.GetActiveUnit();

        #endregion

        #region Roster Accessors

        // Roster accessors through BattleGameObject
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

            _turnRotisserie = GetComponent<TurnRotisserie>();
            if (_turnRotisserie != null)
            {
                _turnRotisserie.BindToBattleBrain(this);
            }
            playerTurnFlow = GetComponent<PlayerTurnFlow>();
            playerTurnFlow.Intialize();
        }

        private void Start()
        {
            if (
                _brain?.gamewideContextBrain != null
                && _brain.gamewideContextBrain.GamewidePersistentPlayerRoster == null
            )
            {
                _brain.gamewideContextBrain.CreateOrRecallGamewidePersistentPlayerRoster();
            }

            _playerTeamRoster =
                _brain?.gamewideContextBrain?.GamewidePersistentPlayerRoster ?? _playerTeamRoster;

            _brain?.gamewideContextBrain?.GetOrCreatePlayerTeamRoster(_playerTeamRoster);
        }

        #endregion

        #region Battle Lifecycle

        public void HandleStartBattle()
        {
            TurnrootLogger.Log("BattleBrain: Handling StartBattle event");

            BattleObject = FindBattleGameObjectInScene();

            if (BattleObject == null)
            {
                TurnrootLogger.Log(
                    "BattleBrain: No BattleGameObject found in any loaded scene",
                    TurnrootLogger.LogLevel.Error
                );
                return;
            }

            // Connect systems
            BattleObject.Brain = _brain;
            BattleObject.ConnectToBrainEvents();
            BattleObject.ConnectBattleConditionsToContext();

            BattleObject.Context.InvalidateUnitPositionCache();

            TurnrootLogger.Log($"BattleBrain: Connected to BattleGameObject");

            InitializeBattleRosters();

            // Publish BattleGameObject; rosters and participants have already been initialized and populated by now.
            _brain?.PublishBattleObjectSet(BattleObject);

            _brain?.PublishBattleStarted();

            var allInstances = GetAllActiveInstances();
            foreach (var inst in allInstances)
            {
                if (inst != null)
                {
                    inst.LastAttackedTarget = null;
                    ClearLastAttacker(BattleObject?.Context, inst);
                }
            }

            // Clear central last-attacker mapping in the context
            BattleObject?.Context?.ClearLastAttackHistory();

            // Initialize advanced systems (commands, snapshots)
            // Clear any previous battle's command history
            _brain.Commands?.Clear();
            // Take initial snapshot of battle state
            _brain.TakeSnapshot();

            TurnrootLogger.Log("BattleBrain: Battle initialization complete");
            ProgressTurnOrder();
            return;
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
                    ClearLastAttacker(BattleObject?.Context, inst);
                }
            }

            // Clear central last-attacker mapping in the context
            _brain?.battleBrain?.BattleObject?.Context?.ClearLastAttackHistory();
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

            // Populate the battle context participants *before* spawning units so commands that run during spawn
            // can reliably find instances in the context (avoids race conditions where participants are empty).
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
            if (BattleObject == null || BattleObject.Context == null)
            {
                return OperationResult.Failure("BattleObject or BattleContext is null");
            }

            var context = BattleObject.Context;

            // Clear existing
            context.Participants.Targets.Clear();
            context.Participants.Allies.Clear();
            context.Participants.ThirdParty.Clear();

            // Populate from rosters (BattleBrain owns these)
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

            // 1. Spawn enemy units (iterate runtime placements)
            foreach (var p in enemyRoster.GetPlacements())
            {
                var characterData = p.CharacterData;
                var characterInstance = enemyRoster.GetInstanceFor(characterData);
                var placement = p;
                BattleObject.Context.SpawnAtPosition(characterInstance, placement.SpawnPosition);
                enemyRoster.SetOrder(characterData, placement.Order);
            }

            // 2. Spawn third-party units, if needed
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

            // 3. Spawn player team units
            foreach (var p in playerTeamRoster.GetPlacements())
            {
                var characterData = p.CharacterData;
                var characterInstance = playerTeamRoster.GetInstanceFor(characterData);
                var placement = p;
                BattleObject.Context.SpawnAtPosition(characterInstance, placement.SpawnPosition);
                playerTeamRoster.SetOrder(characterData, placement.Order);
            }

            BattleObject.Context.InvalidateUnitPositionCache();
        }

        #endregion

        #region Turn Management

        public void ProgressTurnOrder()
        {
            if (!_turnRotisserie.Progress())
            {
                TurnrootLogger.Log(
                    "BattleBrain: Failed to progress turn order!",
                    TurnrootLogger.LogLevel.Error
                );
                Debug.Break();
            }
        }

        #endregion

        #region Roster Management API
        public GenericRosterInstance InstantiateGenericRoster(
            GenericRoster roster,
            bool register = false
        ) => _brain?.gamewideContextBrain?.GetOrCreateGenericRoster(roster, register);

        public PlayerTeamRosterInstance InstantiatePlayerTeamRoster() =>
            _brain?.gamewideContextBrain?.GetOrCreatePlayerTeamRoster(_playerTeamRoster);

        public void RecallGenericRosters(List<GenericRoster> rosters) =>
            _brain?.gamewideContextBrain?.RecallGenericRosters(rosters);

        public CharacterInstance FindInstanceByTemplate(CharacterData template) =>
            _brain?.gamewideContextBrain?.FindInstanceByTemplate(template);

        public List<CharacterInstance> GetAllActiveInstances() =>
            _brain?.gamewideContextBrain?.GetAllActiveInstances();

        public void SaveUniqueCharacterProgress(CharacterInstance instance) =>
            _brain?.gamewideContextBrain?.SaveUniqueCharacterProgress(instance);

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
                unit.MapGridPosition = target;
                // publish both simple event and the advanced UnitMovedEvent for subscribers
                _brain?.PublishCharacterMoveCompleted(unit, newPoint);
                _brain?.PublishUnitMoved(unit, target);
                _brain?.Publish(new Events.UnitMovedEvent(unit, from, target));
            }
            return result.Success;
        }

        #endregion
    }
}
