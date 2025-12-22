using System;
using System.Collections.Generic;
using Turnroot.Characters;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    [RequireComponent(typeof(LongTermMemory))]
    /// <summary>
    /// Manages gamewide context within the game's brain system.
    /// Holds all instances and handles Data -> Instance conversions.
    /// </summary>
    // BRAIN: Coordinator only - delegates to specialized managers
    public class GamewideContextBrain : BrainComponent
    {
        public Brain CentralBrain => _brain;

        public enum TamperPolicy
        {
            NotifyOnly, // Log warning but use data
            Reject, // Return null/default
            Replace, // Create safe default instance
        }

        [SerializeField]
        private TamperPolicy _tamperPolicy = TamperPolicy.Replace;

        public TamperPolicy Policy => _tamperPolicy;

        // Managers handle specific responsibilities
        private RosterManager _rosterManager;
        private CharacterPersistence _characterPersistence;

        // Event handlers for EventBus
        private Action<CharacterInstance> _onCharacterLevelUpHandler;
        private Action<CharacterInstance> _onCharacterClassChangedHandler;
        private Action<CharacterInstance, Skill> _onCharacterLearnedSkillHandler;

        [Header("Rosters")]
        [SerializeField]
        private List<GenericRoster> _genericRosters = new();

        [SerializeField]
        private PlayerTeamRoster _playerTeamRoster;

        public IReadOnlyList<GenericRoster> ConfiguredGenericRosters => _genericRosters;

        protected override void Awake()
        {
            _brain = GetComponent<Brain>();
            if (_brain == null)
            {
                Debug.LogError($"{GetType().Name}: Brain component not found!");
                return;
            }

            // Initialize managers
            _rosterManager = new RosterManager(this, _brain);
            _characterPersistence = new CharacterPersistence(GetComponent<LongTermMemory>(), this);

            // Now subscribe to brain events
            Debug.Log(
                $"{GetType().Name} Awake - subscribing to brain events with priority {GetSubscriptionPriority()}."
            );
            SubscribeToBrainEvents();
        }

        protected override void SubscribeToBrainEvents()
        {
            _brain.OnRostersReady += _rosterManager.OnRostersReady;
            _onCharacterLevelUpHandler = (c) => _rosterManager.InvalidateCache();
            _brain.OnCharacterLevelUp += _onCharacterLevelUpHandler;
            _onCharacterClassChangedHandler = (c) => _rosterManager.InvalidateCache();
            _brain.OnCharacterClassChanged += _onCharacterClassChangedHandler;
            _onCharacterLearnedSkillHandler = (c, s) => _rosterManager.InvalidateCache();
            _brain.OnCharacterLearnedSkill += _onCharacterLearnedSkillHandler;
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            _brain.OnRostersReady -= _rosterManager.OnRostersReady;
            _brain.OnCharacterLevelUp -= _onCharacterLevelUpHandler;
            _brain.OnCharacterClassChanged -= _onCharacterClassChangedHandler;
            _brain.OnCharacterLearnedSkill -= _onCharacterLearnedSkillHandler;
        }

        public void Start()
        {
            _rosterManager.RecallGenericRosters(_genericRosters);
            _rosterManager.RecallPlayerRoster(_playerTeamRoster);
        }

        #region Public API - Delegates to managers

        public GenericRosterInstance InstantiateGenericRoster(
            GenericRoster roster,
            bool register = false
        ) => _rosterManager.InstantiateGenericRoster(roster, register);

        public void SaveUniqueCharacterProgress(CharacterInstance instance) =>
            _characterPersistence.SaveCharacter(instance, updateIndex: false);

        public CharacterInstance FindInstanceByTemplate(CharacterData template) =>
            _rosterManager.FindInstanceByTemplate(template);

        public List<CharacterInstance> GetAllActiveInstances() =>
            _rosterManager.GetAllActiveInstances();

        public PlayerTeamRosterInstance InstantiatePlayerTeamRoster() =>
            _rosterManager.InstantiatePlayerTeamRoster(_playerTeamRoster);

        #endregion
    }
}
