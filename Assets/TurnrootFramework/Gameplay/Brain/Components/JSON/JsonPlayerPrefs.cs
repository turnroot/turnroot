using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// A replacement for Unity's PlayerPrefs that stores data in a JSON file.
/// </summary>
[Serializable]
public class JsonPlayerPrefs
{
    // EXAMPLE USAGE:
    // JsonPlayerPrefs prefs = new JsonPlayerPrefs(Application.persistentDataPath + "/Preferences.json");
    // prefs.SetInt("testKey", 18);
    // prefs.Save();
    // int i = prefs.GetInt("testKey");

    [Serializable]
    class PlayerPref
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
    List<PlayerPref> playerPrefs = new();

    // runtime decoded-keys cache + versioning
    int keyCacheVersion = 0; // increments when keys change
    int cachedKeysVersion = -1; // version of cachedDecodedKeys
    List<string> cachedDecodedKeys = null;

    /// <summary>
    /// Expose the current key cache version for consumers that want to detect changes.
    /// </summary>
    public int KeyCacheVersion => keyCacheVersion;

    /// <summary>
    /// Fired when the internal keyset changes (keys added/removed). The int is the new keyCacheVersion.
    /// </summary>
    public event Action<int> OnKeySetChanged;
    string savePath;

    // Constructor
    public JsonPlayerPrefs(string savePath)
    {
        this.savePath = savePath;
        // try to load existing data
        if (File.Exists(savePath))
        {
            try
            {
                using StreamReader reader = new(savePath);
                string json = reader.ReadToEnd();
                JsonPlayerPrefs data = JsonUtility.FromJson<JsonPlayerPrefs>(json);
                if (data != null && data.playerPrefs != null)
                {
                    this.playerPrefs = data.playerPrefs;
                }
                else
                {
                    // Malformed or empty JSON — fallback to empty prefs
                    Debug.LogWarning(
                        $"JsonPlayerPrefs: invalid or empty JSON at {savePath}, starting fresh."
                    );
                    this.playerPrefs = new List<PlayerPref>();
                }
            }
            catch (Exception ex)
            {
                // If reading/parsing fails, warn and continue with an empty prefs list
                Debug.LogWarning(
                    $"JsonPlayerPrefs: failed to load prefs from {savePath}: {ex.Message}"
                );
                this.playerPrefs = new List<PlayerPref>();
            }
        }
    }

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
        bool removed = false;
        for (int i = playerPrefs.Count - 1; i >= 0; i--) // in reverse since we're removing
        {
            if (playerPrefs[i].key == encoded)
            {
                playerPrefs.RemoveAt(i);
                removed = true;
            }
        }
        if (removed)
            InvalidateKeyCache();
    }

    /// <summary>
    /// Returns the value corresponding to key in the preference file if it exists.
    /// </summary>
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

    /// <summary>
    /// Returns the value corresponding to key in the preference file if it exists.
    /// </summary>
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

    /// <summary>
    /// Returns the value corresponding to key in the preference file if it exists.
    /// </summary>
    public string GetString(string key, string defaultValue = "")
    {
        if (TryGetPlayerPref(key, out PlayerPref playerPref))
        {
            return playerPref.value;
        }
        return defaultValue;
    }

    /// <summary>
    /// Returns true if key exists in the preferences.
    /// </summary>
    public bool HasKey(string key)
    {
        var encoded = EncodeKey(key);
        for (int i = 0; i < playerPrefs.Count; i++)
        {
            if (playerPrefs[i].key == encoded)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Writes all modified preferences to disk.
    /// </summary>
    public void Save()
    {
        // create directory if it doesn't already exist
        string directory = Path.GetDirectoryName(@savePath);
        Directory.CreateDirectory(directory);
        // serialize and save file
        string json = JsonUtility.ToJson(this);
        using StreamWriter writer = new(savePath);
        writer.WriteLine(json);
    }

    /// <summary>
    /// Sets the value of the preference identified by key.
    /// </summary>
    public void SetFloat(string key, float value)
    {
        SetString(key, value.ToString());
    }

    /// <summary>
    /// Sets the value of the preference identified by key.
    /// </summary>
    public void SetInt(string key, int value)
    {
        SetString(key, value.ToString());
    }

    /// <summary>
    /// Sets the value of the preference identified by key.
    /// </summary>
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

    public bool GetBool(string key, bool defaultValue = false)
    {
        int intValue = GetInt(key, defaultValue ? 1 : 0);
        return intValue != 0;
    }

    public void SetBool(string key, bool value)
    {
        SetInt(key, value ? 1 : 0);
    }

    bool TryGetPlayerPref(string key, out PlayerPref playerPref)
    {
        var encoded = EncodeKey(key);
        return TryGetPlayerPrefInternal(encoded, out playerPref);
    }

    // Internal lookup that expects an already-encoded key
    bool TryGetPlayerPrefInternal(string encodedKey, out PlayerPref playerPref)
    {
        playerPref = null;
        for (int i = 0; i < playerPrefs.Count; i++)
        {
            if (playerPrefs[i].key == encodedKey)
            {
                playerPref = playerPrefs[i];
                return true;
            }
        }
        return false;
    }

    internal IEnumerable<string> GetAllKeys()
    {
        // return cached decoded keys when cache version matches
        if (cachedDecodedKeys != null && cachedKeysVersion == keyCacheVersion)
        {
            foreach (var k in cachedDecodedKeys)
                yield return k;
            yield break;
        }

        // rebuild cache
        var list = new List<string>(playerPrefs.Count);
        foreach (var pref in playerPrefs)
        {
            list.Add(DecodeKeySafe(pref.key));
        }

        cachedDecodedKeys = list;
        cachedKeysVersion = keyCacheVersion;

        foreach (var k in cachedDecodedKeys)
            yield return k;
    }

    void InvalidateKeyCache()
    {
        keyCacheVersion++;
        cachedDecodedKeys = null;
        cachedKeysVersion = -1;
        try
        {
            OnKeySetChanged?.Invoke(keyCacheVersion);
        }
        catch { }
    }

    string EncodeKey(string key)
    {
        key ??= string.Empty;
        try
        {
            var bytes = Encoding.UTF8.GetBytes(key);
            return Convert.ToBase64String(bytes);
        }
        catch
        {
            return key;
        }
    }

    string DecodeKeySafe(string stored)
    {
        if (string.IsNullOrEmpty(stored))
            return stored;
        try
        {
            var bytes = Convert.FromBase64String(stored);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return stored;
        }
    }
}
