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

            // Try to find the persistent player roster asset in Resources and recall it from LTM if present
            TryLoadAndRecallPersistentPlayerRoster();
        }

        // TODO: Store instances outside of battle

        protected override void SubscribeToBrainEvents()
        {
            // Subscribe to save requests so we can persist roster changes triggered at runtime
            _brain.OnSavePlayerRosterRequested += HandleSavePlayerRosterRequested;
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            _brain.OnSavePlayerRosterRequested -= HandleSavePlayerRosterRequested;
        }

        #region Persistent Player Roster

        [HideInInspector]
        public PlayerTeamRoster GamewidePersistentPlayerRoster { get; set; }

        public PlayerTeamRoster CreateOrRecallGamewidePersistentPlayerRoster()
        {
            // Prefer an editor-created asset assigned to GamewidePersistentPlayerRoster.
            if (GamewidePersistentPlayerRoster == null)
            {
                Debug.LogWarning(
                    "GamewideContextBrain: No GamewidePersistentPlayerRoster assigned. Assign one in editor or implement an editor wrapper."
                );
                return null;
            }

            // If roster exists and is registered in LTM, recall it (this will create a runtime instance)
            if (
                _rosterPersistence != null
                && _rosterPersistence.HasPlayerRosterInLTM(GamewidePersistentPlayerRoster)
            )
            {
                Debug.Log("GamewideContextBrain: Recalling existing persistent player roster");
                _rosterManager?.RecallPlayerTeamRoster(GamewidePersistentPlayerRoster);
            }

            return GamewidePersistentPlayerRoster;
        }

        /// <summary>
        /// Attempts to find the `PersistentPlayerRoster` singleton asset in Resources and initialize
        /// the gamewide persistent player roster from it. If LTM contains a serialized copy of the
        /// roster, the persisted payload is decoded and applied before instantiation.
        /// </summary>
        private void TryLoadAndRecallPersistentPlayerRoster()
        {
            // Try graceful load via singleton accessor if available
            var persistent = Turnroot.Gameplay.Roster.PersistentPlayerRoster.Instance;
            if (persistent == null)
            {
                return; // nothing to do
            }

            GamewidePersistentPlayerRoster = persistent.PlayerRoster;

            if (GamewidePersistentPlayerRoster == null)
            {
                Debug.LogWarning(
                    "GamewideContextBrain: PersistentPlayerRoster.asset has no PlayerRoster assigned."
                );
                return;
            }

            // Attempt to recall persisted roster payload from LTM and apply it to the asset
            var key = GamewideContextBrainHelpers.BuildRosterLedgerKey(
                GamewidePersistentPlayerRoster.Id
            );
            var ltm = GetComponent<LongTermMemory>();
            var encoded = ltm?.Recall(key);

            if (!string.IsNullOrEmpty(encoded))
            {
                var decode = GamewideContextBrainHelpers.DecodeInstanceFromString<PlayerTeamRoster>(
                    this,
                    encoded
                );
                if (decode.Success && decode.Value != null)
                {
                    try
                    {
                        // Do NOT modify the ScriptableObject asset at runtime.
                        // Instead, instantiate a runtime team roster and apply the decoded payload to it.
                        Debug.Log(
                            "GamewideContextBrain: Recalling persisted player roster from LTM into runtime instance."
                        );
                        var runtimeInstance = _rosterManager?.InstantiatePlayerTeamRoster(
                            GamewidePersistentPlayerRoster
                        );
                        if (runtimeInstance != null)
                        {
                            _rosterManager?.ApplyDecodedPlayerRoster(runtimeInstance, decode.Value);
                            Debug.Log(
                                "GamewideContextBrain: Applied persisted player roster to runtime instance."
                            );
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning(
                            $"Failed to apply persisted player roster to runtime instance: {ex.Message}"
                        );
                    }
                }
            }

            // Ensure there is a runtime instance for the persistent player roster (it will register if needed)
            _rosterManager?.RecallPlayerTeamRoster(GamewidePersistentPlayerRoster);
        }

        private void HandleSavePlayerRosterRequested()
        {
            if (GamewidePersistentPlayerRoster == null)
            {
                Debug.LogWarning("GamewideContextBrain: No persistent player roster to save.");
                return;
            }

            try
            {
                // Prefer to encode the runtime instance (so runtime ordering and selections are preserved)
                var runtimeInstance = _rosterManager?.GetPersistentPlayerRosterInstance();
                if (runtimeInstance != null)
                {
                    var encode = GamewideContextBrainHelpers.EncodeInstanceToString(
                        this,
                        runtimeInstance
                    );
                    if (!encode.Success)
                    {
                        Debug.LogError(
                            $"GamewideContextBrain: Failed to encode runtime player roster: {encode.Error}"
                        );
                        return;
                    }

                    var encoded = encode.Value;
                    var key = GamewideContextBrainHelpers.BuildRosterLedgerKey(
                        GamewidePersistentPlayerRoster.Id
                    );
                    var ltm = GetComponent<LongTermMemory>();
                    ltm?.Remember(key, encoded);

                    // Ensure roster index/hash registration
                    _rosterPersistence?.RegisterPlayerRoster(GamewidePersistentPlayerRoster);

                    Debug.Log("GamewideContextBrain: Saved runtime player roster to LTM.");
                    return;
                }

                // Fallback: no runtime instance available, encode the template (rare)
                var fallbackEncode = GamewideContextBrainHelpers.EncodeInstanceToString(
                    this,
                    GamewidePersistentPlayerRoster
                );
                if (!fallbackEncode.Success)
                {
                    Debug.LogError(
                        $"GamewideContextBrain: Failed to encode player roster: {fallbackEncode.Error}"
                    );
                    return;
                }

                var fallbackEncoded = fallbackEncode.Value;
                var fallbackKey = GamewideContextBrainHelpers.BuildRosterLedgerKey(
                    GamewidePersistentPlayerRoster.Id
                );
                var fallbackLtm = GetComponent<LongTermMemory>();
                fallbackLtm?.Remember(fallbackKey, fallbackEncoded);

                _rosterPersistence?.RegisterPlayerRoster(GamewidePersistentPlayerRoster);

                Debug.Log(
                    "GamewideContextBrain: Saved persistent player roster to LTM (fallback template encoded)."
                );
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"GamewideContextBrain: Save player roster failed: {ex.Message}");
            }
        }

        #endregion

        #region Roster Manager Facade

        /// <summary>
        /// Returns a runtime GenericRosterInstance for the provided template. GamewideContextBrain
        /// owns and tracks persistent runtime rosters; callers should request, not create.
        /// </summary>
        public GenericRosterInstance GetOrCreateGenericRoster(
            GenericRoster roster,
            bool register = false
        )
        {
            return _rosterManager.InstantiateGenericRoster(roster, register);
        }

        /// <summary>
        /// Returns a runtime PlayerTeamRosterInstance for the provided persistent PlayerTeamRoster template.
        /// </summary>
        public PlayerTeamRosterInstance GetOrCreatePlayerTeamRoster(PlayerTeamRoster roster)
        {
            return _rosterManager.InstantiatePlayerTeamRoster(roster);
        }

        #endregion
    }
}
