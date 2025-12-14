using System;
using System.Collections.Generic;
using UnityEngine;

namespace Turnroot.Gameplay.Brain.Events
{
    /// <summary>
    /// A priority-based event bus that allows handlers to execute in a defined order.
    /// Supports immediate and deferred event publishing, event coalescing, and predicate filtering.
    /// </summary>
    public class PriorityEventBus
    {
        // Handler storage indexed by event type
        private readonly Dictionary<Type, IHandlerList> _handlers = new();

        // Deferred event queues indexed by event type
        private readonly Dictionary<Type, IDeferredEventQueue> _deferredQueues = new();

        // Lock for thread safety during handler modification
        private readonly object _lock = new();

        // Tracking for event coalescing
        private readonly Dictionary<Type, HashSet<int>> _coalescedEvents = new();

        // Current frame for coalescing checks
        private int _currentFrame;

        #region Subscription Management

        /// <summary>
        /// Subscribe to an event type with a specific priority.
        /// </summary>
        /// <typeparam name="T">The type of event to subscribe to.</typeparam>
        /// <param name="handler">The handler to invoke when the event is published.</param>
        /// <param name="priority">The priority level for this handler.</param>
        /// <param name="predicate">Optional predicate to filter which events this handler receives.</param>
        public void Subscribe<T>(
            Action<T> handler,
            EventPriority priority = EventPriority.Normal,
            Func<T, bool> predicate = null
        )
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            lock (_lock)
            {
                var handlerList = GetOrCreateHandlerList<T>();
                handlerList.Add(handler, priority, predicate);
            }
        }

        /// <summary>
        /// Unsubscribe a handler from an event type.
        /// </summary>
        /// <typeparam name="T">The type of event to unsubscribe from.</typeparam>
        /// <param name="handler">The handler to remove.</param>
        /// <returns>True if the handler was found and removed, false otherwise.</returns>
        public bool Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null)
            {
                return false;
            }

            lock (_lock)
            {
                var type = typeof(T);
                if (_handlers.TryGetValue(type, out var list) && list is HandlerList<T> typedList)
                {
                    return typedList.Remove(handler);
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if there are any subscribers for an event type.
        /// </summary>
        public bool HasSubscribers<T>()
        {
            lock (_lock)
            {
                var type = typeof(T);
                return _handlers.TryGetValue(type, out var list) && list.Count > 0;
            }
        }

        /// <summary>
        /// Gets the count of subscribers for an event type.
        /// </summary>
        public int GetSubscriberCount<T>()
        {
            lock (_lock)
            {
                var type = typeof(T);
                return _handlers.TryGetValue(type, out var list) ? list.Count : 0;
            }
        }

        /// <summary>
        /// Clears all subscriptions for a specific event type.
        /// </summary>
        public void ClearSubscriptions<T>()
        {
            lock (_lock)
            {
                var type = typeof(T);
                if (_handlers.TryGetValue(type, out var list))
                {
                    list.Clear();
                }
            }
        }

        /// <summary>
        /// Clears all subscriptions for all event types.
        /// </summary>
        public void ClearAllSubscriptions()
        {
            lock (_lock)
            {
                foreach (var list in _handlers.Values)
                {
                    list.Clear();
                }
                _handlers.Clear();
            }
        }

        #endregion

        #region Immediate Publishing

        /// <summary>
        /// Publishes an event immediately, invoking all handlers synchronously in priority order.
        /// Critical (Highest priority) handlers are protected from exceptions in lower priority handlers.
        /// </summary>
        /// <typeparam name="T">The type of event to publish.</typeparam>
        /// <param name="eventData">The event data to pass to handlers.</param>
        public void PublishImmediate<T>(T eventData)
        {
            IReadOnlyList<PriorityHandler<T>> sortedHandlers;

            lock (_lock)
            {
                var type = typeof(T);
                if (
                    !_handlers.TryGetValue(type, out var list)
                    || list is not HandlerList<T> typedList
                )
                {
                    return;
                }
                sortedHandlers = typedList.GetSortedHandlers();
            }

            if (sortedHandlers.Count == 0)
            {
                return;
            }

            // Track exceptions from non-critical handlers
            List<Exception> nonCriticalExceptions = null;

            foreach (var handler in sortedHandlers)
            {
                // Skip if predicate fails
                if (!handler.ShouldExecute(eventData))
                {
                    continue;
                }

                if (handler.Priority.IsProtected())
                {
                    // Critical handlers execute without try-catch protection
                    // They must complete regardless of what happens in lower priority handlers
                    handler.Handler(eventData);
                }
                else
                {
                    // Non-critical handlers are wrapped in try-catch
                    try
                    {
                        handler.Handler(eventData);
                    }
                    catch (Exception ex)
                    {
                        nonCriticalExceptions ??= new List<Exception>();
                        nonCriticalExceptions.Add(ex);
                        Debug.LogError(
                            $"[PriorityEventBus] Exception in handler for {typeof(T).Name} at priority {handler.Priority}: {ex}"
                        );
                    }
                }
            }

            // Log summary if there were exceptions
            if (nonCriticalExceptions != null && nonCriticalExceptions.Count > 0)
            {
                Debug.LogWarning(
                    $"[PriorityEventBus] {nonCriticalExceptions.Count} exception(s) occurred during event {typeof(T).Name} processing."
                );
            }
        }

        /// <summary>
        /// Publishes an event immediately. Alias for PublishImmediate.
        /// </summary>
        public void Publish<T>(T eventData) => PublishImmediate(eventData);

        #endregion

        #region Deferred Publishing

        /// <summary>
        /// Queues an event for deferred processing. Events are coalesced if identical.
        /// Call ProcessDeferredEvents at frame end to dispatch all queued events.
        /// </summary>
        /// <typeparam name="T">The type of event to queue.</typeparam>
        /// <param name="eventData">The event data to queue.</param>
        public void PublishDeferred<T>(T eventData)
        {
            lock (_lock)
            {
                var type = typeof(T);

                // Check for coalescing - skip if we already have this exact event queued this frame
                if (ShouldCoalesce(eventData))
                {
                    return;
                }

                var queue = GetOrCreateDeferredQueue<T>();
                queue.Enqueue(eventData, _currentFrame);

                // Track for coalescing
                TrackForCoalescing(eventData);
            }
        }

        /// <summary>
        /// Processes all deferred events for all event types.
        /// Call this at the end of each frame.
        /// </summary>
        public void ProcessDeferredEvents()
        {
            // Increment frame counter
            _currentFrame++;

            // Clear coalescing tracking for the new frame
            lock (_lock)
            {
                _coalescedEvents.Clear();
            }

            // Get all queues to process
            List<IDeferredEventQueue> queuesToProcess;
            lock (_lock)
            {
                queuesToProcess = new List<IDeferredEventQueue>(_deferredQueues.Values);
            }

            // Process each queue
            foreach (var queue in queuesToProcess)
            {
                queue.ProcessAll();
            }
        }

        /// <summary>
        /// Gets the number of deferred events waiting to be processed.
        /// </summary>
        public int GetDeferredEventCount()
        {
            int count = 0;
            lock (_lock)
            {
                foreach (var queue in _deferredQueues.Values)
                {
                    count += queue.Count;
                }
            }
            return count;
        }

        /// <summary>
        /// Clears all deferred events without processing them.
        /// </summary>
        public void ClearDeferredEvents()
        {
            lock (_lock)
            {
                foreach (var queue in _deferredQueues.Values)
                {
                    queue.Clear();
                }
                _coalescedEvents.Clear();
            }
        }

        #endregion

        #region Private Helpers

        private HandlerList<T> GetOrCreateHandlerList<T>()
        {
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var list))
            {
                list = new HandlerList<T>();
                _handlers[type] = list;
            }
            return (HandlerList<T>)list;
        }

        private DeferredEventQueue<T> GetOrCreateDeferredQueue<T>()
        {
            var type = typeof(T);
            if (!_deferredQueues.TryGetValue(type, out var queue))
            {
                queue = new DeferredEventQueue<T>(this);
                _deferredQueues[type] = queue;
            }
            return (DeferredEventQueue<T>)queue;
        }

        /// <summary>
        /// Checks if an event should be coalesced (deduplicated) based on its hash.
        /// Note: Uses GetHashCode() which may have collisions.
        /// </summary>
        // TODO: Improve coalescing mechanism if needed
        private bool ShouldCoalesce<T>(T eventData)
        {
            if (!_coalescedEvents.TryGetValue(typeof(T), out var hashes))
            {
                return false;
            }

            var hash = eventData?.GetHashCode() ?? 0;
            return hashes.Contains(hash);
        }

        private void TrackForCoalescing<T>(T eventData)
        {
            var type = typeof(T);
            if (!_coalescedEvents.TryGetValue(type, out var hashes))
            {
                hashes = new HashSet<int>();
                _coalescedEvents[type] = hashes;
            }

            var hash = eventData?.GetHashCode() ?? 0;
            hashes.Add(hash);
        }

        #endregion

        #region Nested Types

        /// <summary>
        /// Queue for deferred events of a specific type.
        /// </summary>
        private class DeferredEventQueue<T> : IDeferredEventQueue
        {
            private readonly PriorityEventBus _bus;
            private readonly Queue<DeferredEvent<T>> _queue = new();

            public int Count => _queue.Count;

            public DeferredEventQueue(PriorityEventBus bus)
            {
                _bus = bus;
            }

            public void Enqueue(T eventData, int frame)
            {
                _queue.Enqueue(new DeferredEvent<T>(eventData, frame));
            }

            public void ProcessAll()
            {
                while (_queue.Count > 0)
                {
                    var evt = _queue.Dequeue();
                    _bus.PublishImmediate(evt.EventData);
                }
            }

            public void Clear()
            {
                _queue.Clear();
            }
        }

        #endregion
    }
}
