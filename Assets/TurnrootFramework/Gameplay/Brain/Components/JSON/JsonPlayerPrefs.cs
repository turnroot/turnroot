using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// A replacement for Unity's PlayerPrefs that stores data in a JSON file.
/// Supports key encoding for obfuscation and change tracking via events.
/// </summary>
/// <example>
/// JsonPlayerPrefs prefs = new JsonPlayerPrefs(Application.persistentDataPath + "/Preferences.json");
/// prefs.SetInt("testKey", 18);
/// prefs.Save();
/// int i = prefs.GetInt("testKey");
/// </example>
[Serializable]
public class JsonPlayerPrefs
{
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
    private List<PlayerPref> playerPrefs = new List<PlayerPref>();

    private readonly string savePath;

    // Runtime decoded-keys cache + versioning
    private int keyCacheVersion = 0;
    private int cachedKeysVersion = -1;
    private List<string> cachedDecodedKeys = null;

    /// <summary>
    /// Expose the current key cache version for consumers that want to detect changes.
    /// </summary>
    public int KeyCacheVersion => keyCacheVersion;

    /// <summary>
    /// Fired when the internal keyset changes (keys added/removed). The int is the new keyCacheVersion.
    /// </summary>
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
            return;

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
                    Debug.LogWarning(
                        $"JsonPlayerPrefs: invalid or empty JSON at {savePath}, starting fresh."
                    );
                    playerPrefs = new List<PlayerPref>();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning(
                $"JsonPlayerPrefs: failed to load prefs from {savePath}: {ex.Message}"
            );
            playerPrefs = new List<PlayerPref>();
        }
    }

    #endregion

    #region Public API - Get

    public float GetFloat(string key, float defaultValue = 0f)
    {
        if (TryGetPlayerPref(key, out PlayerPref playerPref))
        {
            if (
                float.TryParse(
                    playerPref.value,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out float value
                )
            )
            {
                return value;
            }
        }
        return defaultValue;
    }

    public int GetInt(string key, int defaultValue = 0)
    {
        if (TryGetPlayerPref(key, out PlayerPref playerPref))
        {
            if (int.TryParse(playerPref.value, out int value))
            {
                return value;
            }
        }
        return defaultValue;
    }

    public string GetString(string key, string defaultValue = "")
    {
        if (TryGetPlayerPref(key, out PlayerPref playerPref))
        {
            return playerPref.value;
        }
        return defaultValue;
    }

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

    public void SetFloat(string key, float value)
    {
        SetString(key, value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public void SetInt(string key, int value)
    {
        SetString(key, value.ToString());
    }

    public void SetString(string key, string value)
    {
        var encoded = EncodeKey(key);

        if (TryGetPlayerPrefInternal(encoded, out PlayerPref playerPref))
        {
            playerPref.value = value;
        }
        else
        {
            playerPrefs.Add(new PlayerPref(encoded, value));
            InvalidateKeyCache();
        }
    }

    public void SetBool(string key, bool value)
    {
        SetInt(key, value ? 1 : 0);
    }

    #endregion

    #region Public API - Delete

    /// <summary>
    /// Removes all keys and values from the preferences. Use with caution.
    /// </summary>
    public void DeleteAll()
    {
        if (playerPrefs.Count > 0)
        {
            playerPrefs.Clear();
            InvalidateKeyCache();
        }
    }

    /// <summary>
    /// Removes key and its corresponding value from the preferences.
    /// </summary>
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

    /// <summary>
    /// Writes all modified preferences to disk.
    /// </summary>
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
                yield return k;
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
            yield return k;
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
            return string.Empty;

        try
        {
            var bytes = Encoding.UTF8.GetBytes(key);
            return Convert.ToBase64String(bytes);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"JsonPlayerPrefs: failed to encode key '{key}': {ex.Message}");
            return key;
        }
    }

    private string DecodeKey(string encoded)
    {
        if (string.IsNullOrEmpty(encoded))
            return encoded;

        try
        {
            var bytes = Convert.FromBase64String(encoded);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"JsonPlayerPrefs: failed to decode key: {ex.Message}");
            return encoded;
        }
    }

    #endregion
}
