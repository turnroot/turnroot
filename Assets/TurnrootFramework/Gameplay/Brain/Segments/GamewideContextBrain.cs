using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Turnroot.Characters;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Manages gamewide context within the game's brain system.
    /// Holds all instances and handles Data -> Instance conversions.
    /// </summary>
    public class GamewideContextBrain : BrainComponent
    {
        public Brain CentralBrain => _brain;

        public enum TamperPolicy
        {
            NotifyOnly = 0,
            Reject = 1,
            Replace = 2,
        }

        [Header("Rosters")]
        [SerializeField]
        private List<Roster> rosters = new();
        public IReadOnlyList<Roster> ConfiguredRosters => rosters;

        // Using SingleValueCache for roster instances
        private readonly SingleValueCache<List<RosterInstance>> _rosterInstancesCache = new();

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

        protected override void Awake() => base.Awake(); // Calls parent Awake

        protected override void SubscribeToBrainEvents()
        {
            _brain.OnRosterReady += HandleRosterReady;

            // Subscribe to Brain for automatic cache invalidation
            _brain.OnCharacterLevelUp += OnCharacterLevelUpHandler;
            _brain.OnCharacterClassChanged += OnCharacterClassChangedHandler;
            _brain.OnCharacterLearnedSkill += OnCharacterSkillLearnedHandler;
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            _brain.OnRosterReady -= HandleRosterReady;

            // Unsubscribe from Brain
            _brain.OnCharacterLevelUp -= OnCharacterLevelUpHandler;
            _brain.OnCharacterClassChanged -= OnCharacterClassChangedHandler;
            _brain.OnCharacterLearnedSkill -= OnCharacterSkillLearnedHandler;
        }

        public void Start() => RecallRosters();

        private void HandleRosterReady(RosterInstance instance) =>
            // Automatically invalidate cache when new roster is created
            _rosterInstancesCache.Invalidate();

        private void OnCharacterLevelUpHandler(CharacterInstance character) =>
            // Auto-invalidate cache when character levels up
            _rosterInstancesCache.Invalidate();

        private void OnCharacterClassChangedHandler(CharacterInstance character) =>
            // Auto-invalidate cache when character class changes
            _rosterInstancesCache.Invalidate();

        private void OnCharacterSkillLearnedHandler(CharacterInstance character, Skill skill) =>
            // Auto-invalidate cache when character learns skill
            _rosterInstancesCache.Invalidate();

        #region Roster Cache Management

        /// <summary>
        /// Get all RosterInstance references. Automatically refreshes if roster count changed.
        /// </summary>
        private List<RosterInstance> GetCachedRosterInstances()
        {
            return _rosterInstancesCache.GetOrCompute(() =>
            {
                var rosters = FindObjectsByType<RosterInstance>(FindObjectsSortMode.None);
                var instances = rosters.Where(r => r != null).ToList();
                Debug.Log($"Roster cache refreshed: {instances.Count} active rosters");
                return instances;
            });
        }

        /// <summary>
        /// Manually invalidate the roster cache when rosters are added or removed.
        /// </summary>
        public void InvalidateRosterCache() => _rosterInstancesCache.Invalidate();

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
                _brain?.PublishRosterFailed(null, "Invalid roster or empty characters");
                return null;
            }

            var existing = FindExistingRosterInstance(roster);
            return existing != null
                ? HandleExistingRoster(existing, roster, registerGlobally)
                : CreateNewRosterInstance(roster, registerGlobally);
        }

        private RosterInstance FindExistingRosterInstance(Roster roster) =>
            GetCachedRosterInstances().FirstOrDefault(r => r != null && r.roster == roster);

        private RosterInstance HandleExistingRoster(
            RosterInstance existing,
            Roster roster,
            bool registerGlobally
        )
        {
            if (HasAnyInstancesPopulated(existing, roster))
            {
                Debug.Log(
                    $"InstantiateRoster: RosterInstance already exists and contains instances for roster '{roster.name}'. Doing nothing."
                );
                _brain?.PublishRosterFailed(
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

            _brain?.PublishRosterReady(existing);
            return existing;
        }

        private bool HasAnyInstancesPopulated(RosterInstance rosterInstance, Roster roster)
        {
            foreach (var cd in roster.characters)
            {
                if (cd != null && rosterInstance.GetInstanceFor(cd) != null)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Consolidated roster population logic. Single source of truth for creating/recalling characters.
        /// </summary>
        private void PopulateRosterInstance(RosterInstance rosterInstance, Roster roster)
        {
            if (rosterInstance == null || roster?.characters == null)
            {
                return;
            }

            var createdInstances = new List<CharacterInstance>();

            foreach (var characterData in roster.characters)
            {
                if (characterData == null)
                {
                    continue;
                }

                CharacterInstance inst = CreateOrRecallCharacterInstance(characterData);

                if (inst != null)
                {
                    createdInstances.Add(inst);
                }
            }

            rosterInstance.AddInstances(createdInstances);
            Debug.Log(
                $"Populated RosterInstance '{rosterInstance.name}' with {createdInstances.Count} instances."
            );
        }

        /// <summary>
        /// Create or recall a character instance. Handles unique character persistence.
        /// </summary>
        private CharacterInstance CreateOrRecallCharacterInstance(CharacterData characterData)
        {
            if (characterData.IsUnique)
            {
                // Try to load existing unique character
                var inst = RecallUniqueCharacter(characterData);
                if (inst == null)
                {
                    // Create new unique instance and save it
                    inst = CharacterInstance.Create(characterData);
                    if (inst != null)
                    {
                        SaveUniqueCharacterInternal(inst, updateIndex: true);
                    }
                }
                return inst;
            }

            // Non-unique characters are always created fresh
            return CharacterInstance.Create(characterData);
        }

        private RosterInstance CreateNewRosterInstance(Roster roster, bool registerGlobally)
        {
            var go = new GameObject($"RosterInstance - {roster.name}");
            var newRi = go.AddComponent<RosterInstance>();
            newRi.roster = roster;

            PopulateRosterInstance(newRi, roster);

            Debug.Log(
                $"InstantiateRoster: Created new RosterInstance '{go.name}' with {newRi.Instances.Count} instances."
            );

            if (registerGlobally)
            {
                RegisterRosterInLTM(roster);
            }

            _brain?.PublishRosterReady(newRi);
            return newRi;
        }

        #endregion

        #region Unique Character Persistence

        /// <summary>
        /// Internal unified method for saving unique characters with optional index updating.
        /// </summary>
        private void SaveUniqueCharacterInternal(CharacterInstance instance, bool updateIndex)
        {
            if (instance?.CharacterTemplate == null || !instance.CharacterTemplate.IsUnique)
            {
                Debug.LogWarning("Cannot save: instance is null or not unique");
                return;
            }

            try
            {
                var encoded = EncodeInstanceToString(instance);
                var ltm = GetComponent<LongTermMemory>();
                var templateName = instance.CharacterTemplate.name;
                var key = BuildUniqueCharacterKey(instance.CharacterTemplate);

                ltm.Remember(key, encoded);

                if (updateIndex)
                {
                    var indexJson = ltm.Recall(LtmKeys.UniqueCharacterIndex);
                    var index = string.IsNullOrEmpty(indexJson)
                        ? new List<string>()
                        : JsonConvert.DeserializeObject<List<string>>(indexJson);

                    if (!index.Contains(templateName))
                    {
                        index.Add(templateName);
                        ltm.Remember(
                            LtmKeys.UniqueCharacterIndex,
                            JsonConvert.SerializeObject(index)
                        );
                    }
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
        /// Save unique character progress (preserves existing index).
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

            SaveUniqueCharacterInternal(instance, updateIndex: false);
        }

        private CharacterInstance RecallUniqueCharacter(CharacterData characterData)
        {
            if (characterData == null || !characterData.IsUnique)
            {
                return null;
            }

            try
            {
                var ltm = GetComponent<LongTermMemory>();
                var key = BuildUniqueCharacterKey(characterData);
                var encoded = ltm.Recall(key);

                if (string.IsNullOrEmpty(encoded))
                {
                    return null;
                }

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

        private string BuildUniqueCharacterKey(CharacterData characterData) =>
            $"GWB.UniqueCharacter.{characterData.name}";

        #endregion

        #region Roster Registration

        private void RegisterRosterInLTM(Roster roster)
        {
            try
            {
                if (!TryGetComponent<LongTermMemory>(out var ltm))
                {
                    return;
                }

                var key = GamewideContextBrainHelpers.BuildRosterLedgerKey(roster.Id);

                if (string.IsNullOrEmpty(key))
                {
                    return;
                }

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
                Hash = GamewideContextBrainHelpers.ComputeFNV1a64Hex($"{payload}|v:{versionHex}"),
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
            var ltm = GetComponent<LongTermMemory>();

            if (ltm == null || _brain == null)
            {
                return;
            }

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
                {
                    return;
                }

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
            if (!TryGetComponent<LongTermMemory>(out var ltm))
            {
                return false;
            }

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

        #region Character Instance Lookup

        public CharacterInstance FindInstanceByTemplate(CharacterData template)
        {
            if (template == null)
            {
                return null;
            }

            var rosters = GetCachedRosterInstances();

            foreach (var roster in rosters)
            {
                if (roster == null)
                {
                    continue;
                }

                var instance = roster.GetInstanceFor(template);
                if (instance != null)
                {
                    return instance;
                }
            }

            return null;
        }

        public List<CharacterInstance> FindInstancesByTemplates(CharacterData[] templates)
        {
            var results = new List<CharacterInstance>();

            if (templates == null || templates.Length == 0)
            {
                return results;
            }

            var rosters = GetCachedRosterInstances();
            var instanceLookup = new Dictionary<CharacterData, CharacterInstance>();

            foreach (var roster in rosters)
            {
                if (roster?.Instances == null)
                {
                    continue;
                }

                foreach (var instance in roster.Instances)
                {
                    if (instance?.CharacterTemplate != null)
                    {
                        instanceLookup[instance.CharacterTemplate] = instance;
                    }
                }
            }

            foreach (var template in templates)
            {
                if (template != null && instanceLookup.TryGetValue(template, out var instance))
                {
                    results.Add(instance);
                }
            }

            return results;
        }

        public List<CharacterInstance> GetAllActiveInstances()
        {
            var results = new List<CharacterInstance>();
            var rosters = GetCachedRosterInstances();

            foreach (var roster in rosters)
            {
                if (roster?.Instances != null)
                {
                    results.AddRange(roster.Instances);
                }
            }

            return results;
        }

        #endregion

        #region Public API

        public void UpdateRosterInLTM(Roster roster)
        {
            if (roster == null)
            {
                return;
            }

            RegisterRosterInLTM(roster);
        }

        public string DesignateInstanceType<T>() =>
            GamewideContextBrainHelpers.DesignateInstanceType<T>();

        public string EncodeInstanceToString<T>(T instance) =>
            GamewideContextBrainHelpers.EncodeInstanceToString(this, instance);

        public T DecodeInstanceFromString<T>(string encodedString) =>
            GamewideContextBrainHelpers.DecodeInstanceFromString<T>(this, encodedString);

        #endregion
    }
}
