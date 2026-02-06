using System;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Static partial class providing hash verification methods for payload and ledger integrity.
    /// </summary>
    public static partial class GamewideContextBrainHelpers
    {
        #region Verification

        private static bool VerifyPayloadHash(SerializedWrapper wrapper)
        {
            try
            {
                var recomputed = ComputeFNV1a64Hex(wrapper.Payload + "|v:" + wrapper.Version);
                return string.Equals(recomputed, wrapper.Hash, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                // Hash verification failure likely means corrupted data
                TurnrootLogger.Log(
                    $"Hash verification failed: {ex.Message}",
                    TurnrootLogger.LogLevel.Warning
                );
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
                    return true;
                }

                var stored = ltm.Recall(key);

                if (string.IsNullOrEmpty(stored))
                {
                    ltm.Remember(key, wrapper.Hash);
                    return true;
                }

                return string.Equals(stored, wrapper.Hash, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                // Ledger verification can fail with corrupted data - default to accepting
                TurnrootLogger.Log(
                    $"Ledger verification failed: {ex.Message}",
                    TurnrootLogger.LogLevel.Warning
                );
                return true;
            }
        }

        #endregion
    }
}
