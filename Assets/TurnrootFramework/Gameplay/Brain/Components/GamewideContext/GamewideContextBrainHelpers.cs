using System;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Gameplay.Objects;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Shared helper methods for GamewideContextBrain to keep the main class small.
    /// Provides serialization, hashing, and tamper detection utilities.
    /// </summary>
    public static class GamewideContextBrainHelpers
    {
        private static class LedgerKeyPrefixes
        {
            public const string InstanceHash = "GWB.InstanceHash";
            public const string Roster = "GWB.Roster";
        }

        public static JsonSerializerSettings GetJsonSerializerSettings()
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                NullValueHandling = NullValueHandling.Include,
            };
            settings.Converters.Add(new UnityObjectJsonConverter());
            settings.Converters.Add(new CharacterInstanceJsonConverter());
            settings.Converters.Add(new ObjectItemInstanceJsonConverter());
            return settings;
        }

        [Serializable]
        public class SerializedWrapper
        {
            public string TypeName;
            public string Payload;
            public string Hash;
            public string Version;
        }

        /// <summary>
        /// Computes a lightweight deterministic FNV-1a 64-bit hash.
        /// </summary>
        /// <returns>Lower-case hexadecimal hash string.</returns>
        public static string ComputeFNV1a64Hex(string input)
        {
            const ulong offsetBasis = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;

            ulong hash = offsetBasis;
            if (!string.IsNullOrEmpty(input))
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                foreach (var b in bytes)
                {
                    hash ^= b;
                    hash *= prime;
                }
            }

            return hash.ToString("x16");
        }

        /// <summary>
        /// Creates a safe default instance when tampering is detected.
        /// </summary>
        public static T CreateDefaultInstanceFromWrapper<T>(SerializedWrapper wrapper)
        {
            var t = typeof(T);

            // Special case: CharacterInstance requires a CharacterData template
            if (t == typeof(CharacterInstance))
            {
                var characterData = TryExtractCharacterDataFromWrapper(wrapper);
                return characterData != null
                    ? (T)(object)CharacterInstance.Create(characterData)
                    : default;
            }

            // Generic fallback: try parameterless constructor
            if (t.IsValueType)
            {
                return default;
            }

            var ctor = t.GetConstructor(Type.EmptyTypes);
            return ctor != null ? (T)Activator.CreateInstance(t) : default;
        }

        private static CharacterData TryExtractCharacterDataFromWrapper(SerializedWrapper wrapper)
        {
            if (wrapper == null || string.IsNullOrEmpty(wrapper.Payload))
            {
                return null;
            }

            try
            {
                var payloadObj = JObject.Parse(wrapper.Payload);
                var templateToken =
                    payloadObj.SelectToken("_characterTemplate")
                    ?? payloadObj.SelectToken("CharacterTemplate");

                if (templateToken?.Type != JTokenType.Object)
                {
                    return null;
                }

#if UNITY_EDITOR
                var characterData = TryLoadCharacterDataInEditor(templateToken);
                if (characterData != null)
                {
                    return characterData;
                }
#endif

                var name = templateToken.Value<string>("name");
                if (!string.IsNullOrEmpty(name))
                {
                    return Resources.Load<CharacterData>(name);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to extract CharacterData from wrapper: {ex.Message}");
            }

            return null;
        }

#if UNITY_EDITOR
        private static CharacterData TryLoadCharacterDataInEditor(JToken templateToken)
        {
            var guid = templateToken.Value<string>("guid");
            if (!string.IsNullOrEmpty(guid))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path))
                {
                    var characterData = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>(
                        path
                    );
                    if (characterData != null)
                    {
                        return characterData;
                    }
                }
            }

            var assetPath = templateToken.Value<string>("assetPath");
            return !string.IsNullOrEmpty(assetPath)
                ? UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>(assetPath)
                : null;
        }
#endif

        /// <summary>
        /// Computes modification-check hash for an instance.
        /// </summary>
        public static string GetModificationCheckHash<T>(T instance, string versionHex = null)
        {
            try
            {
                var settings = GetJsonSerializerSettings();
                var json = JsonConvert.SerializeObject(instance, settings);
                var input = json + "|v:" + (string.IsNullOrEmpty(versionHex) ? "0" : versionHex);
                return ComputeFNV1a64Hex(input);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to compute modification hash: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Builds an obfuscated ledger key for storing instance hashes in LongTermMemory.
        /// Attempts to use an instance Id if available, otherwise falls back to hash/version.
        /// </summary>
        public static string BuildHashLedgerKey<T>(T instance, SerializedWrapper wrapper)
        {
            try
            {
                var tname = typeof(T).FullName ?? typeof(T).Name;
                var id = ExtractInstanceId(instance, wrapper);

                string rawKey = !string.IsNullOrEmpty(id)
                    ? $"{LedgerKeyPrefixes.InstanceHash}.{tname}.{id}"
                    : BuildHashBasedKey(tname, wrapper);

                var keyHash = ComputeFNV1a64Hex(rawKey);
                return $"{LedgerKeyPrefixes.InstanceHash}.{tname}.{keyHash}";
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to build ledger key: {ex.Message}");
                return null;
            }
        }

        private static string ExtractInstanceId<T>(T instance, SerializedWrapper wrapper)
        {
            // Try to extract Id from live instance
            if (instance != null)
            {
                var id = TryGetIdFromInstance(instance);
                if (!string.IsNullOrEmpty(id))
                {
                    return id;
                }
            }

            // Try parsing wrapper payload
            return wrapper != null && !string.IsNullOrEmpty(wrapper.Payload)
                ? TryGetIdFromPayload(wrapper.Payload)
                : null;
        }

        private static string TryGetIdFromInstance<T>(T instance)
        {
            var instType = instance.GetType();

            // Try property first
            var prop = instType.GetProperty(
                "Id",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            if (prop?.PropertyType == typeof(string))
            {
                return prop.GetValue(instance) as string;
            }

            // Try field
            var field = instType.GetField(
                "_id",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
            );
            return field?.FieldType == typeof(string) ? field.GetValue(instance) as string : null;
        }

        private static string TryGetIdFromPayload(string payload)
        {
            try
            {
                var obj = JObject.Parse(payload);
                return obj.SelectToken("_id")?.Value<string>()
                    ?? obj.SelectToken("Id")?.Value<string>();
            }
            catch
            {
                return null;
            }
        }

        private static string BuildHashBasedKey(string typeName, SerializedWrapper wrapper)
        {
            var hashPart = wrapper?.Hash ?? string.Empty;
            var versionPart = wrapper?.Version ?? string.Empty;
            var shortHash = hashPart.Length > 8 ? hashPart.Substring(0, 8) : hashPart;
            return $"{LedgerKeyPrefixes.InstanceHash}.{typeName}.hash_{shortHash}.v_{versionPart}";
        }

        /// <summary>
        /// Builds a ledger key for roster storage.
        /// </summary>
        public static string BuildRosterLedgerKey(string rosterId)
        {
            var rosterType = typeof(Turnroot.Characters.Roster);
            var rawKey = $"{LedgerKeyPrefixes.Roster}.{rosterType.FullName}.{rosterId}";
            var keyHash = ComputeFNV1a64Hex(rawKey);
            return $"{LedgerKeyPrefixes.Roster}.{rosterType.FullName}.{keyHash}";
        }

        public static SerializedWrapper DecodeWrapperFromBase64(string encoded)
        {
            if (string.IsNullOrEmpty(encoded))
            {
                return null;
            }

            try
            {
                var wrapperJson = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                return JsonConvert.DeserializeObject<SerializedWrapper>(wrapperJson);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to decode wrapper: {ex.Message}");
                return null;
            }
        }

        public static string EncodeWrapperToBase64(SerializedWrapper wrapper)
        {
            if (wrapper == null)
            {
                return null;
            }

            try
            {
                var json = JsonConvert.SerializeObject(wrapper, Formatting.None);
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to encode wrapper: {ex.Message}");
                return null;
            }
        }

        public static JObject DecodeWrapperAsJObject(string encoded)
        {
            if (string.IsNullOrEmpty(encoded))
            {
                return null;
            }

            try
            {
                var wrapperJson = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                return JObject.Parse(wrapperJson);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to decode wrapper as JObject: {ex.Message}");
                return null;
            }
        }

        public static string EncodeJObjectToBase64(JObject wrapper)
        {
            if (wrapper == null)
            {
                return null;
            }

            try
            {
                var json = wrapper.ToString(Formatting.None);
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to encode JObject: {ex.Message}");
                return null;
            }
        }

        public static string RecomputeHashFromWrapperJObject(JObject wrapper)
        {
            if (wrapper == null)
            {
                return string.Empty;
            }

            try
            {
                var payload = (string)wrapper["Payload"] ?? string.Empty;
                var version = (string)wrapper["Version"] ?? "0";
                return ComputeFNV1a64Hex(payload + "|v:" + version);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to recompute hash: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Encodes an instance to Base64 wrapper string and persists ledger entry.
        /// </summary>
        public static string EncodeInstanceToString<T>(GamewideContextBrain brain, T instance)
        {
            try
            {
                var wrapper = CreateWrapperForInstance(instance);
                var encoded = EncodeWrapperToBase64(wrapper);

                if (!string.IsNullOrEmpty(encoded))
                {
                    PersistLedgerEntry(brain, instance, wrapper);
                }

                return encoded;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error encoding instance to string: {e.Message}");
                return null;
            }
        }

        private static SerializedWrapper CreateWrapperForInstance<T>(T instance)
        {
            var settings = GetJsonSerializerSettings();
            var payload = JsonConvert.SerializeObject(instance, settings);
            var versionHex = DateTime.UtcNow.Ticks.ToString("x16");

            return new SerializedWrapper
            {
                TypeName = typeof(T).FullName,
                Payload = payload,
                Hash = ComputeFNV1a64Hex(payload + "|v:" + versionHex),
                Version = versionHex,
            };
        }

        private static void PersistLedgerEntry<T>(
            GamewideContextBrain brain,
            T instance,
            SerializedWrapper wrapper
        )
        {
            try
            {
                var ltm = brain.GetComponent<LongTermMemory>();
                var key = BuildHashLedgerKey(instance, wrapper);

                if (!string.IsNullOrEmpty(key) && ltm != null)
                {
                    ltm.Remember(key, wrapper.Hash);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to write hash ledger entry: {ex.Message}");
            }
        }

        /// <summary>
        /// Decodes an instance from the wrapper produced by EncodeInstanceToString.
        /// Performs tamper detection and applies the configured policy.
        /// </summary>
        public static T DecodeInstanceFromString<T>(
            GamewideContextBrain brain,
            string encodedString
        )
        {
            try
            {
                var wrapper = DecodeWrapperFromBase64(encodedString);
                if (wrapper == null)
                {
                    Debug.LogError("Decoded wrapper is null or invalid.");
                    return default;
                }

                var settings = GetJsonSerializerSettings();
                var instance = JsonConvert.DeserializeObject<T>(wrapper.Payload, settings);

                // Verify payload integrity
                if (!VerifyPayloadHash(wrapper))
                {
                    return TamperHandler.HandlePayloadMismatch(brain, instance, wrapper);
                }

                // Post-deserialization hook
                if (instance is Turnroot.Serialization.IPostDeserialize post)
                {
                    post.OnAfterDeserialize();
                }

                // Verify ledger integrity
                if (!VerifyLedgerHash(brain, instance, wrapper))
                {
                    var ltm = brain.GetComponent<LongTermMemory>();
                    var key = BuildHashLedgerKey(instance, wrapper);
                    var stored = ltm?.Recall(key);
                    return TamperHandler.HandleLedgerMismatch(brain, instance, wrapper, stored);
                }

                return instance;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error decoding instance from string: {e.Message}");
                return default;
            }
        }

        private static bool VerifyPayloadHash(SerializedWrapper wrapper)
        {
            try
            {
                var recomputed = ComputeFNV1a64Hex(wrapper.Payload + "|v:" + wrapper.Version);
                return string.Equals(recomputed, wrapper.Hash, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Hash verification failed: {ex.Message}");
                return false;
            }
        }

        private static bool VerifyLedgerHash<T>(
            GamewideContextBrain brain,
            T instance,
            SerializedWrapper wrapper
        )
        {
            try
            {
                var ltm = brain.GetComponent<LongTermMemory>();
                var key = BuildHashLedgerKey(instance, wrapper);

                if (string.IsNullOrEmpty(key) || ltm == null)
                {
                    return true; // Can't verify, assume valid
                }

                var stored = ltm.Recall(key);

                if (string.IsNullOrEmpty(stored))
                {
                    // First time seeing this instance, store its hash
                    ltm.Remember(key, wrapper.Hash);
                    return true;
                }

                return string.Equals(stored, wrapper.Hash, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Ledger verification failed: {ex.Message}");
                return true; // Can't verify, assume valid
            }
        }

        public static string DesignateInstanceType<T>() => typeof(T).FullName;

        /// <summary>
        /// Encodes an instance to Base64 without persisting ledger entries.
        /// Useful for creating replacement payloads during tamper handling.
        /// </summary>
        public static string EncodeInstanceToBase64NoLedger<T>(T instance)
        {
            try
            {
                var wrapper = CreateWrapperForInstance(instance);
                return EncodeWrapperToBase64(wrapper);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"EncodeInstanceToBase64NoLedger failed: {e.Message}");
                return null;
            }
        }
    }
}
