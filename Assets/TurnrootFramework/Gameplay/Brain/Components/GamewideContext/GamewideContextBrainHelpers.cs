using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Gameplay.Maps;
using Turnroot.Serialization;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public static class GamewideContextBrainHelpers
    {
        public enum ExploredState
        {
            NotExplored,
            PartiallyExplored,
            FullyExplored,
        }

        public enum ExploredQuadrant
        {
            LeftHalf,
            RightHalf,
            TopLeft,
            BottomLeft,
            TopRight,
            BottomRight,
        }

        [Serializable]
        public struct ExploredPartial
        {
            public Dictionary<ExploredQuadrant, ExploredState> statuses;
            public MapGrid map;
        }

        [Serializable]
        public class SerializedWrapper
        {
            public string TypeName;
            public string Payload;
            public string Hash;
            public string Version;
        }

        #region Serialization Settings

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

        #endregion

        #region Hashing

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

        public static string GetModificationCheckHash<T>(T instance, string versionHex = null)
        {
            return TryExecute(
                () =>
                {
                    var settings = GetJsonSerializerSettings();
                    var json = JsonConvert.SerializeObject(instance, settings);
                    var input =
                        json + "|v:" + (string.IsNullOrEmpty(versionHex) ? "0" : versionHex);
                    return ComputeFNV1a64Hex(input);
                },
                string.Empty,
                "Failed to compute modification hash"
            );
        }

        public static string RecomputeHashFromWrapperJObject(JObject wrapper)
        {
            if (wrapper == null)
            {
                return string.Empty;
            }

            return TryExecute(
                () =>
                {
                    var payload = (string)wrapper["Payload"] ?? string.Empty;
                    var version = (string)wrapper["Version"] ?? "0";
                    return ComputeFNV1a64Hex(payload + "|v:" + version);
                },
                string.Empty,
                "Failed to recompute hash"
            );
        }

        #endregion

        #region Default Instance Creation

        public static T CreateDefaultInstanceFromWrapper<T>(SerializedWrapper wrapper)
        {
            var t = typeof(T);

            if (t == typeof(CharacterInstance))
            {
                var characterData = TryExtractCharacterDataFromWrapper(wrapper);
                return characterData != null
                    ? (T)(object)CharacterInstance.Create(characterData)
                    : default;
            }

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

            return TryExecute(
                () =>
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
                    return !string.IsNullOrEmpty(name) ? Resources.Load<CharacterData>(name) : null;
                },
                null,
                "Failed to extract CharacterData from wrapper"
            );
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

        #endregion

        #region Ledger Keys

        public static string BuildHashLedgerKey<T>(T instance, SerializedWrapper wrapper)
        {
            return TryExecute(
                () =>
                {
                    var tname = typeof(T).FullName ?? typeof(T).Name;
                    var id = ExtractInstanceId(instance, wrapper);

                    string rawKey = !string.IsNullOrEmpty(id)
                        ? $"{LtmKeys.InstanceHash}.{tname}.{id}"
                        : BuildHashBasedKey(tname, wrapper);

                    var keyHash = ComputeFNV1a64Hex(rawKey);
                    return $"{LtmKeys.InstanceHash}.{tname}.{keyHash}";
                },
                null,
                "Failed to build ledger key"
            );
        }

        public static string BuildRosterLedgerKey(string rosterId)
        {
            var rosterType = typeof(Characters.Roster);
            var rawKey = $"{LtmKeys.Roster}.{rosterType.FullName}.{rosterId}";
            var keyHash = ComputeFNV1a64Hex(rawKey);
            return $"{LtmKeys.Roster}.{rosterType.FullName}.{keyHash}";
        }

        private static string ExtractInstanceId<T>(T instance, SerializedWrapper wrapper)
        {
            if (instance != null)
            {
                var id = TryGetIdFromInstance(instance);
                if (!string.IsNullOrEmpty(id))
                {
                    return id;
                }
            }

            return wrapper != null && !string.IsNullOrEmpty(wrapper.Payload)
                ? TryGetIdFromPayload(wrapper.Payload)
                : null;
        }

        private static string TryGetIdFromInstance<T>(T instance)
        {
            var instType = instance.GetType();
            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var prop = instType.GetProperty("Id", flags);
            if (prop?.PropertyType == typeof(string))
            {
                return prop.GetValue(instance) as string;
            }

            var field = instType.GetField("_id", flags);
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
            return $"{LtmKeys.InstanceHash}.{typeName}.hash_{shortHash}.v_{versionPart}";
        }

        #endregion

        #region Wrapper Encoding/Decoding

        public static OperationResult<SerializedWrapper> DecodeWrapperFromBase64(string encoded)
        {
            if (string.IsNullOrEmpty(encoded))
            {
                return OperationResult<SerializedWrapper>.Failure(
                    "Encoded string is null or empty."
                );
            }

            try
            {
                var wrapperJson = DeviceDataCipher.DecryptFromBase64(encoded);
                var wrapper = JsonConvert.DeserializeObject<SerializedWrapper>(wrapperJson);
                return OperationResult<SerializedWrapper>.SuccessResult(wrapper);
            }
            catch (Exception ex)
            {
                return OperationResult<SerializedWrapper>.Failure(
                    $"Failed to decode wrapper: {ex.Message}",
                    ex
                );
            }
        }

        public static OperationResult<string> EncodeWrapperToBase64(SerializedWrapper wrapper)
        {
            if (wrapper == null)
            {
                return OperationResult<string>.Failure("Wrapper is null.");
            }

            try
            {
                var json = JsonConvert.SerializeObject(wrapper, Formatting.None);
                var encoded = DeviceDataCipher.EncryptToBase64(json);
                return OperationResult<string>.SuccessResult(encoded);
            }
            catch (Exception ex)
            {
                return OperationResult<string>.Failure(
                    $"Failed to encode wrapper: {ex.Message}",
                    ex
                );
            }
        }

        public static OperationResult<JObject> DecodeWrapperAsJObject(string encoded)
        {
            if (string.IsNullOrEmpty(encoded))
            {
                return OperationResult<JObject>.Failure("Encoded string is null or empty.");
            }

            try
            {
                var wrapperJson = DeviceDataCipher.DecryptFromBase64(encoded);
                var obj = JObject.Parse(wrapperJson);
                return OperationResult<JObject>.SuccessResult(obj);
            }
            catch (Exception ex)
            {
                return OperationResult<JObject>.Failure(
                    $"Failed to decode wrapper as JObject: {ex.Message}",
                    ex
                );
            }
        }

        public static OperationResult<string> EncodeJObjectToBase64(JObject wrapper)
        {
            if (wrapper == null)
            {
                return OperationResult<string>.Failure("JObject wrapper is null.");
            }

            try
            {
                var json = wrapper.ToString(Formatting.None);
                var encoded = DeviceDataCipher.EncryptToBase64(json);
                return OperationResult<string>.SuccessResult(encoded);
            }
            catch (Exception ex)
            {
                return OperationResult<string>.Failure(
                    $"Failed to encode JObject: {ex.Message}",
                    ex
                );
            }
        }

        #endregion

        #region Instance Encoding/Decoding

        public static OperationResult<string> EncodeInstanceToString<T>(
            GamewideContextBrain brain,
            T instance
        )
        {
            try
            {
                var wrapper = CreateWrapperForInstance(instance);
                var encodeResult = EncodeWrapperToBase64(wrapper);

                if (!encodeResult.Success)
                {
                    return OperationResult<string>.Failure(
                        encodeResult.Error,
                        encodeResult.Exception
                    );
                }

                PersistLedgerEntry(brain, instance, wrapper);
                return OperationResult<string>.SuccessResult(encodeResult.Value);
            }
            catch (Exception e)
            {
                return OperationResult<string>.Failure(
                    $"Error encoding instance to string: {e.Message}",
                    e
                );
            }
        }

        public static OperationResult<string> EncodeInstanceToBase64NoLedger<T>(T instance)
        {
            try
            {
                var wrapper = CreateWrapperForInstance(instance);
                return EncodeWrapperToBase64(wrapper);
            }
            catch (Exception e)
            {
                return OperationResult<string>.Failure(
                    $"EncodeInstanceToBase64NoLedger failed: {e.Message}",
                    e
                );
            }
        }

        public static OperationResult<T> DecodeInstanceFromString<T>(
            GamewideContextBrain brain,
            string encodedString
        )
        {
            try
            {
                var decodeResult = DecodeWrapperFromBase64(encodedString);
                if (!decodeResult.Success)
                {
                    return OperationResult<T>.Failure(decodeResult.Error, decodeResult.Exception);
                }

                var wrapper = decodeResult.Value;
                var settings = GetJsonSerializerSettings();
                var instance = JsonConvert.DeserializeObject<T>(wrapper.Payload, settings);

                if (!VerifyPayloadHash(wrapper))
                {
                    var tampered = TamperHandler.HandlePayloadMismatch(brain, instance, wrapper);
                    return OperationResult<T>.SuccessResult(tampered);
                }

                if (instance is IPostDeserialize post)
                {
                    post.OnAfterDeserialize();
                }

                if (!VerifyLedgerHash(brain, instance, wrapper))
                {
                    var ltm = brain.GetComponent<LongTermMemory>();
                    var key = BuildHashLedgerKey(instance, wrapper);
                    var stored = ltm?.Recall(key);
                    var tampered = TamperHandler.HandleLedgerMismatch(
                        brain,
                        instance,
                        wrapper,
                        stored
                    );
                    return OperationResult<T>.SuccessResult(tampered);
                }

                return OperationResult<T>.SuccessResult(instance);
            }
            catch (Exception e)
            {
                return OperationResult<T>.Failure(
                    $"Error decoding instance from string: {e.Message}",
                    e
                );
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
            TryExecute(
                () =>
                {
                    var ltm = brain.GetComponent<LongTermMemory>();
                    var key = BuildHashLedgerKey(instance, wrapper);

                    if (!string.IsNullOrEmpty(key) && ltm != null)
                    {
                        ltm.Remember(key, wrapper.Hash);
                    }
                },
                "Failed to write hash ledger entry"
            );
        }

        #endregion

        #region Verification

        private static bool VerifyPayloadHash(SerializedWrapper wrapper)
        {
            return TryExecute(
                () =>
                {
                    var recomputed = ComputeFNV1a64Hex(wrapper.Payload + "|v:" + wrapper.Version);
                    return string.Equals(
                        recomputed,
                        wrapper.Hash,
                        StringComparison.OrdinalIgnoreCase
                    );
                },
                false,
                "Hash verification failed"
            );
        }

        private static bool VerifyLedgerHash<T>(
            GamewideContextBrain brain,
            T instance,
            SerializedWrapper wrapper
        )
        {
            return TryExecute(
                () =>
                {
                    var ltm = brain.GetComponent<LongTermMemory>();
                    var key = BuildHashLedgerKey(instance, wrapper);

                    if (string.IsNullOrEmpty(key) || ltm == null)
                    {
                        return true;
                    }

                    var stored = ltm.Recall(key);

                    if (string.IsNullOrEmpty(stored))
                    {
                        ltm.Remember(key, wrapper.Hash);
                        return true;
                    }

                    return string.Equals(stored, wrapper.Hash, StringComparison.OrdinalIgnoreCase);
                },
                true,
                "Ledger verification failed"
            );
        }

        #endregion

        #region Utilities

        public static string DesignateInstanceType<T>() => typeof(T).FullName;

        private static T TryExecute<T>(Func<T> action, T defaultValue, string errorMessage)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                TurnrootLogger.Log(
                    $"{errorMessage}: {ex.Message}",
                    TurnrootLogger.LogLevel.Warning
                );
                return defaultValue;
            }
        }

        private static void TryExecute(Action action, string errorMessage)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                TurnrootLogger.Log(
                    $"{errorMessage}: {ex.Message}",
                    TurnrootLogger.LogLevel.Warning
                );
            }
        }

        #endregion
    }
}
