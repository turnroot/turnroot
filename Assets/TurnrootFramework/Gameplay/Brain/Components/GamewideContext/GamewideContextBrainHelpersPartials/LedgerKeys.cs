using System.Reflection;
using Newtonsoft.Json.Linq;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Static partial class providing ledger key generation for instance tracking and hashing.
    /// </summary>
    public static partial class GamewideContextBrainHelpers
    {
        #region Ledger Keys

        public static string BuildHashLedgerKey<T>(T instance, SerializedWrapper wrapper)
        {
            try
            {
                var tname = typeof(T).FullName ?? typeof(T).Name;
                var id = ExtractInstanceId(instance, wrapper);

                string rawKey = !string.IsNullOrEmpty(id)
                    ? $"{LtmKeys.InstanceHash}.{tname}.{id}"
                    : BuildHashBasedKey(tname, wrapper);

                var keyHash = ComputeFNV1a64Hex(rawKey);
                return $"{LtmKeys.InstanceHash}.{tname}.{keyHash}";
            }
            catch (System.Exception ex)
            {
                // Key building can fail with invalid instance - return null to signal failure
                TurnrootLogger.Log(
                    $"Failed to build ledger key: {ex.Message}",
                    TurnrootLogger.LogLevel.Warning
                );
                return null;
            }
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
    }
}
