namespace Turnroot.Gameplay.Brain.Events
{
    /// <summary>
    /// Defines the priority levels for event handlers.
    /// Lower numeric values execute first.
    /// Gaps between values allow insertion of new priorities if needed.
    /// </summary>
    public enum EventPriority
    {
        /// <summary>
        /// Highest priority (0). Used for critical state changes that must complete first.
        /// Handlers at this priority are protected from exceptions in lower priority handlers.
        /// Use for: State persistence, data validation, critical game logic.
        /// </summary>
        Highest = 0,

        /// <summary>
        /// High priority (100). Used for important game logic that depends on state changes.
        /// Use for: Core gameplay mechanics, combat calculations, AI decisions.
        /// </summary>
        High = 100,

        /// <summary>
        /// Normal priority (500). Default priority for most handlers.
        /// Use for: Standard gameplay logic, general event handling.
        /// </summary>
        Normal = 500,

        /// <summary>
        /// Low priority (900). Used for UI updates and visual feedback.
        /// Executes after game state is fully updated.
        /// Use for: UI updates, visual effects, audio cues.
        /// </summary>
        Low = 900,

        /// <summary>
        /// Lowest priority (1000). Used for analytics, logging, and non-critical systems.
        /// Should never affect gameplay and can tolerate delays.
        /// Use for: Analytics, achievement tracking, debug logging.
        /// </summary>
        Lowest = 1000,
    }

    /// <summary>
    /// Extension methods for EventPriority.
    /// </summary>
    public static class EventPriorityExtensions
    {
        /// <summary>
        /// Gets the numeric value of the priority for sorting.
        /// </summary>
        public static int ToSortValue(this EventPriority priority) => (int)priority;

        /// <summary>
        /// Returns true if this priority should execute before the other priority.
        /// </summary>
        public static bool ExecutesBefore(this EventPriority priority, EventPriority other) =>
            (int)priority < (int)other;

        /// <summary>
        /// Returns true if this priority is protected from exceptions in lower priority handlers.
        /// Only Highest priority handlers are protected.
        /// </summary>
        public static bool IsProtected(this EventPriority priority) =>
            priority == EventPriority.Highest;
    }
}
