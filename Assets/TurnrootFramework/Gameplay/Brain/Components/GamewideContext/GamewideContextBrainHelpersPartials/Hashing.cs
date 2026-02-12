using System.Text;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Static partial class providing hashing utilities for data integrity verification.
    /// </summary>
    public static partial class GamewideContextBrainHelpers
    {
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
            try
            {
                var settings = GetJsonSerializerSettings();
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(instance, settings);
                var input = json + "|v:" + (string.IsNullOrEmpty(versionHex) ? "0" : versionHex);
                return ComputeFNV1a64Hex(input);
            }
            catch (System.Exception ex)
            {
                // Hash computation can fail with corrupted/invalid data - return empty to signal failure
                $"Failed to compute modification hash: {ex.Message}".LogWarning(
                    "GamewideContextBrainHelpers"
                );
                return string.Empty;
            }
        }

        public static string RecomputeHashFromWrapperJObject(Newtonsoft.Json.Linq.JObject wrapper)
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
            catch (System.Exception ex)
            {
                // Wrapper might be corrupted - return empty to signal failure
                $"Failed to recompute hash from wrapper: {ex.Message}".LogWarning(
                    "GamewideContextBrainHelpers"
                );
                return string.Empty;
            }
        }

        #endregion
    }
}
