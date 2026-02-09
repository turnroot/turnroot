using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Gameplay.PlayerSettings;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Manages gamewide context including player roster, character persistence, and exploration state.
    /// Core logic is split into partial files in the GamewideContextBrainPartials subfolder.
    /// </summary>
    [RequireComponent(typeof(LongTermMemory))]
    [RequireComponent(typeof(Brain))]
    public partial class GamewideContextBrain : BrainComponent
    {
        #region Configuration
        public enum TamperPolicy
        {
            NotifyOnly,
            Reject,
            Replace,
        }

        [field: SerializeField]
        public TamperPolicy Policy { get; } = TamperPolicy.Replace;
        #endregion

        #region Dependencies
        public Brain CentralBrain => _brain;

        private LongTermMemory _ltm;
        private RosterPersistence _rosterPersistence;
        private RosterManager _rosterManager;
        private CharacterPersistence _characterPersistence;
        private PlayerSettingsPersistence _playerSettingsPersistence;
        #endregion

        #region State
        private readonly Dictionary<string, object> _activeRosterInstances = new();

        [HideInInspector]
        public PlayerTeamRoster GamewidePersistentPlayerRoster { get; set; }
        public List<GamewideContextBrainHelpers.ExploredPartial> MapExplorationStatuses
        {
            get;
            private set;
        }

        [HideInInspector]
        public GameplayPlayerSettings PlayerSettings => _playerSettingsPersistence?.PlayerSettings;
        #endregion

        #region Initialization
        protected override EventPriority GetSubscriptionPriority() => EventPriority.High;

        protected override void Awake()
        {
            _brain = GetComponent<Brain>();
            SubscribeToBrainEvents();

            _rosterPersistence = new RosterPersistence(GetComponent<LongTermMemory>());
            _rosterManager = new RosterManager(_brain, _rosterPersistence);
            _characterPersistence = new CharacterPersistence(_brain);
            _playerSettingsPersistence = new PlayerSettingsPersistence(
                GetComponent<LongTermMemory>(),
                this
            );

            MapExplorationStatuses = new List<GamewideContextBrainHelpers.ExploredPartial>();
        }

        private void Start()
        {
            // All Brain components are initialized by this point, safe to access
            _ltm = GetComponent<LongTermMemory>();
            _playerSettingsPersistence?.Initialize();
            TryLoadAndRecallPersistentPlayerRoster();
            _brain.volumeBrain?.ApplySettingsToVolumes(PlayerSettings);
            PopulateMapExplorationStatusesFromLtm();
        }
        #endregion

        #region Event Subscription
        protected override void SubscribeToBrainEvents() =>
            _brain.OnSavePlayerRosterRequested += HandleSavePlayerRosterRequested;

        protected override void UnsubscribeFromBrainEvents() =>
            _brain.OnSavePlayerRosterRequested -= HandleSavePlayerRosterRequested;
        #endregion

        // Remaining API methods are implemented in partial files within GamewideContextBrainPartials/
        // - Persistence.cs: Character and roster persistence methods
        // - RosterManagement.cs: Roster and character management API
        // - MapExploration.cs: Map exploration state management
        // - PlayerSettings.cs: Player settings management
    }

    [System.Serializable]
    public class PlayerRosterSaveData
    {
        public string RosterId;
        public Characters.Roster.UnitPlacement[] Placements;
        public CharacterInstance[] CharacterInstances;

        // Last saved battle turn number (0 = no battle saved, 1 = first turn, >1 ongoing)
        public int LastSavedBattleTurn = 0;
    }
}
