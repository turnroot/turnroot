using System;
using System.Collections.Generic;

namespace Turnroot.Gameplay.Brain.Events
{
    /// <summary>
    /// Wraps an event handler with its priority and optional predicate filter.
    /// </summary>
    /// <typeparam name="T">The type of event data.</typeparam>
    internal class PriorityHandler<T>
    {
        public Action<T> Handler { get; }
        public EventPriority Priority { get; }
        public Func<T, bool> Predicate { get; }
        public int RegistrationOrder { get; }

        public PriorityHandler(
            Action<T> handler,
            EventPriority priority,
            Func<T, bool> predicate,
            int registrationOrder
        )
        {
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
            Priority = priority;
            Predicate = predicate;
            RegistrationOrder = registrationOrder;
        }

        /// <summary>
        /// Checks if this handler should execute for the given event data.
        /// </summary>
        public bool ShouldExecute(T eventData)
        {
            return Predicate == null || Predicate(eventData);
        }
    }

    /// <summary>
    /// Comparer for sorting priority handlers.
    /// Handlers are sorted by priority (ascending), then by registration order (ascending).
    /// </summary>
    /// <typeparam name="T">The type of event data.</typeparam>
    internal class PriorityHandlerComparer<T> : IComparer<PriorityHandler<T>>
    {
        public static readonly PriorityHandlerComparer<T> Instance = new();

        public int Compare(PriorityHandler<T> x, PriorityHandler<T> y)
        {
            if (x == null && y == null)
            {
                return 0;
            }

            if (x == null)
            {
                return -1;
            }

            if (y == null)
            {
                return 1;
            }

            int priorityComparison = x.Priority.ToSortValue().CompareTo(y.Priority.ToSortValue());
            if (priorityComparison != 0)
            {
                return priorityComparison;
            }

            // Same priority, use registration order for stable sorting
            return x.RegistrationOrder.CompareTo(y.RegistrationOrder);
        }
    }

    /// <summary>
    /// Non-generic wrapper for event type handlers.
    /// Used for storing handlers of different event types in a single dictionary.
    /// </summary>
    internal interface IHandlerList
    {
        void Clear();
        int Count { get; }
    }

    /// <summary>
    /// List of priority handlers for a specific event type.
    /// Maintains handlers in sorted order by priority.
    /// </summary>
    /// <typeparam name="T">The type of event data.</typeparam>
    internal class HandlerList<T> : IHandlerList
    {
        private readonly List<PriorityHandler<T>> _handlers = new();
        private int _nextRegistrationOrder;
        private bool _isSorted = true;

        public int Count => _handlers.Count;

        public void Add(Action<T> handler, EventPriority priority, Func<T, bool> predicate = null)
        {
            var wrapper = new PriorityHandler<T>(
                handler,
                priority,
                predicate,
                _nextRegistrationOrder++
            );
            _handlers.Add(wrapper);
            _isSorted = false;
        }

        public bool Remove(Action<T> handler)
        {
            for (int i = _handlers.Count - 1; i >= 0; i--)
            {
                if (_handlers[i].Handler == handler)
                {
                    _handlers.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public void Clear()
        {
            _handlers.Clear();
            _nextRegistrationOrder = 0;
            _isSorted = true;
        }

        /// <summary>
        /// Gets handlers sorted by priority. Sorts lazily on demand.
        /// </summary>
        public IReadOnlyList<PriorityHandler<T>> GetSortedHandlers()
        {
            if (!_isSorted)
            {
                _handlers.Sort(PriorityHandlerComparer<T>.Instance);
                _isSorted = true;
            }
            return _handlers;
        }
    }

    /// <summary>
    /// Represents a deferred event waiting to be processed.
    /// </summary>
    internal readonly struct DeferredEvent<T>
    {
        public T EventData { get; }
        public int FrameQueued { get; }

        public DeferredEvent(T eventData, int frameQueued)
        {
            EventData = eventData;
            FrameQueued = frameQueued;
        }
    }

    /// <summary>
    /// Non-generic interface for processing deferred events.
    /// </summary>
    internal interface IDeferredEventQueue
    {
        void ProcessAll();
        void Clear();
        int Count { get; }
    }
}
