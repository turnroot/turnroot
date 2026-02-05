using System.Text;

namespace Turnroot.Gameplay.Brain
{
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
            return TryExecute(
                () =>
                {
                    var settings = GetJsonSerializerSettings();
                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(instance, settings);
                    var input =
                        json + "|v:" + (string.IsNullOrEmpty(versionHex) ? "0" : versionHex);
                    return ComputeFNV1a64Hex(input);
                },
                string.Empty,
                "Failed to compute modification hash"
            );
        }

        public static string RecomputeHashFromWrapperJObject(Newtonsoft.Json.Linq.JObject wrapper)
        {
            return wrapper == null
                ? string.Empty
                : TryExecute(
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
    }
}
