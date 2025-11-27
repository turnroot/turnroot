using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Turnroot.Characters;
using Newtonsoft.Json;
using Turnroot.Characters;
using UnityEngine;

namespace Assets.Turnroot.Gameplay.Brain
{
    [RequireComponent(typeof(Brain))]
    [RequireComponent(typeof(LongTermMemory))]
    /// <summary>
    /// Manages gamewide context within the game's brain system
    /// What this actually means is- the GWCB holds all the instances, which need a central place to live,
    /// and Data -> Instance needs to happen in one local place.
    /// This Brain can convert, say, CharacterData to CharacterInstance
    /// and hold those instances for the entire game as needed.
    /// Since LongTermMemory wants strings, this Brain encodes and decodes instances <-> strings.
    /// </summary>
    public class GamewideContextBrain : MonoBehaviour
    {
        [Header("Rosters")]
        [SerializeField]
        private List<Roster> rosters = new();
        public IReadOnlyList<Roster> ConfiguredRosters => rosters;
        private int lastKnownLtmKeyCacheVersion = 0;

        public enum TamperPolicy
        {
            NotifyOnly = 0,
            Reject = 1,
            Replace = 2,
        }

        [Header("Tamper Detection")]
        [Tooltip(
            "Policy that controls what happens when an encoded payload fails the integrity check."
        )]
        [SerializeField]
        private TamperPolicy tamperPolicy = TamperPolicy.Replace;

        // runtime accessor so tools / editor helpers can change the active policy
        public TamperPolicy Policy
        {
            get => tamperPolicy;
            set => tamperPolicy = value;
        }

        /* ------------------- Recall from LongTermMemory on Start ------------------ */
        public void Start()
        {
            // Subscribe to Brain-level LTM key-cache notifications so we stay in sync
            try
            {
                var parent = GetComponent<Brain>();
                if (parent != null)
                {
                    parent.OnLtmKeyCacheUpdated += HandleLtmKeyCacheUpdated;
                    try
                    {
                        lastKnownLtmKeyCacheVersion =
                            parent.ltm?.KeyCacheVersion ?? lastKnownLtmKeyCacheVersion;
                    }
                    catch { }
                }
            }
            catch { }

            RecallRosters();
        }

        /* --------------------------- Roster instantiation -------------------------- */
        /// <summary>
        /// Instantiate runtime CharacterInstance objects for the provided Roster
        /// ScriptableObject using the canonical factory. Returns the created
        /// instances so callers can register them as needed.
        /// </summary>
        public RosterInstance InstantiateRoster(Roster roster, bool registerGlobally = false)
        {
            var brain = GetComponent<Brain>();

            if (roster == null || roster.characters == null)
            {
                Debug.LogWarning("InstantiateRoster called with null roster or characters.");
                brain?.PublishRosterFailed(null, "Invalid roster or empty characters");
                return null;
            }

            var existing = FindObjectsByType<RosterInstance>(FindObjectsSortMode.None)
                .FirstOrDefault(r => r != null && r.roster == roster);

            if (existing != null)
            {
                bool anyPresent = false;
                foreach (var cd in roster.characters)
                {
                    if (cd == null)
                        continue;
                    if (existing.GetInstanceFor(cd) != null)
                    {
                        anyPresent = true;
                        break;
                    }
                }

                if (anyPresent)
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

                var createdInstances = new List<CharacterInstance>();
                foreach (var characterData in roster.characters)
                {
                    if (characterData == null)
                        continue;
                    var inst = CharacterInstance.Create(characterData);
                    if (inst != null)
                    {
                        // Persist instance using our encoder so a ledger entry is created
                        try
                        {
                            EncodeInstanceToString(inst);
                        }
                        catch { }
                        createdInstances.Add(inst);
                    }
                }

                existing.AddInstances(createdInstances);
                Debug.Log(
                    $"InstantiateRoster: Registered {createdInstances.Count} instances into existing RosterInstance '{existing.name}'."
                );
                if (registerGlobally)
                {
                    try
                    {
                        try
                        {
                            var encodedRoster = EncodeInstanceToString(roster);
                            var wrapper = GamewideContextBrainHelpers.DecodeWrapperFromBase64(
                                encodedRoster
                            );
                            var ltm = GetComponent<LongTermMemory>();
                            var rawKey = $"GWB.Roster.{typeof(Roster).FullName}.{roster.Id}";
                            var keyHash = GamewideContextBrainHelpers.ComputeFNV1a64Hex(rawKey);
                            var key = $"GWB.Roster.{typeof(Roster).FullName}.{keyHash}";
                            if (!string.IsNullOrEmpty(key) && ltm != null)
                            {
                                var _saved = ltm.Remember(key, wrapper?.Hash);
                                try
                                {
                                    lastKnownLtmKeyCacheVersion = ltm.KeyCacheVersion;
                                }
                                catch { }
                                // LongTermMemory will publish OnKeySetChanged; don't publish here.
                                // Update master index for roster registries
                                var indexKey = "GWB.Roster.Index";
                                try
                                {
                                    var indexJson = ltm.Recall(indexKey);
                                    var idx = string.IsNullOrEmpty(indexJson)
                                        ? new List<string>()
                                        : JsonConvert.DeserializeObject<List<string>>(indexJson);
                                    if (!idx.Contains(roster.Id))
                                    {
                                        idx.Add(roster.Id);
                                        var _idxSaved = ltm.Remember(
                                            indexKey,
                                            JsonConvert.SerializeObject(idx)
                                        );
                                        try
                                        {
                                            lastKnownLtmKeyCacheVersion = ltm.KeyCacheVersion;
                                        }
                                        catch { }
                                        // LongTermMemory will publish OnKeySetChanged; don't publish here.
                                    }
                                }
                                catch { }
                            }
                        }
                        catch { }
                    }
                    catch { }
                }
                brain?.PublishRosterReady(existing);
                return existing;
            }

            var go = new GameObject($"RosterInstance - {roster.name}");
            var newRi = go.AddComponent<RosterInstance>();
            newRi.roster = roster;

#if UNITY_EDITOR
            newRi.InitializeFromRoster(roster);
            // Persist ledger entries for instances created by InitializeFromRoster
            try
            {
                foreach (var inst in newRi.Instances)
                {
                    try
                    {
                        EncodeInstanceToString(inst);
                    }
                    catch { }
                }
            }
            catch { }
#else
            var createdRuntime = new System.Collections.Generic.List<CharacterInstance>();
            foreach (var characterData in roster.characters)
            {
                if (characterData == null)
                    continue;
                var inst = CharacterInstance.Create(characterData);
                if (inst != null)
                    createdRuntime.Add(inst);
            }
            newRi.AddInstances(createdRuntime);
#endif

            Debug.Log(
                $"InstantiateRoster: Created new RosterInstance '{go.name}' with {newRi.Instances.Count} instances."
            );
            if (registerGlobally)
            {
                try
                {
                    // Persist roster wrapper/hash in ledger using the canonical encoder
                    try
                    {
                        var encodedRoster = EncodeInstanceToString(roster);
                        var wrapper = GamewideContextBrainHelpers.DecodeWrapperFromBase64(
                            encodedRoster
                        );
                        var ltm = GetComponent<LongTermMemory>();
                        var rawKey = $"GWB.Roster.{typeof(Roster).FullName}.{roster.Id}";
                        var keyHash = GamewideContextBrainHelpers.ComputeFNV1a64Hex(rawKey);
                        var key = $"GWB.Roster.{typeof(Roster).FullName}.{keyHash}";
                        if (!string.IsNullOrEmpty(key) && ltm != null)
                        {
                            var _saved = ltm.Remember(key, wrapper?.Hash);
                            try
                            {
                                lastKnownLtmKeyCacheVersion = ltm.KeyCacheVersion;
                            }
                            catch { }
                            // update index
                            var indexKey = "GWB.Roster.Index";
                            try
                            {
                                var idxJson = ltm.Recall(indexKey);
                                var idx = string.IsNullOrEmpty(idxJson)
                                    ? new List<string>()
                                    : JsonConvert.DeserializeObject<List<string>>(idxJson);
                                if (!idx.Contains(roster.Id))
                                {
                                    idx.Add(roster.Id);
                                    var _idxSaved = ltm.Remember(
                                        indexKey,
                                        JsonConvert.SerializeObject(idx)
                                    );
                                    try
                                    {
                                        lastKnownLtmKeyCacheVersion = ltm.KeyCacheVersion;
                                    }
                                    catch { }
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
                catch { }
            }
            brain?.PublishRosterReady(newRi);
            return newRi;
        }

        /* ----------------------------- Recall rosters ----------------------------- */
        private void RecallRosters()
        {
            var brain = GetComponent<Brain>();
            var ltm = GetComponent<LongTermMemory>();
            if (ltm == null || brain == null)
                return;

            var rosterType = typeof(Roster);
            var prefix = $"GWB.Roster";

            // Use the configured roster list (editable in the inspector). Do NOT scan Resources.
            var allRosters = rosters?.Where(r => r != null).ToArray() ?? new Roster[0];

            if (allRosters.Length == 0)
            {
                Debug.LogWarning(
                    "GamewideContextBrain: No configured rosters found in the inspector - nothing to recall or register."
                );
                return;
            }

            // prefer an index key if present
            var indexKey = "GWB.Roster.Index";
            var indexJson = ltm.Recall(indexKey);
            if (!string.IsNullOrEmpty(indexJson))
            {
                try
                {
                    var idList = JsonConvert.DeserializeObject<List<string>>(indexJson);
                    if (idList != null)
                    {
                        foreach (var id in idList)
                        {
                            try
                            {
                                var candidate = allRosters.FirstOrDefault(r =>
                                    r != null && r.Id == id
                                );
                                if (candidate == null)
                                    continue;

                                var rawKey = $"GWB.Roster.{rosterType.FullName}.{candidate.Id}";
                                var keyHash = GamewideContextBrainHelpers.ComputeFNV1a64Hex(rawKey);
                                var ledgerKey = $"GWB.Roster.{rosterType.FullName}.{keyHash}";
                                var storedHash = ltm.Recall(ledgerKey);
                                if (string.IsNullOrEmpty(storedHash))
                                    continue;

                                InstantiateRoster(candidate, registerGlobally: false);
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning(
                                    $"RecallRosters: failed to instantiate roster by id '{id}': {ex.Message}"
                                );
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"RecallRosters: failed to parse roster index: {ex.Message}");
                }

                return;
            }

            // fallback: scan keys and map them to roster assets by recomputing hashed keys
            var rosterEntries = ltm.RecallKeysByPrefix(prefix);
            if (rosterEntries == null || rosterEntries.Count == 0)
            {
                // first-run: register all rosters
                foreach (var rosterCandidate in allRosters)
                {
                    try
                    {
                        InstantiateRoster(rosterCandidate, registerGlobally: true);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning(
                            $"RecallRosters: failed to register roster '{rosterCandidate?.name}': {ex.Message}"
                        );
                    }
                }

                return;
            }

            foreach (var entry in rosterEntries)
            {
                try
                {
                    Roster rosterAsset = null;
                    foreach (var candidate in allRosters)
                    {
                        if (candidate == null)
                            continue;
                        var rawCandidateKey = $"GWB.Roster.{rosterType.FullName}.{candidate.Id}";
                        var candidateKeyHash = GamewideContextBrainHelpers.ComputeFNV1a64Hex(
                            rawCandidateKey
                        );
                        var candidateLedgerKey =
                            $"GWB.Roster.{rosterType.FullName}.{candidateKeyHash}";
                        if (
                            string.Equals(
                                candidateLedgerKey,
                                entry,
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        {
                            rosterAsset = candidate;
                            break;
                        }
                    }

                    if (rosterAsset == null)
                        continue;

                    var storedHash = ltm.Recall(entry);
                    if (string.IsNullOrEmpty(storedHash))
                        continue;

                    var rosterInstance = InstantiateRoster(rosterAsset, registerGlobally: false);
                    if (rosterInstance != null)
                    {
                        Debug.Log(
                            $"RecallRosters: Recalled RosterInstance '{rosterInstance.name}' from LTM."
                        );
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(
                        $"RecallRosters: error processing roster key '{entry}': {ex.Message}"
                    );
                }
            }
        }

        /* ----------------------------- Memory helpers ----------------------------- */
        /// <summary>
        /// Persist a Roster entry in LongTermMemory using the same wrapper/hash
        /// workflow as instances. This writes a hashed key for the roster and
        /// maintains the master roster index so rosters can be discovered later.
        /// </summary>
        public void UpdateRosterInLTM(Roster roster)
        {
            if (roster == null)
                return;

            try
            {
                var ltm = GetComponent<LongTermMemory>();
                var encodedRoster = EncodeInstanceToString(roster);
                var wrapper = GamewideContextBrainHelpers.DecodeWrapperFromBase64(encodedRoster);
                var rawKey = $"GWB.Roster.{typeof(Roster).FullName}.{roster.Id}";
                var keyHash = GamewideContextBrainHelpers.ComputeFNV1a64Hex(rawKey);
                var key = $"GWB.Roster.{typeof(Roster).FullName}.{keyHash}";
                if (!string.IsNullOrEmpty(key) && ltm != null)
                {
                    var _saved = ltm.Remember(key, wrapper?.Hash);
                    try
                    {
                        lastKnownLtmKeyCacheVersion = ltm.KeyCacheVersion;
                    }
                    catch { }
                    // LongTermMemory publishes keyset changes; no manual publish here.

                    // update master index
                    var indexKey = "GWB.Roster.Index";
                    try
                    {
                        var idxJson = ltm.Recall(indexKey);
                        var idx = string.IsNullOrEmpty(idxJson)
                            ? new List<string>()
                            : JsonConvert.DeserializeObject<List<string>>(idxJson);
                        if (!idx.Contains(roster.Id))
                        {
                            idx.Add(roster.Id);
                            var saved = ltm.Remember(indexKey, JsonConvert.SerializeObject(idx));
                            try
                            {
                                lastKnownLtmKeyCacheVersion = ltm.KeyCacheVersion;
                            }
                            catch { }
                            // LongTermMemory publishes keyset changes; no manual publish here.
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"UpdateRosterInLTM failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Called by helpers or other systems to let this brain know the LongTermMemory key cache
        /// version has changed. This keeps an up-to-date local view so the brain can react if needed.
        /// </summary>
        void HandleLtmKeyCacheUpdated(int version)
        {
            try
            {
                lastKnownLtmKeyCacheVersion = version;
            }
            catch { }
        }

        private void OnDestroy()
        {
            try
            {
                var parent = GetComponent<Brain>();
                if (parent != null)
                {
                    parent.OnLtmKeyCacheUpdated -= HandleLtmKeyCacheUpdated;
                }
            }
            catch { }
        }

        public string DesignateInstanceType<T>()
        {
            return GamewideContextBrainHelpers.DesignateInstanceType<T>();
        }

        /// <summary>
        /// Encodes an instance into a single opaque Base64 string using Newtonsoft.Json.
        /// The wrapper contains type information and a payload; the whole wrapper is
        /// serialized to JSON and then Base64 encoded
        /// </summary>
        public string EncodeInstanceToString<T>(T instance)
        {
            return GamewideContextBrainHelpers.EncodeInstanceToString(this, instance);
        }

        /// <summary>
        /// Decodes an instance from the opaque Base64 wrapper string produced by EncodeInstanceToString.
        /// Uses Newtonsoft.Json and registered converters to attempt hydration of UnityEngine.Object references.
        /// </summary>
        public T DecodeInstanceFromString<T>(string encodedString)
        {
            return GamewideContextBrainHelpers.DecodeInstanceFromString<T>(this, encodedString);
        }
    }
}
