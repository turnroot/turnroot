using UnityEngine;

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

    public void Remember(string key, string value)
    {
        prefs.SetString(key, value);
        prefs.Save();
    }

    public void RememberInt(string key, int value)
    {
        prefs.SetInt(key, value);
        prefs.Save();
    }

    public void RememberBool(string key, bool value)
    {
        prefs.SetInt(key, value ? 1 : 0);
        prefs.Save();
    }

    public string Recall(string key)
    {
        return prefs.GetString(key, null);
    }

    public int RecallInt(string key)
    {
        return prefs.GetInt(key, -1);
    }

    public bool RecallBool(string key)
    {
        int intValue = prefs.GetInt(key, -1);
        if (intValue == -1)
            return false;
        return intValue != 0;
    }

    public string Forget(string key)
    {
        string value = prefs.GetString(key, null);
        prefs.DeleteKey(key);
        prefs.Save();
        return value;
    }

    public string ForgetInt(string key)
    {
        int value = prefs.GetInt(key, -1);
        prefs.DeleteKey(key);
        prefs.Save();
        return value.ToString();
    }

    public bool ForgetBool(string key)
    {
        int intValue = prefs.GetInt(key, -1);
        prefs.DeleteKey(key);
        prefs.Save();
        if (intValue == -1)
            return false;
        return intValue != 0;
    }
}
