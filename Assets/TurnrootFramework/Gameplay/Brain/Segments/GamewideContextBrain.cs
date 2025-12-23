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
            // Pass persistence into RosterManager so it can register/recall rosters
            _rosterManager = new RosterManager(_brain, _rosterPersistence);
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
            // Prefer an editor-created asset assigned to GamewidePersistentPlayerRoster.
            // DO NOT create PlayerTeamRoster assets at runtime. If none is assigned, warn and exit.
            // TODO: Expose an editor wrapper to create/configure the persistent player roster asset.
            if (GamewidePersistentPlayerRoster == null)
            {
                Debug.LogWarning(
                    "GamewideContextBrain: No GamewidePersistentPlayerRoster assigned. Assign one in editor or implement an editor wrapper."
                );
                return null;
            }

            // If roster exists and is registered in LTM, recall it (this will create a runtime instance)
            if (_rosterPersistence.HasPlayerRosterInLTM(GamewidePersistentPlayerRoster))
            {
                Debug.Log("GamewideContextBrain: Recalling existing persistent player roster");
                _rosterManager?.RecallPlayerTeamRoster(GamewidePersistentPlayerRoster);
            }

            return GamewidePersistentPlayerRoster;
        }

        #endregion
    }
}
