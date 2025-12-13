using System;
using Turnroot.Characters;
using UnityEngine;

namespace Turnroot.Gameplay.Brain.Components
{
    /// <summary>
    /// Central handler for tamper detection policy decisions.
    /// GamewideContextBrain delegates policy decisions (notify/reject/replace) to this helper
    /// to keep the Brain class small and make policy behavior easier to test and reuse.
    /// </summary>
    public static class TamperHandler
    {
        public static T HandlePayloadMismatch<T>(
            GamewideContextBrain brain,
            T instance,
            GamewideContextBrainHelpers.SerializedWrapper wrapper
        ) => OnTamperDetected(brain, instance, wrapper, "payload mismatch");

        public static T HandleLedgerMismatch<T>(
            GamewideContextBrain brain,
            T instance,
            GamewideContextBrainHelpers.SerializedWrapper wrapper,
            string storedHash
        )
        {
            var reason = $"ledger mismatch: stored={storedHash}, wrapper={wrapper?.Hash}";
            return OnTamperDetected(brain, instance, wrapper, reason);
        }

        private static T OnTamperDetected<T>(
            GamewideContextBrain brain,
            T instance,
            GamewideContextBrainHelpers.SerializedWrapper wrapper,
            string reason
        )
        {
            LogTamperDetection(brain, instance, reason);

            switch (brain.Policy)
            {
                case GamewideContextBrain.TamperPolicy.NotifyOnly:
                    return instance;

                case GamewideContextBrain.TamperPolicy.Reject:
                    return default;

                case GamewideContextBrain.TamperPolicy.Replace:
                default:
                    return HandleReplace(brain, instance, wrapper);
            }
        }

        private static void LogTamperDetection<T>(
            GamewideContextBrain brain,
            T instance,
            string reason
        )
        {
            var typeName = typeof(T).Name;
            var id = ExtractInstanceId(instance);
            var message = $"Tampering detected ({reason}): type={typeName}, id={id}";

            var parentBrain = brain.GetComponent<Brain>();
            parentBrain?.NotifyIllegalModification(message);

            Debug.LogWarning(message);
        }

        private static string ExtractInstanceId<T>(T instance)
        {
            if (instance is CharacterInstance ci)
            {
                return ci.Id;
            }

            // Could extend to other instance types with Id properties
            return string.Empty;
        }

        private static T HandleReplace<T>(
            GamewideContextBrain brain,
            T instance,
            GamewideContextBrainHelpers.SerializedWrapper wrapper
        )
        {
            var replacement = GamewideContextBrainHelpers.CreateDefaultInstanceFromWrapper<T>(
                wrapper
            );
            var replacementWrapper = TryCreateReplacementWrapper(replacement);

            UpdateLedgerForReplacement(brain, instance, wrapper, replacementWrapper);

            return replacement;
        }

        private static GamewideContextBrainHelpers.SerializedWrapper TryCreateReplacementWrapper<T>(
            T replacement
        )
        {
            if (replacement == null)
            {
                return null;
            }

            try
            {
                var encoded = GamewideContextBrainHelpers.EncodeInstanceToBase64NoLedger(
                    replacement
                );
                return GamewideContextBrainHelpers.DecodeWrapperFromBase64(encoded);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"TamperHandler: failed to create replacement wrapper: {ex.Message}"
                );
                return null;
            }
        }

        private static void UpdateLedgerForReplacement<T>(
            GamewideContextBrain brain,
            T instance,
            GamewideContextBrainHelpers.SerializedWrapper originalWrapper,
            GamewideContextBrainHelpers.SerializedWrapper replacementWrapper
        )
        {
            try
            {
                if (!brain.TryGetComponent<LongTermMemory>(out var ltm))
                {
                    return;
                }

                var key = GamewideContextBrainHelpers.BuildHashLedgerKey(
                    instance,
                    replacementWrapper ?? originalWrapper
                );

                if (string.IsNullOrEmpty(key))
                {
                    return;
                }

                var baseHash = replacementWrapper?.Hash ?? originalWrapper?.Hash ?? string.Empty;
                var replacementMarker = baseHash + "|r:" + Guid.NewGuid().ToString("N");

                ltm.Remember(key, replacementMarker);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"TamperHandler: failed to update ledger for replacement: {ex.Message}"
                );
            }
        }
    }
}
