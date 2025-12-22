using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Turnroot.Gameplay.Brain
{
    [RequireComponent(typeof(TurnRotisserie))]
    /// <summary>
    /// The battle brain manages one battle at a time.
    /// Responsible for initializing battles and managing turn order.
    /// </summary>
    public partial class BattleBrain : BrainComponent
    {
        [SerializeField]
        private List<GenericRoster> _genericRosters;

        [SerializeField]
        private PlayerTeamRoster _playerTeamRoster;
        private TurnRotisserie _turnRotisserie;

        private BattleContextAIHelper _aiHelper;

        public BattleGameObject BattleObject { get; private set; }

        // Roster accessors through BattleGameObject
        public PlayerTeamRosterInstance PlayerTeamRoster =>
            BattleObject != null ? BattleObject.PlayerTeamRoster : null;
        public GenericRosterInstance EnemyTeamRoster =>
            BattleObject != null ? BattleObject.EnemyTeamRoster : null;
        public GenericRosterInstance ThirdPartyTeamRoster =>
            BattleObject != null ? BattleObject.ThirdPartyTeamRoster : null;

        private RosterManager _rosterManager;
        private CharacterPersistence _characterPersistence;

        protected override void Awake()
        {
            base.Awake();

            _turnRotisserie = GetComponent<TurnRotisserie>();

            Debug.Log("BattleBrain: TurnRotisserie ready");

            _rosterManager = new RosterManager(_brain);
            _characterPersistence = new CharacterPersistence(_brain);
        }

        private void Start()
        {
            _rosterManager.RecallGenericRosters(_genericRosters);
            _rosterManager.InstantiatePlayerTeamRoster(_playerTeamRoster);
        }

        #region Roster Management API

        public GenericRosterInstance InstantiateGenericRoster(
            GenericRoster roster,
            bool register = false
        ) => _rosterManager.InstantiateGenericRoster(roster, register);

        public CharacterInstance FindInstanceByTemplate(CharacterData template) =>
            _rosterManager.FindInstanceByTemplate(template);

        public List<CharacterInstance> GetAllActiveInstances() =>
            _rosterManager.GetAllActiveInstances();

        public PlayerTeamRosterInstance InstantiatePlayerTeamRoster() =>
            _rosterManager.InstantiatePlayerTeamRoster(_playerTeamRoster);

        public void RecallGenericRosters(List<GenericRoster> rosters) =>
            _rosterManager.RecallGenericRosters(rosters);

        public void SaveUniqueCharacterProgress(CharacterInstance instance) =>
            _characterPersistence.SaveCharacter(instance, updateIndex: false);

        #endregion

        public void ProgressTurnOrder()
        {
            if (!_turnRotisserie.Progress())
            {
                Debug.LogError("BattleBrain: Failed to progress turn order!");
                Debug.Break();
            }
        }

        #region Battle Initialization

        public void HandleStartBattle()
        {
            Debug.Log("BattleBrain: Handling StartBattle event");

            BattleObject = FindBattleGameObjectInScene();

            if (BattleObject == null)
            {
                Debug.LogError("BattleBrain: No BattleGameObject found in any loaded scene!");
                return;
            }

            // Connect systems
            BattleObject.Brain = _brain;
            BattleObject.ConnectToBrainEvents();
            BattleObject.ConnectBattleConditionsToContext();

            Debug.Log($"BattleBrain: Connected to BattleGameObject");

            // Initialize battle using roster system
            InitializeBattleRosters();

            // Initialize advanced systems (commands, snapshots)
            // Clear any previous battle's command history
            _brain.Commands?.Clear();
            // Take initial snapshot of battle state
            _brain.TakeSnapshot();

            Debug.Log("BattleBrain: Battle initialization complete");
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
                        Debug.Log($"BattleBrain: Found BattleGameObject in scene '{scene.name}'");
                        return battleObj;
                    }
                }
            }

            Debug.LogWarning("BattleBrain: No BattleGameObject found in loaded scenes");
            return null;
        }

        private void InitializeBattleRosters()
        {
            // 1. Create empty runtime roster instances
            BattleObject.InitializeBattleRosters();

            // 2. Populate rosters from templates and persistent data
            var result = BattleObject.PopulateBattleRostersFromTemplates();
            if (!result.Success)
            {
                Debug.LogError($"Failed to populate battle rosters: {result.ErrorMessage}");
            }

            SpawnRosterUnitsOntoGrid();

            _aiHelper = new BattleContextAIHelper(BattleObject.Context);
        }

        private void SpawnRosterUnitsOntoGrid()
        {
            var enemyRoster = BattleObject.EnemyTeamRoster;
            if (BattleObject.HasThirdParty)
            {
                var thirdPartyRoster = BattleObject.ThirdPartyTeamRoster;
            }
            var playerTeamRoster = BattleObject.PlayerTeamRoster;
            // 1. Spawn enemy units
            foreach (var c in enemyRoster.roster.characters)
            {
                var characterData = c.CharacterData;
                var characterInstance = enemyRoster.GetInstanceFor(characterData);
                var placement = enemyRoster.GetPlacementFor(characterData);
                BattleObject.Context.SpawnAtPosition(characterInstance, placement.SpawnPosition);
                enemyRoster.SetOrder(characterData, placement.Order);
            }
            // 2. Spawn third-party units, if needed
            if (BattleObject.HasThirdParty)
            {
                var thirdPartyRoster = BattleObject.ThirdPartyTeamRoster;
                foreach (var c in thirdPartyRoster.roster.characters)
                {
                    var characterData = c.CharacterData;
                    var characterInstance = thirdPartyRoster.GetInstanceFor(characterData);
                    var placement = thirdPartyRoster.GetPlacementFor(characterData);
                    BattleObject.Context.SpawnAtPosition(
                        characterInstance,
                        placement.SpawnPosition
                    );
                    thirdPartyRoster.SetOrder(characterData, placement.Order);
                }
            }
            // 3. Spawn player team units
            foreach (var c in playerTeamRoster.roster.characters)
            {
                var characterData = c.CharacterData;
                var characterInstance = playerTeamRoster.GetInstanceFor(characterData);
                var placement = playerTeamRoster.GetPlacementFor(characterData);
                BattleObject.Context.SpawnAtPosition(characterInstance, placement.SpawnPosition);
                playerTeamRoster.SetOrder(characterData, placement.Order);
            }
        }

        #endregion

        #region Battle Cleanup

        private void HandleExitBattle(BattleExitType exitType)
        {
            Debug.Log($"BattleBrain: Handling ExitBattle event with type: {exitType}");
            if (exitType != BattleExitType.Bookmark)
            {
                _brain.Commands?.Clear();
                _brain.Snapshots?.Clear();
            }
            _brain.battleBrain.BattleObject.ClearBattleRosters();
            Debug.Log("BattleBrain: Battle cleanup complete");
        }

        #endregion
    }
}
