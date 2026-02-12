using System;
using Newtonsoft.Json;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Serialization;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Static partial class providing instance encoding/decoding functionality with ledger tracking.
    /// </summary>
    public static partial class GamewideContextBrainHelpers
    {
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

                if (instance is IPostDeserialize post)
                {
                    post.OnAfterDeserialize();
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
                // Ledger persistence is non-critical - log and continue
                $"Failed to write hash ledger entry: {ex.Message}".LogWarning(
                    "GamewideContextBrainHelpers"
                );
            }
        }

        #endregion
    }
}
