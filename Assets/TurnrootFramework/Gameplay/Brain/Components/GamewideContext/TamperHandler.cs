using System;
using Assets.Turnroot.Gameplay.Brain.Components;
using Newtonsoft.Json.Linq;
using Turnroot.Characters;
using Turnroot.Serialization;
using UnityEngine;

namespace Assets.Turnroot.Gameplay.Brain.Components
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
        )
        {
            return OnTamperDetected(brain, instance, wrapper, "payload mismatch");
        }

        public static T HandleLedgerMismatch<T>(
            GamewideContextBrain brain,
            T instance,
            GamewideContextBrainHelpers.SerializedWrapper wrapper,
            string stored
        )
        {
            var reason = $"ledger mismatch: stored={stored}, wrapper={wrapper?.Hash}";
            return OnTamperDetected(brain, instance, wrapper, reason);
        }

        private static T OnTamperDetected<T>(
            GamewideContextBrain brain,
            T instance,
            GamewideContextBrainHelpers.SerializedWrapper wrapper,
            string reason
        )
        {
            try
            {
                var b = brain.GetComponent<Brain>();
                string typeName = typeof(T).Name;
                string id = string.Empty;
                if (instance is CharacterInstance ci)
                    id = ci.Id;

                string message = $"Tampering detected ({reason}): type={typeName}, id={id}";
                b?.NotifyIllegalModification(message);
                Debug.LogWarning(message);

                switch (brain.Policy)
                {
                    case GamewideContextBrain.TamperPolicy.NotifyOnly:
                        return instance;
                    case GamewideContextBrain.TamperPolicy.Reject:
                        return default;
                    case GamewideContextBrain.TamperPolicy.Replace:
                    default:
                        T replacement =
                            GamewideContextBrainHelpers.CreateDefaultInstanceFromWrapper<T>(
                                wrapper
                            );

                        // Attempt to produce a replacement wrapper for hash content, but do
                        // not rely on this succeeding — regardless we must update the
                        // original ledger entry so Replace is observable and deterministic.
                        Assets.Turnroot.Gameplay.Brain.GamewideContextBrainHelpers.SerializedWrapper replacementWrapper =
                            null;
                        try
                        {
                            if (replacement != null)
                            {
                                var encodedReplacement =
                                    Assets.Turnroot.Gameplay.Brain.GamewideContextBrainHelpers.EncodeInstanceToBase64NoLedger(
                                        replacement
                                    );
                                replacementWrapper =
                                    Assets.Turnroot.Gameplay.Brain.GamewideContextBrainHelpers.DecodeWrapperFromBase64(
                                        encodedReplacement
                                    );
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning(
                                $"TamperHandler: failed to encode/parse replacement instance: {ex.Message}"
                            );
                            replacementWrapper = null;
                        }

                        try
                        {
                            var ltm = brain.GetComponent<LongTermMemory>();
                            var originalKey =
                                Assets.Turnroot.Gameplay.Brain.GamewideContextBrainHelpers.BuildHashLedgerKey(
                                    instance,
                                    replacementWrapper ?? wrapper
                                );
                            if (!string.IsNullOrEmpty(originalKey) && ltm != null)
                            {
                                var baseHash = (
                                    replacementWrapper?.Hash ?? wrapper?.Hash ?? string.Empty
                                );
                                var newVal = baseHash + "|r:" + Guid.NewGuid().ToString("N");
                                var saved = ltm.Remember(originalKey, newVal);
                                // LongTermMemory will publish OnKeySetChanged; don't publish here.
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning(
                                $"TamperHandler: failed to update original ledger entry: {ex.Message}"
                            );
                        }

                        return replacement;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"TamperHandler failed: {ex.Message}");
                return default;
            }
        }
    }
}
