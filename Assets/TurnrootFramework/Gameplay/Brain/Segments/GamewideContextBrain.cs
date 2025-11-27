using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Turnroot.Characters;
using Newtonsoft.Json;
using Turnroot.Characters;
using UnityEngine;

namespace Assets.Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Manages gamewide context within the game's brain system.
    /// Holds all instances and handles Data -> Instance conversions.
    /// Encodes and decodes instances to/from strings for LongTermMemory storage.
    /// </summary>
    [RequireComponent(typeof(Brain))]
    [RequireComponent(typeof(LongTermMemory))]
    public class GamewideContextBrain : MonoBehaviour
    {
        private static class LtmKeys
        {
            public const string RosterIndex = "GWB.Roster.Index";
            public const string UniqueCharacterIndex = "GWB.UniqueCharacter.Index";
        }

        public enum TamperPolicy
        {
            NotifyOnly = 0,
            Reject = 1,
            Replace = 2,
        }

        [Header("Rosters")]
        [SerializeField]
        private List<Roster> rosters = new List<Roster>();
        public IReadOnlyList<Roster> ConfiguredRosters => rosters;

        [Header("Tamper Detection")]
        [Tooltip(
            "Policy that controls what happens when an encoded payload fails the integrity check."
        )]
        [SerializeField]
        private TamperPolicy tamperPolicy = TamperPolicy.Replace;

        public TamperPolicy Policy
        {
            get => tamperPolicy;
            set => tamperPolicy = value;
        }

        #region Initialization

        public void Start()
        {
            RecallRosters();
        }

        #endregion

        #region Roster Instantiation

        /// <summary>
        /// Instantiate runtime CharacterInstance objects for the provided Roster.
        /// </summary>
        public RosterInstance InstantiateRoster(Roster roster, bool registerGlobally = false)
        {
            if (roster == null || roster.characters == null)
            {
                Debug.LogWarning("InstantiateRoster called with null roster or characters.");
                GetComponent<Brain>()
                    ?.PublishRosterFailed(null, "Invalid roster or empty characters");
                return null;
            }

            var existing = FindExistingRosterInstance(roster);
            if (existing != null)
            {
                return HandleExistingRoster(existing, roster, registerGlobally);
            }

            return CreateNewRosterInstance(roster, registerGlobally);
        }

        private RosterInstance FindExistingRosterInstance(Roster roster)
        {
            return FindObjectsByType<RosterInstance>(FindObjectsSortMode.None)
                .FirstOrDefault(r => r != null && r.roster == roster);
        }

        private RosterInstance HandleExistingRoster(
            RosterInstance existing,
            Roster roster,
            bool registerGlobally
        )
        {
            var brain = GetComponent<Brain>();

            if (HasAnyInstancesPopulated(existing, roster))
            {
                Debug.Log(
                    $"InstantiateRoster: RosterInstance already exists and contains instances for roster '{roster.name}'. Doing nothing."
                );
                brain?.PublishRosterFailed(
                    roster,
                    "RosterInstance already exists and is populated"
                );
                return existing;
            }

            PopulateRosterInstance(existing, roster);

            if (registerGlobally)
            {
                RegisterRosterInLTM(roster);
            }

            brain?.PublishRosterReady(existing);
            return existing;
        }

        private bool HasAnyInstancesPopulated(RosterInstance rosterInstance, Roster roster)
        {
            foreach (var cd in roster.characters)
            {
                if (cd != null && rosterInstance.GetInstanceFor(cd) != null)
                    return true;
            }
            return false;
        }

        private void PopulateRosterInstance(RosterInstance rosterInstance, Roster roster)
        {
            var createdInstances = new List<CharacterInstance>();

            foreach (var characterData in roster.characters)
            {
                if (characterData == null)
                    continue;

                CharacterInstance inst;

                // For unique characters, try to load from LTM first
                if (characterData.IsUnique)
                {
                    inst = RecallUniqueCharacter(characterData);
                    if (inst == null)
                    {
                        // Create new unique instance and save it
                        inst = CharacterInstance.Create(characterData);
                        if (inst != null)
                        {
                            SaveUniqueCharacter(inst);
                        }
                    }
                }
                else
                {
                    // Non-unique characters are always created fresh
                    inst = CharacterInstance.Create(characterData);
                }

                if (inst != null)
                {
                    createdInstances.Add(inst);
                }
            }

            rosterInstance.AddInstances(createdInstances);
            Debug.Log(
                $"InstantiateRoster: Registered {createdInstances.Count} instances into existing RosterInstance '{rosterInstance.name}'."
            );
        }

        private RosterInstance CreateNewRosterInstance(Roster roster, bool registerGlobally)
        {
            var go = new GameObject($"RosterInstance - {roster.name}");
            var newRi = go.AddComponent<RosterInstance>();
            newRi.roster = roster;

#if UNITY_EDITOR
            newRi.InitializeFromRoster(roster);
            // Save unique characters after roster initialization
            SaveUniqueCharactersInRoster(newRi);
#else
            PopulateRosterInstanceAtRuntime(newRi, roster);
#endif

            Debug.Log(
                $"InstantiateRoster: Created new RosterInstance '{go.name}' with {newRi.Instances.Count} instances."
            );

            if (registerGlobally)
            {
                RegisterRosterInLTM(roster);
            }

            GetComponent<Brain>()?.PublishRosterReady(newRi);
            return newRi;
        }

        private void PopulateRosterInstanceAtRuntime(RosterInstance rosterInstance, Roster roster)
        {
            var createdInstances = new List<CharacterInstance>();

            foreach (var characterData in roster.characters)
            {
                if (characterData == null)
                    continue;

                CharacterInstance inst;

                // For unique characters, try to load from LTM first
                if (characterData.IsUnique)
                {
                    inst = RecallUniqueCharacter(characterData);
                    if (inst == null)
                    {
                        inst = CharacterInstance.Create(characterData);
                        if (inst != null)
                        {
                            SaveUniqueCharacter(inst);
                        }
                    }
                }
                else
                {
                    inst = CharacterInstance.Create(characterData);
                }

                if (inst != null)
                {
                    createdInstances.Add(inst);
                }
            }

            rosterInstance.AddInstances(createdInstances);
        }

        private void SaveUniqueCharactersInRoster(RosterInstance rosterInstance)
        {
            foreach (var inst in rosterInstance.Instances)
            {
                if (inst?.CharacterTemplate?.IsUnique == true)
                {
                    SaveUniqueCharacter(inst);
                }
            }
        }

        #endregion

        #region Unique Character Persistence

        /// <summary>
        /// Saves a unique character instance to LongTermMemory with tamper detection.
        /// </summary>
        private void SaveUniqueCharacter(CharacterInstance instance)
        {
            if (instance?.CharacterTemplate == null || !instance.CharacterTemplate.IsUnique)
                return;

            try
            {
                var encoded = EncodeInstanceToString(instance);
                var ltm = GetComponent<LongTermMemory>();

                // Add to unique character index
                var indexJson = ltm.Recall(LtmKeys.UniqueCharacterIndex);
                var index = string.IsNullOrEmpty(indexJson)
                    ? new List<string>()
                    : JsonConvert.DeserializeObject<List<string>>(indexJson);

                var templateName = instance.CharacterTemplate.name;
                if (!index.Contains(templateName))
                {
                    index.Add(templateName);
                    ltm.Remember(LtmKeys.UniqueCharacterIndex, JsonConvert.SerializeObject(index));
                }

                Debug.Log(
                    $"Saved unique character: {instance.CharacterTemplate.DisplayName} (template: {templateName})"
                );
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to save unique character: {ex.Message}");
            }
        }

        /// <summary>
        /// Recalls a unique character instance from LongTermMemory.
        /// Returns null if not found or if tamper detection fails.
        /// </summary>
        private CharacterInstance RecallUniqueCharacter(CharacterData characterData)
        {
            if (characterData == null || !characterData.IsUnique)
                return null;

            try
            {
                var ltm = GetComponent<LongTermMemory>();
                var key = BuildUniqueCharacterKey(characterData);
                var encoded = ltm.Recall(key);

                if (string.IsNullOrEmpty(encoded))
                    return null;

                var instance = DecodeInstanceFromString<CharacterInstance>(encoded);

                if (instance != null)
                {
                    Debug.Log(
                        $"Recalled unique character: {characterData.DisplayName} (template: {characterData.name})"
                    );
                }

                return instance;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"Failed to recall unique character {characterData.DisplayName}: {ex.Message}"
                );
                return null;
            }
        }

        private string BuildUniqueCharacterKey(CharacterData characterData)
        {
            // Use template asset name as deterministic key
            return $"GWB.UniqueCharacter.{characterData.name}";
        }

        /// <summary>
        /// Public API to save a unique character's current state.
        /// Call this when you want to persist character progression (level, stats, inventory, etc.)
        /// </summary>
        public void SaveUniqueCharacterProgress(CharacterInstance instance)
        {
            if (instance?.CharacterTemplate == null)
            {
                Debug.LogWarning("Cannot save null character instance.");
                return;
            }

            if (!instance.CharacterTemplate.IsUnique)
            {
                Debug.LogWarning(
                    $"Cannot save non-unique character {instance.CharacterTemplate.DisplayName}. Only unique characters are persisted."
                );
                return;
            }

            SaveUniqueCharacter(instance);
        }

        #endregion

        #region Roster Registration

        private void RegisterRosterInLTM(Roster roster)
        {
            try
            {
                var ltm = GetComponent<LongTermMemory>();
                if (ltm == null)
                    return;

                var key = GamewideContextBrainHelpers.BuildRosterLedgerKey(roster.Id);

                if (string.IsNullOrEmpty(key))
                    return;

                // Check if already registered
                var existingHash = ltm.Recall(key);
                if (!string.IsNullOrEmpty(existingHash))
                {
                    Debug.Log($"Roster {roster.name} already registered in LTM, skipping.");
                    return;
                }

                var encoded = EncodeRosterToStringNoLedger(roster);
                var wrapper = GamewideContextBrainHelpers.DecodeWrapperFromBase64(encoded);

                ltm.Remember(key, wrapper?.Hash);
                UpdateRosterIndex(ltm, roster.Id);

                Debug.Log($"Roster {roster.name} registered in LTM with key: {key}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to register roster in LTM: {ex.Message}");
            }
        }

        private string EncodeRosterToStringNoLedger(Roster roster)
        {
            var settings = GamewideContextBrainHelpers.GetJsonSerializerSettings();
            var payload = JsonConvert.SerializeObject(roster, settings);
            var versionHex = DateTime.UtcNow.Ticks.ToString("x16");

            var wrapper = new GamewideContextBrainHelpers.SerializedWrapper
            {
                TypeName = typeof(Roster).FullName,
                Payload = payload,
                Hash = GamewideContextBrainHelpers.ComputeFNV1a64Hex(payload + "|v:" + versionHex),
                Version = versionHex,
            };

            var wrapperJson = JsonConvert.SerializeObject(wrapper, Formatting.None);
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(wrapperJson));
        }

        private void UpdateRosterIndex(LongTermMemory ltm, string rosterId)
        {
            try
            {
                var indexJson = ltm.Recall(LtmKeys.RosterIndex);
                var index = string.IsNullOrEmpty(indexJson)
                    ? new List<string>()
                    : JsonConvert.DeserializeObject<List<string>>(indexJson);

                if (!index.Contains(rosterId))
                {
                    index.Add(rosterId);
                    ltm.Remember(LtmKeys.RosterIndex, JsonConvert.SerializeObject(index));
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to update roster index: {ex.Message}");
            }
        }

        #endregion

        #region Roster Recall

        private void RecallRosters()
        {
            var brain = GetComponent<Brain>();
            var ltm = GetComponent<LongTermMemory>();

            if (ltm == null || brain == null)
                return;

            var allRosters = rosters?.Where(r => r != null).ToArray() ?? new Roster[0];

            if (allRosters.Length == 0)
            {
                Debug.LogWarning(
                    "GamewideContextBrain: No configured rosters found in the inspector - nothing to recall or register."
                );
                return;
            }

            var indexJson = ltm.Recall(LtmKeys.RosterIndex);

            if (!string.IsNullOrEmpty(indexJson))
            {
                RecallRostersFromIndex(allRosters, indexJson);
            }
            else
            {
                RegisterAllRosters(allRosters);
            }
        }

        private void RecallRostersFromIndex(Roster[] allRosters, string indexJson)
        {
            try
            {
                var idList = JsonConvert.DeserializeObject<List<string>>(indexJson);
                if (idList == null)
                    return;

                foreach (var id in idList)
                {
                    var roster = Array.Find(allRosters, r => r != null && r.Id == id);
                    if (roster != null && HasRosterInLTM(roster))
                    {
                        InstantiateRoster(roster, registerGlobally: false);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"RecallRosters: failed to parse roster index: {ex.Message}");
            }
        }

        private bool HasRosterInLTM(Roster roster)
        {
            var ltm = GetComponent<LongTermMemory>();
            if (ltm == null)
                return false;

            var key = GamewideContextBrainHelpers.BuildRosterLedgerKey(roster.Id);
            var storedHash = ltm.Recall(key);
            return !string.IsNullOrEmpty(storedHash);
        }

        private void RegisterAllRosters(Roster[] allRosters)
        {
            foreach (var roster in allRosters)
            {
                try
                {
                    InstantiateRoster(roster, registerGlobally: true);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(
                        $"RecallRosters: failed to register roster '{roster?.name}': {ex.Message}"
                    );
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Persist a Roster entry in LongTermMemory.
        /// </summary>
        public void UpdateRosterInLTM(Roster roster)
        {
            if (roster == null)
                return;

            RegisterRosterInLTM(roster);
        }

        public string DesignateInstanceType<T>()
        {
            return GamewideContextBrainHelpers.DesignateInstanceType<T>();
        }

        /// <summary>
        /// Encodes an instance into a single opaque Base64 string using Newtonsoft.Json.
        /// </summary>
        public string EncodeInstanceToString<T>(T instance)
        {
            return GamewideContextBrainHelpers.EncodeInstanceToString(this, instance);
        }

        /// <summary>
        /// Decodes an instance from the opaque Base64 wrapper string.
        /// </summary>
        public T DecodeInstanceFromString<T>(string encodedString)
        {
            return GamewideContextBrainHelpers.DecodeInstanceFromString<T>(this, encodedString);
        }

        #endregion
    }
}
