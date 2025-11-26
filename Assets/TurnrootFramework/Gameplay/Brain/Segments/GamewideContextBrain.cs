using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Assets.Turnroot.Gameplay.Brain.Components;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Turnroot.Characters;
using Turnroot.Serialization;
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

        /* ----------------------------- Memory helpers ----------------------------- */
        public string DesignateInstanceType<T>()
        {
            return typeof(T).FullName;
        }

        /// <summary>
        /// Encodes an instance into a single opaque Base64 string using Newtonsoft.Json.
        /// The wrapper contains type information and a payload; the whole wrapper is
        /// serialized to JSON and then Base64 encoded so it's a single non-human string.
        /// </summary>
        public string EncodeInstanceToString<T>(T instance)
        {
            try
            {
                var settings = GamewideContextBrainHelpers.GetJsonSerializerSettings();
                var payload = JsonConvert.SerializeObject(instance, settings);
                var versionHex = DateTime.UtcNow.Ticks.ToString("x16");
                var wrapper = new GamewideContextBrainHelpers.SerializedWrapper
                {
                    TypeName = typeof(T).FullName,
                    Payload = payload,
                    Hash = GamewideContextBrainHelpers.ComputeFNV1a64Hex(
                        payload + "|v:" + versionHex
                    ),
                    Version = versionHex,
                };
                var wrapperJson = JsonConvert.SerializeObject(wrapper, Formatting.None);
                var bytes = Encoding.UTF8.GetBytes(wrapperJson);
                var encoded = Convert.ToBase64String(bytes);

                // Persist the wrapper hash in LongTermMemory ledger for this instance when possible
                try
                {
                    var ltm = GetComponent<LongTermMemory>();
                    var key = GamewideContextBrainHelpers.BuildHashLedgerKey(instance, wrapper);
                    if (!string.IsNullOrEmpty(key) && ltm != null)
                    {
                        ltm.Remember(key, wrapper.Hash);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to write hash ledger entry: {ex.Message}");
                }

                return encoded;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error encoding instance to string: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Decodes an instance from the opaque Base64 wrapper string produced by EncodeInstanceToString.
        /// Uses Newtonsoft.Json and registered converters to attempt hydration of UnityEngine.Object references.
        /// </summary>
        public T DecodeInstanceFromString<T>(string encodedString)
        {
            try
            {
                var wrapperJson = Encoding.UTF8.GetString(Convert.FromBase64String(encodedString));
                var wrapper =
                    JsonConvert.DeserializeObject<GamewideContextBrainHelpers.SerializedWrapper>(
                        wrapperJson
                    );
                if (wrapper == null)
                {
                    Debug.LogError("Decoded wrapper is null or invalid.");
                    return default;
                }

                var settings = GamewideContextBrainHelpers.GetJsonSerializerSettings();
                var instance = JsonConvert.DeserializeObject<T>(wrapper.Payload, settings);

                // Verify the modification hash to detect tampering (payload mismatch)
                try
                {
                    // compute the hash directly from the payload string included in the wrapper
                    var recomputed = GamewideContextBrainHelpers.ComputeFNV1a64Hex(
                        wrapper.Payload + "|v:" + wrapper.Version.ToString()
                    );
                    if (
                        !string.Equals(recomputed, wrapper.Hash, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        // notify brain about illegal modification
                        var brain = GetComponent<Brain>();
                        string typeName = typeof(T).Name;
                        string id = "";
                        if (instance is CharacterInstance tamperedInstance)
                            id = tamperedInstance.Id;
                        string message = $"Tampering detected: type={typeName}, id={id}";
                        brain?.NotifyIllegalModification(message);
                        Debug.LogWarning(message);

                        // Decide what to do depending on project policy
                        switch (tamperPolicy)
                        {
                            case TamperPolicy.NotifyOnly:
                                return instance;
                            case TamperPolicy.Reject:
                                return default;
                            case TamperPolicy.Replace:
                            default:
                                T replacement =
                                    GamewideContextBrainHelpers.CreateDefaultInstanceFromWrapper<T>(
                                        wrapper
                                    );
                                if (replacement != null)
                                {
                                    var replacementEncoded = EncodeInstanceToString(replacement);
                                }
                                return replacement;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Hash verification failed: {ex.Message}");
                }

                // allow the instance to perform any necessary post-deserialization initialization
                if (instance is global::Turnroot.Serialization.IPostDeserialize post)
                    post.OnAfterDeserialize();

                // Validate against LongTermMemory ledger when present. If there is no ledger
                // entry for this key, create one (first-save initialization). If the ledger
                // contains a different hash value, treat as tampering (wrapper was changed
                // but ledger not updated) and apply the tamper policy.
                try
                {
                    var ltm = GetComponent<LongTermMemory>();
                    var key = GamewideContextBrainHelpers.BuildHashLedgerKey(instance, wrapper);
                    if (!string.IsNullOrEmpty(key) && ltm != null)
                    {
                        var stored = ltm.Recall(key);
                        if (string.IsNullOrEmpty(stored))
                        {
                            // First time we see this instance — initialize the ledger with current hash
                            ltm.Remember(key, wrapper.Hash);
                        }
                        else if (
                            !string.Equals(stored, wrapper.Hash, StringComparison.OrdinalIgnoreCase)
                        )
                        {
                            // Ledger mismatch -> tamper detected (wrapper was changed but ledger not updated)
                            var brain = GetComponent<Brain>();
                            string message =
                                $"Tampering detected (ledger mismatch): key={key}, stored={stored}, wrapper={wrapper.Hash}";
                            brain?.NotifyIllegalModification(message);
                            Debug.LogWarning(message);

                            switch (tamperPolicy)
                            {
                                case TamperPolicy.NotifyOnly:
                                    return instance;
                                case TamperPolicy.Reject:
                                    return default;
                                case TamperPolicy.Replace:
                                default:
                                    T replacement =
                                        GamewideContextBrainHelpers.CreateDefaultInstanceFromWrapper<T>(
                                            wrapper
                                        );
                                    if (replacement != null)
                                    {
                                        var replacementEncoded = EncodeInstanceToString(
                                            replacement
                                        );
                                    }
                                    return replacement;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Ledger verification failed: {ex.Message}");
                }

                return instance;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error decoding instance from string: {e.Message}");
                return default;
            }
        }
    }
}
