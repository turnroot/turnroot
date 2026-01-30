using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace Turnroot.Utilities
{
    public static class TurnrootLogger
    {
        public enum LogLevel
        {
            Info,
            Warning,
            Error,
        }

        [Conditional("UNITY_EDITOR")]
        public static void Log(string message, LogLevel level = LogLevel.Info)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            switch (level)
            {
                case LogLevel.Warning:
                    Debug.LogWarning($"[Turnroot] {message}");
                    break;
                case LogLevel.Error:
                    Debug.LogError($"[Turnroot] {message}");
                    break;
                default:
                    TurnrootLogger.Log($"[Turnroot] {message}");
                    break;
            }
        }
    }
}
