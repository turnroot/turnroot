using System;
using Turnroot.Gameplay.Brain.Components;

namespace Turnroot.Gameplay.Brain
{
    public static partial class GamewideContextBrainHelpers
    {
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
    }
}
