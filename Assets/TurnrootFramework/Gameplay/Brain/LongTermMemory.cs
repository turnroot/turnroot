using UnityEngine;

public class LongTermMemory : MonoBehaviour
{
    JsonPlayerPrefs prefs;

    public void OnEnable()
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

    public string Recall(string key)
    {
        return prefs.GetString(key, null);
    }

    public string Forget(string key)
    {
        string value = prefs.GetString(key, null);
        prefs.DeleteKey(key);
        prefs.Save();
        return value;
    }
}
