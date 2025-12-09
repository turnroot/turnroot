using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages long-term memory storage and retrieval for the brain system.
/// Provides batched save operations and event notifications for key set changes.
/// </summary>
public class LongTermMemory : MonoBehaviour
{
    private JsonPlayerPrefs prefs;
    private bool isDirty;

    /// <summary>
    /// Fired when the underlying keyset changes in JsonPlayerPrefs.
    /// Subscribers should use this to invalidate caches or react to keyset changes.
    /// The int value is the keyCacheVersion from JsonPlayerPrefs.
    /// </summary>
    public event Action<int> OnKeySetChanged;

    /// <summary>
    /// Expose the runtime key cache version from JsonPlayerPrefs.
    /// </summary>
    public int KeyCacheVersion => prefs?.KeyCacheVersion ?? 0;

    public void Awake()
    {
        InitializePrefs();
    }

    private void InitializePrefs()
    {
        prefs = new JsonPlayerPrefs(
            Application.persistentDataPath + "/TurnrootBrain/structured/.turnrootdata"
        );
        Debug.Log($"Brain LongTermMemory initialized at: {Application.persistentDataPath}");

        prefs.OnKeySetChanged += HandlePrefsKeySetChanged;
    }

    private void HandlePrefsKeySetChanged(int version)
    {
        OnKeySetChanged?.Invoke(version);

        var brain = gameObject.GetComponent<Assets.Turnroot.Gameplay.Brain.Brain>();
        brain?.PublishLtmKeyCacheUpdated(version);
    }

    /// <summary>
    /// Stores a string value in long-term memory.
    /// Changes are written to disk immediately unless deferred mode is active.
    /// </summary>
    public string Remember(string key, string value)
    {
        prefs.SetString(key, value);
        SaveImmediate();
        return value;
    }

    /// <summary>
    /// Stores an integer value in long-term memory.
    /// </summary>
    public int RememberInt(string key, int value)
    {
        prefs.SetInt(key, value);
        SaveImmediate();
        return value;
    }

    /// <summary>
    /// Stores a boolean value in long-term memory.
    /// </summary>
    public bool RememberBool(string key, bool value)
    {
        prefs.SetBool(key, value);
        SaveImmediate();
        return value;
    }

    /// <summary>
    /// Retrieves a string value from long-term memory.
    /// </summary>
    /// <returns>The string value, or null if the key does not exist.</returns>
    public string Recall(string key)
    {
        return prefs.GetString(key, null);
    }

    /// <summary>
    /// Retrieves an integer value from long-term memory.
    /// </summary>
    /// <returns>The integer value, or -1 if the key does not exist.</returns>
    public int RecallInt(string key)
    {
        return prefs.GetInt(key, -1);
    }

    /// <summary>
    /// Retrieves a boolean value from long-term memory.
    /// </summary>
    /// <returns>The boolean value, or false if the key does not exist.</returns>
    public bool RecallBool(string key)
    {
        return prefs.GetBool(key, false);
    }

    /// <summary>
    /// Removes a value from long-term memory.
    /// </summary>
    /// <param name="key">The key to forget.</param>
    /// <param name="permanent">If true, saves the deleted value with a "_deleted" suffix.</param>
    public void Forget(string key, bool permanent = false)
    {
        if (permanent)
        {
            string value = prefs.GetString(key, null);
            if (value != null)
            {
                Remember(key + "_deleted", value);
            }
        }

        prefs.DeleteKey(key);
        SaveImmediate();
    }

    /// <summary>
    /// Removes an integer value from long-term memory.
    /// </summary>
    public void ForgetInt(string key, bool permanent = false)
    {
        if (permanent)
        {
            int value = prefs.GetInt(key, -1);
            if (value != -1)
            {
                RememberInt(key + "_deleted", value);
            }
        }

        prefs.DeleteKey(key);
        SaveImmediate();
    }

    /// <summary>
    /// Removes a boolean value from long-term memory.
    /// </summary>
    public void ForgetBool(string key, bool permanent = false)
    {
        if (permanent)
        {
            int intValue = prefs.GetInt(key, -1);
            if (intValue != -1)
            {
                RememberInt(key + "_deleted", intValue);
            }
        }

        prefs.DeleteKey(key);
        SaveImmediate();
    }

    /// <summary>
    /// Retrieves all keys from long-term memory that start with the specified prefix.
    /// </summary>
    public List<string> RecallKeysByPrefix(string prefix)
    {
        var keys = new List<string>();
        if (prefs == null)
        {
            return keys;
        }

        foreach (var key in prefs.GetAllKeys())
        {
            if (key != null && key.StartsWith(prefix))
            {
                keys.Add(key);
            }
        }

        return keys;
    }

    /// <summary>
    /// Writes all pending changes to disk immediately.
    /// </summary>
    private void SaveImmediate()
    {
        prefs.Save();
    }

    private void OnDestroy()
    {
        if (prefs != null)
        {
            prefs.OnKeySetChanged -= HandlePrefsKeySetChanged;
        }
    }
}
