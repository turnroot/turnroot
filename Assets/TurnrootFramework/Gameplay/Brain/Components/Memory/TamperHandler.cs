using System;
using Turnroot.Characters;
using Turnroot.Utilities;

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

            var parentBrain = brain.Brain;
            parentBrain.PublishIllegalModification(message);

            TurnrootLogger.Log(message, TurnrootLogger.LogLevel.Warning);
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

            var encodeResult = GamewideContextBrainHelpers.EncodeInstanceToBase64NoLedger(
                replacement
            );
            if (!encodeResult.Success)
            {
                TurnrootLogger.Log(
                    $"TamperHandler: failed to encode replacement instance: {encodeResult.Error}",
                    TurnrootLogger.LogLevel.Warning
                );
                return null;
            }

            var decodeResult = GamewideContextBrainHelpers.DecodeWrapperFromBase64(
                encodeResult.Value
            );
            if (!decodeResult.Success)
            {
                TurnrootLogger.Log(
                    $"TamperHandler: failed to decode wrapper from replacement instance: {decodeResult.Error}",
                    TurnrootLogger.LogLevel.Warning
                );
                return null;
            }

            return decodeResult.Value;
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

                brain.Brain.ltm.Remember(key, replacementMarker);
            }
            catch (Exception ex)
            {
                TurnrootLogger.Log(
                    $"TamperHandler: failed to update ledger for replacement: {ex.Message}",
                    TurnrootLogger.LogLevel.Warning
                );
            }
        }
    }
}
