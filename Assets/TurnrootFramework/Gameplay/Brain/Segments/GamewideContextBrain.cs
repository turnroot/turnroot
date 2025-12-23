using Turnroot.Characters;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    [RequireComponent(typeof(LongTermMemory))]
    [RequireComponent(typeof(Brain))]
    public class GamewideContextBrain : BrainComponent
    {
        public Brain CentralBrain => _brain;

        private RosterPersistence _rosterPersistence;

        private RosterManager _rosterManager;

        public enum TamperPolicy
        {
            NotifyOnly,
            Reject,
            Replace,
        }

        [SerializeField]
        private TamperPolicy _tamperPolicy = TamperPolicy.Replace;
        public TamperPolicy Policy => _tamperPolicy;

        protected override void Awake()
        {
            _brain = GetComponent<Brain>();

            // Now subscribe to brain events
            Debug.Log(
                $"{GetType().Name} Awake - subscribing to brain events with priority {GetSubscriptionPriority()}."
            );
            SubscribeToBrainEvents();

            _rosterPersistence = new RosterPersistence(GetComponent<LongTermMemory>());
            _rosterManager = new RosterManager(_brain);
        }

        // TODO: Store instances outside of battle

        protected override void SubscribeToBrainEvents() { }

        protected override void UnsubscribeFromBrainEvents()
        {
            //  don't currently need to subscribe to anything
        }

        #region Persistent Player Roster

        [HideInInspector] // TODO: Set up an accessible editor
        public PlayerTeamRoster GamewidePersistentPlayerRoster { get; set; }

        public PlayerTeamRoster CreateOrRecallGamewidePersistentPlayerRoster()
        {
            if (_rosterPersistence.HasPlayerRosterInLTM(GamewidePersistentPlayerRoster))
            {
                Debug.Log("GamewideContextBrain: Recalling existing persistent player roster");
                var recalledRoster = _rosterManager.RecallPlayerTeamRoster();
                GamewidePersistentPlayerRoster = recalledRoster;
            }

            Debug.Log("GamewideContextBrain: Creating new persistent player roster");
            GamewidePersistentPlayerRoster = ScriptableObject.CreateInstance<PlayerTeamRoster>();
            GamewidePersistentPlayerRoster.name = "Gamewide Persistent Player Roster";
            _rosterPersistence.RegisterPlayerRoster(GamewidePersistentPlayerRoster);

            return GamewidePersistentPlayerRoster;
        }

        #endregion
    }
}
