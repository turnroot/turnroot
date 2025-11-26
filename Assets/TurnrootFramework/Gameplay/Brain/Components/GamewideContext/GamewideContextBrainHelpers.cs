using System;
using System.Reflection;
using System.Text;
using Assets.Turnroot.Gameplay.Brain.Components;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Objects;
using UnityEngine;

namespace Assets.Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Shared helper methods for GamewideContextBrain to keep the main class small.
    /// </summary>
    public static class GamewideContextBrainHelpers
    {
        public static JsonSerializerSettings GetJsonSerializerSettings()
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                NullValueHandling = NullValueHandling.Include,
            };
            settings.Converters.Add(new UnityObjectJsonConverter());
            settings.Converters.Add(new CharacterInstanceJsonConverter());
            // Instances backed by a ScriptableObject template (like ObjectItemInstance)
            // require a read-time converter to ensure the template constructor runs
            // and the private backing field is set. SampleInstanceJsonConverter<TData,TInstance>
            // is a reusable converter for these cases.
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

        // Lightweight deterministic FNV-1a 64-bit hash; returns lower-case hex
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

        // Create a safe default instance when tampering is detected.
        // This logic mirrors the original implementation but is now testable in isolation.
        public static T CreateDefaultInstanceFromWrapper<T>(SerializedWrapper wrapper)
        {
            try
            {
                var t = typeof(T);
                // Special case: CharacterInstance -> attempt to find CharacterData referenced in payload
                if (t == typeof(CharacterInstance))
                {
                    try
                    {
                        var payloadObj = JObject.Parse(wrapper.Payload);
                        var templateToken =
                            payloadObj.SelectToken("_characterTemplate")
                            ?? payloadObj.SelectToken("CharacterTemplate");
                        if (templateToken != null && templateToken.Type == JTokenType.Object)
                        {
#if UNITY_EDITOR
                            var guid = templateToken.Value<string>("guid");
                            var assetPath = templateToken.Value<string>("assetPath");
                            if (!string.IsNullOrEmpty(guid))
                            {
                                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                                if (!string.IsNullOrEmpty(path))
                                {
                                    var characterData =
                                        UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>(
                                            path
                                        );
                                    if (characterData != null)
                                    {
                                        return (T)(object)CharacterInstance.Create(characterData);
                                    }
                                }
                            }
                            if (!string.IsNullOrEmpty(assetPath))
                            {
                                var characterData =
                                    UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>(
                                        assetPath
                                    );
                                if (characterData != null)
                                {
                                    return (T)(object)CharacterInstance.Create(characterData);
                                }
                            }
#endif
                            var name = templateToken.Value<string>("name");
                            if (!string.IsNullOrEmpty(name))
                            {
                                var characterData = Resources.Load<CharacterData>(name);
                                if (characterData != null)
                                {
                                    return (T)(object)CharacterInstance.Create(characterData);
                                }
                            }
                        }
                    }
                    catch { }
                    // If all else fails, we cannot create a CharacterInstance without a template
                    return default;
                }

                // Generic fallback: try parameterless constructor
                if (t.IsValueType)
                    return default;
                var ctor = t.GetConstructor(Type.EmptyTypes);
                if (ctor != null)
                {
                    return (T)Activator.CreateInstance(t);
                }

                return default;
            }
            catch
            {
                return default;
            }
        }

        // Helper: compute modification-check hash for an instance given an optional version hex
        public static string GetModificationCheckHash<T>(T instance, string versionHex = null)
        {
            try
            {
                var settings = GetJsonSerializerSettings();
                var json = JsonConvert.SerializeObject(instance, settings);
                var input = json + "|v:" + (string.IsNullOrEmpty(versionHex) ? "0" : versionHex);
                return ComputeFNV1a64Hex(input);
            }
            catch
            {
                return string.Empty;
            }
        }

        // Helper: compose ledger key for storing an instance hash in LongTermMemory.
        // Attempts to use an instance Id if available, otherwise falls back to a deterministic
        // key derived from wrapper hash/version. Keys are obfuscated by hashing the raw key
        // to make them less-friendly for casual hackers.
        public static string BuildHashLedgerKey<T>(T instance, SerializedWrapper wrapper)
        {
            try
            {
                var tname = typeof(T).FullName ?? typeof(T).Name;

                // Try to extract an Id from the live instance first (property or field)
                string id = null;
                if (instance != null)
                {
                    var instType = instance.GetType();
                    var prop = instType.GetProperty(
                        "Id",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    );
                    if (prop != null && prop.PropertyType == typeof(string))
                        id = prop.GetValue(instance) as string;
                    if (string.IsNullOrEmpty(id))
                    {
                        var fi = instType.GetField(
                            "_id",
                            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                        );
                        if (fi != null && fi.FieldType == typeof(string))
                            id = fi.GetValue(instance) as string;
                    }
                }

                // If no id found on instance, try parsing the wrapper payload for an _id/Id token
                if (
                    string.IsNullOrEmpty(id)
                    && wrapper != null
                    && !string.IsNullOrEmpty(wrapper.Payload)
                )
                {
                    try
                    {
                        var obj = JObject.Parse(wrapper.Payload);
                        id =
                            obj.SelectToken("_id")?.Value<string>()
                            ?? obj.SelectToken("Id")?.Value<string>();
                    }
                    catch { }
                }

                string rawKey;
                if (!string.IsNullOrEmpty(id))
                {
                    rawKey = $"GWB.InstanceHash.{tname}.{id}";
                }
                else
                {
                    var hashPart = wrapper?.Hash ?? string.Empty;
                    var versionPart = wrapper?.Version ?? string.Empty;
                    var shortHash = hashPart.Length > 8 ? hashPart.Substring(0, 8) : hashPart;
                    rawKey = $"GWB.InstanceHash.{tname}.hash_{shortHash}.v_{versionPart}";
                }

                var keyHash = ComputeFNV1a64Hex(rawKey);
                return $"GWB.InstanceHash.{tname}.{keyHash}";
            }
            catch
            {
                return null;
            }
        }

        // --- Wrapper / encoding helpers (DRY) ---------------------------------
        public static SerializedWrapper DecodeWrapperFromBase64(string encoded)
        {
            try
            {
                var wrapperJson = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                return JsonConvert.DeserializeObject<SerializedWrapper>(wrapperJson);
            }
            catch
            {
                return null;
            }
        }

        public static string EncodeWrapperToBase64(SerializedWrapper wrapper)
        {
            try
            {
                var json = JsonConvert.SerializeObject(wrapper, Formatting.None);
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            }
            catch
            {
                return null;
            }
        }

        public static Newtonsoft.Json.Linq.JObject DecodeWrapperAsJObject(string encoded)
        {
            try
            {
                var wrapperJson = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                return Newtonsoft.Json.Linq.JObject.Parse(wrapperJson);
            }
            catch
            {
                return null;
            }
        }

        public static string EncodeJObjectToBase64(Newtonsoft.Json.Linq.JObject wrapper)
        {
            try
            {
                var json = wrapper.ToString(Formatting.None);
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            }
            catch
            {
                return null;
            }
        }

        public static string RecomputeHashFromWrapperJObject(Newtonsoft.Json.Linq.JObject wrapper)
        {
            try
            {
                var payload = (string)wrapper["Payload"] ?? string.Empty;
                var version = (string)wrapper["Version"] ?? "0";
                return ComputeFNV1a64Hex(payload + "|v:" + version);
            }
            catch
            {
                return string.Empty;
            }
        }

        // Encode an instance to the Base64 wrapper string and persist ledger entry
        // using the supplied GamewideContextBrain (so storage/ltm behavior is preserved).
        public static string EncodeInstanceToString<T>(GamewideContextBrain brain, T instance)
        {
            try
            {
                var settings = GetJsonSerializerSettings();
                var payload = JsonConvert.SerializeObject(instance, settings);
                var versionHex = DateTime.UtcNow.Ticks.ToString("x16");
                var wrapper = new SerializedWrapper
                {
                    TypeName = typeof(T).FullName,
                    Payload = payload,
                    Hash = ComputeFNV1a64Hex(payload + "|v:" + versionHex),
                    Version = versionHex,
                };

                var wrapperJson = JsonConvert.SerializeObject(wrapper, Formatting.None);
                var bytes = Encoding.UTF8.GetBytes(wrapperJson);
                var encoded = Convert.ToBase64String(bytes);

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

                return encoded;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error encoding instance to string: {e.Message}");
                return null;
            }
        }

        // Decode an instance from the wrapper produced by EncodeInstanceToString.
        // Uses the provided GamewideContextBrain for tamper policy handling and ledger
        // verification so the behavior remains consistent with previous implementation.
        public static T DecodeInstanceFromString<T>(
            GamewideContextBrain brain,
            string encodedString
        )
        {
            try
            {
                var wrapperJson = Encoding.UTF8.GetString(Convert.FromBase64String(encodedString));
                var wrapper = JsonConvert.DeserializeObject<SerializedWrapper>(wrapperJson);
                if (wrapper == null)
                {
                    Debug.LogError("Decoded wrapper is null or invalid.");
                    return default;
                }

                var settings = GetJsonSerializerSettings();
                var instance = JsonConvert.DeserializeObject<T>(wrapper.Payload, settings);

                // Verify modification hash (payload mismatch)
                try
                {
                    var recomputed = ComputeFNV1a64Hex(
                        wrapper.Payload + "|v:" + wrapper.Version.ToString()
                    );
                    if (
                        !string.Equals(recomputed, wrapper.Hash, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        return Assets.Turnroot.Gameplay.Brain.Components.TamperHandler.HandlePayloadMismatch(
                            brain,
                            instance,
                            wrapper
                        );
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Hash verification failed: {ex.Message}");
                }

                if (instance is global::Turnroot.Serialization.IPostDeserialize post)
                    post.OnAfterDeserialize();

                try
                {
                    var ltm = brain.GetComponent<LongTermMemory>();
                    var key = BuildHashLedgerKey(instance, wrapper);
                    if (!string.IsNullOrEmpty(key) && ltm != null)
                    {
                        var stored = ltm.Recall(key);
                        if (string.IsNullOrEmpty(stored))
                        {
                            ltm.Remember(key, wrapper.Hash);
                        }
                        else if (
                            !string.Equals(stored, wrapper.Hash, StringComparison.OrdinalIgnoreCase)
                        )
                        {
                            return Assets.Turnroot.Gameplay.Brain.Components.TamperHandler.HandleLedgerMismatch(
                                brain,
                                instance,
                                wrapper,
                                stored
                            );
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

        public static string DesignateInstanceType<T>()
        {
            return typeof(T).FullName;
        }

        // Encode an instance into the Base64 wrapper string but DO NOT persist any
        // ledger entries. This is useful for internal operations (e.g. creating a
        // replacement payload) where we must avoid creating ledger records for the
        // replacement id.
        public static string EncodeInstanceToBase64NoLedger<T>(T instance)
        {
            try
            {
                var settings = GetJsonSerializerSettings();
                var payload = JsonConvert.SerializeObject(instance, settings);
                var versionHex = DateTime.UtcNow.Ticks.ToString("x16");
                var wrapper = new SerializedWrapper
                {
                    TypeName = typeof(T).FullName,
                    Payload = payload,
                    Hash = ComputeFNV1a64Hex(payload + "|v:" + versionHex),
                    Version = versionHex,
                };

                var wrapperJson = JsonConvert.SerializeObject(wrapper, Formatting.None);
                var bytes = Encoding.UTF8.GetBytes(wrapperJson);
                return Convert.ToBase64String(bytes);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"EncodeInstanceToBase64NoLedger failed: {e.Message}");
                return null;
            }
        }
    }
}
