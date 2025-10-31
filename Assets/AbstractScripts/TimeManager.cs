using UnityEngine;

public class TimeManager : Singleton<TimeManager>
{
    private float _defaultTimeScale = 1.0f;

    public static void SetTimeScale(float newScale)
    {
        Time.timeScale = Mathf.Max(0f, newScale);
    }

    public static void PauseGame()
    {
        Instance._defaultTimeScale = Time.timeScale;
        SetTimeScale(0f);
    }

    public static void ResumeGame()
    {
        SetTimeScale(Instance._defaultTimeScale);
    }
}
