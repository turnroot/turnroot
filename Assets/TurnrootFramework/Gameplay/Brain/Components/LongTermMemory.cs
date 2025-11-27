using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages long-term memory storage and retrieval for the brain system.
/// </summary>
public class LongTermMemory : MonoBehaviour
{
    JsonPlayerPrefs prefs;

    public void Awake()
    {
        prefs ??= new JsonPlayerPrefs(
            Application.persistentDataPath + "/TurnrootBrain/LongTermMemory.json"
        );
        Debug.Log("Brain LongTermMemory initialized at: " + Application.persistentDataPath);
    }

    /// <summary>
    /// Stores a string value in long-term memory.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void Remember(string key, string value)
    {
        prefs.SetString(key, value);
        prefs.Save();
    }

    /// <summary>
    /// Stores an integer value in long-term memory.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void RememberInt(string key, int value)
    {
        prefs.SetInt(key, value);
        prefs.Save();
    }

    /// <summary>
    /// Stores a boolean value in long-term memory.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void RememberBool(string key, bool value)
    {
        prefs.SetInt(key, value ? 1 : 0);
        prefs.Save();
    }

    /// <summary>
    /// Retrieves a string value from long-term memory.
    /// </summary>
    /// <param name="key"></param>
    /// <returns>
    /// The string value associated with the specified key, or null if the key does not exist.
    /// </returns>
    public string Recall(string key)
    {
        return prefs.GetString(key, null);
    }

    /// <summary>
    /// Retrieves an integer value from long-term memory.
    /// </summary>
    /// <param name="key"></param>
    /// <returns>
    /// The integer value associated with the specified key, or -1 if the key does not exist.
    /// </returns>
    public int RecallInt(string key)
    {
        return prefs.GetInt(key, -1);
    }

    /// <summary>
    /// Retrieves a boolean value from long-term memory.
    /// </summary>
    /// <param name="key"></param>
    /// <returns>
    /// The boolean value associated with the specified key, or false if the key does not exist.
    /// </returns>
    public bool RecallBool(string key)
    {
        int intValue = prefs.GetInt(key, -1);
        if (intValue == -1)
            return false;
        return intValue != 0;
    }

    /// <summary>
    /// Forgets a value from long-term memory. Soft delete by default
    /// </summary>
    /// <param name="key"></param>
    /// <param name="permanent">If true, the forgotten value is saved with a "_deleted" suffix.</param>
    public void Forget(string key, bool permanent = false)
    {
        string value = prefs.GetString(key, null);
        if (permanent)
            Remember(key + "_deleted", value);
        prefs.DeleteKey(key);
        prefs.Save();
    }

    /// <summary>
    /// Forgets an integer value from long-term memory. Soft delete by default
    /// </summary>
    /// <param name="key"></param>
    /// <param name="permanent">If true, the forgotten value is saved with a "_deleted" suffix.</param>
    public void ForgetInt(string key, bool permanent = false)
    {
        int value = prefs.GetInt(key, -1);
        if (permanent)
            RememberInt(key + "_deleted", value);
        prefs.DeleteKey(key);
        prefs.Save();
    }

    /// <summary>
    /// Forgets a boolean value from long-term memory. Soft delete by default
    /// </summary>
    /// <param name="key"></param>
    /// <param name="permanent">If true, the forgotten value is saved with a "_deleted" suffix.</param>
    public void ForgetBool(string key, bool permanent = false)
    {
        int intValue = prefs.GetInt(key, -1);
        if (permanent)
            RememberInt(key + "_deleted", intValue);
        prefs.DeleteKey(key);
        prefs.Save();
    }

    /// <summary>
    /// Retrieves all keys from long-term memory that start with the specified prefix.
    /// </summary>
    /// <param name="prefix" type="string">The prefix to filter keys by.</param>
    public List<string> RecallKeysByPrefix(string prefix)
    {
        var keys = new List<string>();
        if (prefs == null)
            return keys;

        foreach (var key in prefs.GetAllKeys())
        {
            if (key != null && key.StartsWith(prefix))
                keys.Add(key);
        }

        return keys;
    }
}
