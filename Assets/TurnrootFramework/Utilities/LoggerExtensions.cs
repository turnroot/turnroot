namespace Turnroot.Utilities
{
    /// <summary>
    /// Extension methods for cleaner, more fluent logging syntax.
    /// Reduces verbosity when logging with context.
    /// </summary>
    public static class LoggerExtensions
    {
        /// <summary>
        /// Logs an error message with optional context prefix.
        /// </summary>
        /// <param name="message">The error message to log</param>
        /// <param name="context">Optional context prefix (e.g., "BattleBrain")</param>
        public static void LogError(this string message, string context = null)
        {
            var fullMessage = context != null ? $"{context}: {message}" : message;
            TurnrootLogger.Log(fullMessage, TurnrootLogger.LogLevel.Error);
        }

        /// <summary>
        /// Logs a warning message with optional context prefix.
        /// </summary>
        /// <param name="message">The warning message to log</param>
        /// <param name="context">Optional context prefix (e.g., "BattleBrain")</param>
        public static void LogWarning(this string message, string context = null)
        {
            var fullMessage = context != null ? $"{context}: {message}" : message;
            TurnrootLogger.Log(fullMessage, TurnrootLogger.LogLevel.Warning);
        }

        /// <summary>
        /// Logs an info message with optional context prefix.
        /// </summary>
        /// <param name="message">The info message to log</param>
        /// <param name="context">Optional context prefix (e.g., "BattleBrain")</param>
        public static void LogInfo(this string message, string context = null)
        {
            var fullMessage = context != null ? $"{context}: {message}" : message;
            TurnrootLogger.Log(fullMessage, TurnrootLogger.LogLevel.Info);
        }
    }
}
