using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace Turnroot.Utilities
{
    /// <summary>
    /// Centralized logging utility for the Turnroot framework.
    /// </summary>
    public static class TurnrootLogger
    {
        public enum LogLevel
        {
            Info,
            Warning,
            Error,
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
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
                    Debug.Log($"[Turnroot] {message}");
                    break;
            }
        }
    }
}
