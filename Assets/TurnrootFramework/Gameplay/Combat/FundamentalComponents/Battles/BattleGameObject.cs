using Turnroot.Characters;
using Turnroot.Characters;
using Turnroot.Characters.Components;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Environment;
using UnityEngine;

namespace Turnroot.Gameplay.Combat
{
    public enum BattleExitType
    {
        Victory,
        Defeat,
        Retreat,
        Bookmark,
    }

    [RequireComponent(typeof(EnvironmentalConditions))]
    public class BattleGameObject : MonoBehaviour
    {
        public bool HasThirdParty;

        [Header("Battle Components")]
        [SerializeField]
        private BattleContext _battleContext;

        [SerializeField, SerializeReference]
        private BattleCondition[] _battleConditions;

        [SerializeField]
        private MapGrid _mapGrid;

        [SerializeField, NaughtyAttributes.ReadOnly]
        private int _currentTurnCount;

        [HideInInspector]
        private Brain.Brain _brain;
        public Brain.Brain Brain
        {
            get => _brain;
            set => _brain = value;
        }

        // Battle rosters - temporary for this battle only
        private RosterInstance _playerTeamRoster;
        private RosterInstance _enemyTeamRoster;
        private RosterInstance _thirdPartyTeamRoster;

        public RosterInstance PlayerTeamRoster => _playerTeamRoster;
        public RosterInstance EnemyTeamRoster => _enemyTeamRoster;
        public RosterInstance ThirdPartyTeamRoster => _thirdPartyTeamRoster;

        public void ConnectToBrainEvents()
        {
            if (_brain != null)
            {
                Debug.Log("BattleGameObject connecting to Brain events.");
                // TODO: Subscribe to relevant Brain events here
            }
            else
            {
                Debug.LogWarning("BattleGameObject has no Brain to connect to.");
            }
        }

        public void ConnectBattleConditionsToGamewideContextBrain()
        {
            if (_brain == null || _brain.gamewideContextBrain == null)
            {
                Debug.LogError(
                    "BattleGameObject cannot connect BattleConditions: Brain or GamewideContextBrain is null."
                );
                Debug.Break();
                return;
            }

            foreach (var condition in _battleConditions)
            {
                condition.gamewideContextBrain = _brain.gamewideContextBrain;
            }
        }

        public void Awake()
        {
            ResetTurnCount();
            _battleContext ??= new BattleContext();
            _mapGrid = _mapGrid != null ? _mapGrid : GetComponentInChildren<MapGrid>();
            if (_mapGrid == null)
            {
                Debug.LogError("BattleGameObject requires a MapGrid child.");
                Debug.Break();
            }
            if (_battleConditions == null)
            {
                Debug.LogError("BattleGameObject requires BattleConditions to be set.");
                Debug.Break();
            }
        }

        public void IncrementTurnCount() => _currentTurnCount++;

        public void ResetTurnCount() => _currentTurnCount = 0;

        public int Turns() => _currentTurnCount;

        #region Battle Roster Management

        /// <summary>
        /// Initialize the three temporary battle rosters.
        /// </summary>
        public void InitializeBattleRosters()
        {
            // Create or clear player roster
            if (_playerTeamRoster == null)
            {
                var go = new GameObject("BattleRoster - Player Team");
                go.transform.SetParent(transform);
                _playerTeamRoster = go.AddComponent<RosterInstance>();
            }
            else
            {
                _playerTeamRoster.Clear();
            }

            // Create or clear enemy roster
            if (_enemyTeamRoster == null)
            {
                var go = new GameObject("BattleRoster - Enemy Team");
                go.transform.SetParent(transform);
                _enemyTeamRoster = go.AddComponent<RosterInstance>();
            }
            else
            {
                _enemyTeamRoster.Clear();
            }

            // Create or clear third party roster
            if (_thirdPartyTeamRoster == null)
            {
                var go = new GameObject("BattleRoster - Third Party Team");
                go.transform.SetParent(transform);
                _thirdPartyTeamRoster = go.AddComponent<RosterInstance>();
            }
            else
            {
                _thirdPartyTeamRoster.Clear();
            }

            Debug.Log("BattleGameObject: Initialized three temporary battle rosters.");
        }

        /// <summary>
        /// Populate battle rosters with characters sorted by faction.
        /// </summary>
        public void PopulateBattleRostersFromGamewideContext(Brain.GamewideContextBrain gwcb)
        {
            if (gwcb == null)
            {
                Debug.LogWarning(
                    "BattleGameObject: Cannot populate battle rosters - GamewideContextBrain is null."
                );
                return;
            }

            var allCharacters = gwcb.GetAllActiveInstances();
            int playerCount = 0;
            int enemyCount = 0;
            int thirdPartyCount = 0;

            foreach (var character in allCharacters)
            {
                if (character?.CharacterTemplate?.Which == null)
                {
                    continue;
                }

                string faction = character.CharacterTemplate.Which.Value;

                if (faction == CharacterWhich.ALLY || faction == CharacterWhich.AVATAR)
                {
                    _playerTeamRoster.AddInstance(character);
                    playerCount++;
                }
                else if (faction == CharacterWhich.ENEMY)
                {
                    _enemyTeamRoster.AddInstance(character);
                    enemyCount++;
                }
                else if (faction == CharacterWhich.NPC)
                {
                    _thirdPartyTeamRoster.AddInstance(character);
                    thirdPartyCount++;
                }
            }

            Debug.Log(
                $"BattleGameObject: Populated battle rosters - Player: {playerCount}, Enemy: {enemyCount}, Third Party: {thirdPartyCount}"
            );
        }

        /// <summary>
        /// Add a character instance to the appropriate battle roster based on faction.
        /// </summary>
        public void AddCharacterToBattleRoster(CharacterInstance character)
        {
            if (character?.CharacterTemplate?.Which == null)
            {
                Debug.LogWarning(
                    "BattleGameObject: Cannot add character to battle roster - invalid character or faction."
                );
                return;
            }

            string faction = character.CharacterTemplate.Which.Value;

            if (faction == CharacterWhich.ALLY || faction == CharacterWhich.AVATAR)
            {
                _playerTeamRoster?.AddInstance(character);
            }
            else if (faction == CharacterWhich.ENEMY)
            {
                _enemyTeamRoster?.AddInstance(character);
            }
            else if (faction == CharacterWhich.NPC)
            {
                _thirdPartyTeamRoster?.AddInstance(character);
            }
        }

        /// <summary>
        /// Clear all three temporary battle rosters.
        /// </summary>
        public void ClearBattleRosters()
        {
            _playerTeamRoster?.Clear();
            _enemyTeamRoster?.Clear();
            _thirdPartyTeamRoster?.Clear();

            Debug.Log("BattleGameObject: Cleared all temporary battle rosters.");
        }

        #endregion
    }
}
