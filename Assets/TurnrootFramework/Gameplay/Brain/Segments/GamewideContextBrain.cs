using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Gameplay.PlayerSettings;
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

        private CharacterPersistence _characterPersistence;

        // Track all active runtime roster instances by roster id
        private readonly System.Collections.Generic.Dictionary<
            string,
            object
        > _activeRosterInstances = new();

        public enum TamperPolicy
        {
            NotifyOnly,
            Reject,
            Replace,
        }

        [field: SerializeField]
        public TamperPolicy Policy { get; } = TamperPolicy.Replace;

        [HideInInspector]
        public GameplayPlayerSettings PlayerSettings { get; private set; }

        protected override EventPriority GetSubscriptionPriority() => EventPriority.High;

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
            _characterPersistence = new CharacterPersistence(_brain);

            // Initialize player settings
            InitializePlayerSettings();

            // Try to find the persistent player roster asset in Resources and recall it from LTM if present
            TryLoadAndRecallPersistentPlayerRoster();
        }

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
#if UNITY_EDITOR
                Debug.Log("GamewideContextBrain: Recalling existing persistent player roster");
#endif
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
            var persistent = Roster.PersistentPlayerRoster.Instance;
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
                // Decode saved DTO containing placements and (optionally) character snapshots
                var decode =
                    GamewideContextBrainHelpers.DecodeInstanceFromString<PlayerRosterSaveData>(
                        this,
                        encoded
                    );
                if (decode.Success && decode.Value != null)
                {
                    try
                    {
                        Debug.Log(
                            "GamewideContextBrain: Recalling persisted player roster from LTM."
                        );

                        var runtimeInstance = _rosterManager?.InstantiatePlayerTeamRoster(
                            GamewidePersistentPlayerRoster
                        );

                        if (runtimeInstance != null)
                        {
                            // Apply saved placements into runtime instance
                            runtimeInstance.ApplyDecodedPlacements(decode.Value.Placements);
                            Debug.Log(
                                $"GamewideContextBrain: Applied {decode.Value.Placements?.Length ?? 0} placements to runtime instance."
                            );
                        }
                    }
                    catch (System.Exception ex)
                    {
#if UNITY_EDITOR
                        Debug.LogWarning($"Failed to apply persisted player roster: {ex.Message}");
#endif
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
#if UNITY_EDITOR
                Debug.LogWarning("GamewideContextBrain: No persistent player roster to save.");
#endif
                return;
            }

            try
            {
                var runtimeInstance = _rosterManager?.GetPersistentPlayerRosterInstance();
                if (runtimeInstance != null)
                {
                    // Create serializable DTO from runtime instance
                    var saveData = new PlayerRosterSaveData
                    {
                        RosterId = GamewidePersistentPlayerRoster.Id,
                        Placements = runtimeInstance.GetPlacements(),
                        CharacterInstances = Enumerable.ToArray(runtimeInstance.Instances),
                    };

                    var encode = GamewideContextBrainHelpers.EncodeInstanceToString(this, saveData);
                    if (!encode.Success)
                    {
                        Debug.LogError(
                            $"GamewideContextBrain: Failed to encode player roster: {encode.Error}"
                        );
                        return;
                    }

                    var encoded = encode.Value;
                    var key = GamewideContextBrainHelpers.BuildRosterLedgerKey(
                        GamewidePersistentPlayerRoster.Id
                    );
                    var ltm = GetComponent<LongTermMemory>();
                    ltm?.Remember(key, encoded);

                    _rosterPersistence?.RegisterPlayerRoster(GamewidePersistentPlayerRoster);

#if UNITY_EDITOR
                    Debug.Log("GamewideContextBrain: Saved runtime player roster to LTM.");
#endif
                    return;
                }

#if UNITY_EDITOR
                Debug.LogWarning("GamewideContextBrain: No runtime instance available to save.");
#endif
            }
            catch (System.Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogError($"GamewideContextBrain: Save player roster failed: {ex.Message}");
#endif
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
            if (roster == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("Cannot get/create null roster");
#endif
                return null;
            }

            // Return tracked instance if exists
            if (_activeRosterInstances.TryGetValue(roster.Id, out var existing))
            {
                return existing as GenericRosterInstance;
            }

            var instance = _rosterManager.InstantiateGenericRoster(roster, register);
            if (instance != null)
            {
                _activeRosterInstances[roster.Id] = instance;
            }

            return instance;
        }

        /// <summary>
        /// Returns a runtime PlayerTeamRosterInstance for the provided persistent PlayerTeamRoster template.
        /// </summary>
        public PlayerTeamRosterInstance GetOrCreatePlayerTeamRoster(PlayerTeamRoster roster)
        {
            if (roster == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("Cannot get/create null player roster");
#endif
                return null;
            }

            if (_activeRosterInstances.TryGetValue(roster.Id, out var existing))
            {
                return existing as PlayerTeamRosterInstance;
            }

            var instance = _rosterManager.InstantiatePlayerTeamRoster(roster);
            if (instance != null)
            {
                _activeRosterInstances[roster.Id] = instance;
            }

            return instance;
        }

        /// <summary>
        /// Find an active CharacterInstance by template across all tracked rosters.
        /// </summary>
        public CharacterInstance FindInstanceByTemplate(CharacterData template) =>
            _rosterManager?.FindInstanceByTemplate(template);

        /// <summary>
        /// Return all active CharacterInstances from all tracked rosters.
        /// </summary>
        public System.Collections.Generic.List<CharacterInstance> GetAllActiveInstances() =>
            _rosterManager?.GetAllActiveInstances()
            ?? new System.Collections.Generic.List<CharacterInstance>();

        /// <summary>
        /// Persist a unique character's state via the centralized character persistence.
        /// </summary>
        public void SaveUniqueCharacterProgress(CharacterInstance instance) =>
            _characterPersistence?.SaveCharacter(instance, updateIndex: false);

        /// <summary>
        /// Delegates to roster manager to recall generic rosters from a list.
        /// </summary>
        public void RecallGenericRosters(System.Collections.Generic.List<GenericRoster> rosters) =>
            _rosterManager?.RecallGenericRosters(rosters);

        /// <summary>
        /// Delegates to roster manager to recall a player team roster and return the runtime instance.
        /// </summary>
        public PlayerTeamRosterInstance RecallPlayerTeamRoster(PlayerTeamRoster roster) =>
            _rosterManager?.RecallPlayerTeamRoster(roster);

        #endregion

        #region Player Settings Persistence

        private void InitializePlayerSettings()
        {
            PlayerSettings = GameplayPlayerSettings.Instance;
            if (PlayerSettings == null)
            {
#if UNITY_EDITOR
                Debug.LogError(
                    "GamewideContextBrain: Could not find GameplayPlayerSettings instance"
                );
#endif
                return;
            }

            // Load saved settings from LTM
            LoadPlayerSettingsFromLTM();
        }

        private void LoadPlayerSettingsFromLTM()
        {
            var ltm = GetComponent<LongTermMemory>();
            if (ltm == null)
                return;

            var settingsData = ltm.Recall("PlayerSettings");
            if (string.IsNullOrEmpty(settingsData))
                return;

            try
            {
                var decode =
                    GamewideContextBrainHelpers.DecodeInstanceFromString<PlayerSettingsSaveData>(
                        this,
                        settingsData
                    );
                if (decode.Success && decode.Value != null)
                {
                    ApplySettingsData(decode.Value);
#if UNITY_EDITOR
                    Debug.Log("GamewideContextBrain: Loaded player settings from LTM");
#endif
                }
            }
            catch (System.Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogError($"Failed to load player settings: {ex.Message}");
#endif
            }
        }

        private void ApplySettingsData(PlayerSettingsSaveData data)
        {
            if (PlayerSettings == null)
                return;

            PlayerSettings.TutorialPrompts = data.TutorialPrompts;
            PlayerSettings.GameDifficulty = data.GameDifficulty;
            PlayerSettings.Brightness = data.Brightness;
            PlayerSettings.Gamma = data.Gamma;
            PlayerSettings.Quality = data.Quality;
            PlayerSettings.Subtitles = data.Subtitles;
            PlayerSettings.Bloom = data.Bloom;
            PlayerSettings.DepthOfField = data.DepthOfField;
            PlayerSettings.AnimatedCameraMovement = data.AnimatedCameraMovement;
        }

        private void HandleSavePlayerSettingsRequested()
        {
            if (PlayerSettings == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("GamewideContextBrain: No player settings to save");
#endif
                return;
            }

            try
            {
                var saveData = new PlayerSettingsSaveData
                {
                    TutorialPrompts = PlayerSettings.TutorialPrompts,
                    GameDifficulty = PlayerSettings.GameDifficulty,
                    Brightness = PlayerSettings.Brightness,
                    Gamma = PlayerSettings.Gamma,
                    Quality = PlayerSettings.Quality,
                    Subtitles = PlayerSettings.Subtitles,
                    Bloom = PlayerSettings.Bloom,
                    DepthOfField = PlayerSettings.DepthOfField,
                    AnimatedCameraMovement = PlayerSettings.AnimatedCameraMovement,
                };

                var encode = GamewideContextBrainHelpers.EncodeInstanceToString(this, saveData);
                if (!encode.Success)
                {
                    Debug.LogError(
                        $"GamewideContextBrain: Failed to encode player settings: {encode.Error}"
                    );
                    return;
                }

                var ltm = GetComponent<LongTermMemory>();
                ltm?.Remember("PlayerSettings", encode.Value);

#if UNITY_EDITOR
                Debug.Log("GamewideContextBrain: Saved player settings to LTM");
#endif
            }
            catch (System.Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogError($"GamewideContextBrain: Save player settings failed: {ex.Message}");
#endif
            }
        }

        public void UpdatePlayerSetting(string settingName, object value)
        {
            if (PlayerSettings == null)
                return;

            try
            {
                switch (settingName.ToLower())
                {
                    case "tutorialprompts":
                        if (value is bool tutorialPrompts)
                            PlayerSettings.TutorialPrompts = tutorialPrompts;
                        break;
                    case "gamedifficulty":
                        if (value is GameplayPlayerSettings.DifficultyLevel difficulty)
                            PlayerSettings.GameDifficulty = difficulty;
                        break;
                    case "brightness":
                        if (value is float brightness)
                            PlayerSettings.Brightness = Mathf.Clamp01(brightness);
                        break;
                    case "gamma":
                        if (value is float gamma)
                            PlayerSettings.Gamma = Mathf.Clamp(gamma, 0.5f, 2.0f);
                        break;
                    case "quality":
                        if (value is float quality)
                            PlayerSettings.Quality = Mathf.Clamp01(quality);
                        break;
                    case "subtitles":
                        if (value is bool subtitles)
                            PlayerSettings.Subtitles = subtitles;
                        break;
                    case "bloom":
                        if (value is bool bloom)
                            PlayerSettings.Bloom = bloom;
                        break;
                    case "depthoffield":
                        if (value is bool depthOfField)
                            PlayerSettings.DepthOfField = depthOfField;
                        break;
                    case "animatedcameramovement":
                        if (value is bool animatedCamera)
                            PlayerSettings.AnimatedCameraMovement = animatedCamera;
                        break;
                    default:
#if UNITY_EDITOR
                        Debug.LogWarning($"Unknown setting: {settingName}");
#endif
                        return;
                }

                // Auto-save after each setting change
                HandleSavePlayerSettingsRequested();

#if UNITY_EDITOR
                Debug.Log($"Updated setting {settingName} to {value}");
#endif
            }
            catch (System.Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogError($"Failed to update setting {settingName}: {ex.Message}");
#endif
            }
        }

        #endregion
    }

    // Serializable DTO for player roster saves
    [System.Serializable]
    public class PlayerRosterSaveData
    {
        public string RosterId;
        public Turnroot.Characters.Roster.UnitPlacement[] Placements;
        public Turnroot.Characters.CharacterInstance[] CharacterInstances;
    }

    // Serializable DTO for player settings saves
    [System.Serializable]
    public class PlayerSettingsSaveData
    {
        public bool TutorialPrompts = true;
        public GameplayPlayerSettings.DifficultyLevel GameDifficulty = GameplayPlayerSettings
            .DifficultyLevel
            .Normal;
        public float Brightness = 1.0f;
        public float Gamma = 1.0f;
        public float Quality = 0.3f;
        public bool Subtitles = true;
        public bool Bloom = true;
        public bool DepthOfField = true;
        public bool AnimatedCameraMovement = true;
    }
}
