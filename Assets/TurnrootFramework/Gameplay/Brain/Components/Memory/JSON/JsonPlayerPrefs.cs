using System;
using System.Collections.Generic;
using System.IO;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain.Components
{
    /// <summary>
    /// A replacement for Unity's PlayerPrefs that stores data in a JSON file.
    /// Supports key encoding for obfuscation and change tracking via events.
    /// </summary>
    [Serializable]
    public class JsonPlayerPrefs
    {
        /// <summary>
        /// Represents a single key-value pair stored in the JSON-based player preferences.
        /// </summary>
        [Serializable]
        private class PlayerPref
        {
            public string key;
            public string value;

            public PlayerPref(string key, string value)
            {
                this.key = key;
                this.value = value;
            }
        }

        [SerializeField]
        private List<PlayerPref> playerPrefs = new();

        private readonly string savePath;

        // Runtime decoded-keys cache + versioning
        private int keyCacheVersion = 0;
        private int cachedKeysVersion = -1;
        private List<string> cachedDecodedKeys = null;

        public int KeyCacheVersion => keyCacheVersion;

        public event Action<int> OnKeySetChanged;

        #region Initialization

        public JsonPlayerPrefs(string savePath)
        {
            this.savePath = savePath;
            LoadFromDisk();
        }

        private void LoadFromDisk()
        {
            if (!File.Exists(savePath))
            {
                return;
            }

            try
            {
                using (StreamReader reader = new StreamReader(savePath))
                {
                    string json = reader.ReadToEnd();
                    JsonPlayerPrefs data = JsonUtility.FromJson<JsonPlayerPrefs>(json);

                    if (data?.playerPrefs != null)
                    {
                        playerPrefs = data.playerPrefs;
                    }
                    else
                    {
                        $"JsonPlayerPrefs: invalid or empty JSON at {savePath}, starting fresh.".LogWarning();
                        playerPrefs = new List<PlayerPref>();
                    }
                }
            }
            catch (Exception ex)
            {
                $"JsonPlayerPrefs: failed to load prefs from {savePath}: {ex.Message}".LogWarning();
                playerPrefs = new List<PlayerPref>();
            }
        }

        #endregion

        #region Generic Get/Set Implementation

        private T GetValue<T>(
            string key,
            T defaultValue,
            Func<string, (bool success, T value)> parser
        )
        {
            if (TryGetPlayerPref(key, out PlayerPref playerPref))
            {
                var (success, value) = parser(playerPref.value);
                if (success)
                {
                    return value;
                }
            }
            return defaultValue;
        }

        private void SetValue<T>(string key, T value)
        {
            var stringValue = value switch
            {
                float f => f.ToString(System.Globalization.CultureInfo.InvariantCulture),
                _ => value?.ToString() ?? string.Empty,
            };

            var encoded = EncodeKey(key);
            if (TryGetPlayerPrefInternal(encoded, out PlayerPref playerPref))
            {
                playerPref.value = stringValue;
            }
            else
            {
                playerPrefs.Add(new PlayerPref(encoded, stringValue));
                InvalidateKeyCache();
            }
        }

        #endregion

        #region Public API - Get

        public float GetFloat(string key, float defaultValue = 0f)
        {
            return GetValue(
                key,
                defaultValue,
                value =>
                {
                    bool success = float.TryParse(
                        value,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out float result
                    );
                    return (success, result);
                }
            );
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            return GetValue(
                key,
                defaultValue,
                value =>
                {
                    bool success = int.TryParse(value, out int result);
                    return (success, result);
                }
            );
        }

        public string GetString(string key, string defaultValue = "") =>
            GetValue(key, defaultValue, value => (true, value));

        public bool GetBool(string key, bool defaultValue = false)
        {
            int intValue = GetInt(key, defaultValue ? 1 : 0);
            return intValue != 0;
        }

        public bool HasKey(string key)
        {
            var encoded = EncodeKey(key);
            return playerPrefs.Exists(p => p.key == encoded);
        }

        #endregion

        #region Public API - Set

        public void SetFloat(string key, float value) => SetValue(key, value);

        public void SetInt(string key, int value) => SetValue(key, value);

        public void SetString(string key, string value) => SetValue(key, value);

        public void SetBool(string key, bool value) => SetValue(key, value ? 1 : 0);

        #endregion

        #region Public API - Delete

        public void DeleteAll()
        {
            if (playerPrefs.Count > 0)
            {
                playerPrefs.Clear();
                InvalidateKeyCache();
            }
        }

        public void DeleteKey(string key)
        {
            var encoded = EncodeKey(key);
            int removedCount = playerPrefs.RemoveAll(p => p.key == encoded);

            if (removedCount > 0)
            {
                InvalidateKeyCache();
            }
        }

        #endregion

        #region Public API - Save

        public void Save()
        {
            string directory = Path.GetDirectoryName(savePath);
            Directory.CreateDirectory(directory);

            string json = JsonUtility.ToJson(this);
            using (StreamWriter writer = new StreamWriter(savePath))
            {
                writer.WriteLine(json);
            }
        }

        #endregion

        #region Public API - Keys

        internal IEnumerable<string> GetAllKeys()
        {
            // Return cached decoded keys when cache version matches
            if (cachedDecodedKeys != null && cachedKeysVersion == keyCacheVersion)
            {
                foreach (var k in cachedDecodedKeys)
                {
                    yield return k;
                }

                yield break;
            }

            // Rebuild cache
            var list = new List<string>(playerPrefs.Count);
            foreach (var pref in playerPrefs)
            {
                list.Add(DecodeKey(pref.key));
            }

            cachedDecodedKeys = list;
            cachedKeysVersion = keyCacheVersion;

            foreach (var k in cachedDecodedKeys)
            {
                yield return k;
            }
        }

        #endregion

        #region Private Helpers

        private bool TryGetPlayerPref(string key, out PlayerPref playerPref)
        {
            var encoded = EncodeKey(key);
            return TryGetPlayerPrefInternal(encoded, out playerPref);
        }

        private bool TryGetPlayerPrefInternal(string encodedKey, out PlayerPref playerPref)
        {
            playerPref = playerPrefs.Find(p => p.key == encodedKey);
            return playerPref != null;
        }

        private void InvalidateKeyCache()
        {
            keyCacheVersion++;
            cachedDecodedKeys = null;
            cachedKeysVersion = -1;
            OnKeySetChanged?.Invoke(keyCacheVersion);
        }

        private string EncodeKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            var result = Utilities.DeviceDataCipher.EncryptToBase64(key);
            if (result.Success)
            {
                return result.Value;
            }
#if UNITY_EDITOR
            Debug.LogWarning($"JsonPlayerPrefs: failed to encode key '{key}': {result.Error}");
#endif
            return key;
        }

        private string DecodeKey(string encoded)
        {
            if (string.IsNullOrEmpty(encoded))
            {
                return encoded;
            }

            var result = Utilities.DeviceDataCipher.DecryptFromBase64(encoded);
            if (result.Success)
            {
                return result.Value;
            }
#if UNITY_EDITOR
            Debug.LogWarning($"JsonPlayerPrefs: failed to decode key: {result.Error}");
#endif
            return encoded;
        }
        #endregion
    }
}
