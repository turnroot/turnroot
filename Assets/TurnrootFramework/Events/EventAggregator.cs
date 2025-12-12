using System;
using System.Collections.Generic;
using Turnroot.Characters.CharacterClass;
using Turnroot.Gameplay.Combat;
using UnityEngine;

namespace Turnroot.Events
{
    /// <summary>
    /// Event aggregator for decoupling components and standardizing event publishing.
    /// Implements mediator pattern to reduce circular dependencies.
    /// </summary>
    public class EventAggregator
    {
        private static EventAggregator _instance;
        public static EventAggregator Instance => _instance ??= new EventAggregator();

        private readonly Dictionary<Type, List<Delegate>> _subscribers = new();
        private readonly object _lock = new();

        /// <summary>
        /// Subscribe to an event type.
        /// </summary>
        public void Subscribe<TEvent>(Action<TEvent> handler)
            where TEvent : IGameEvent
        {
            if (handler == null)
            {
                return;
            }

            lock (_lock)
            {
                var eventType = typeof(TEvent);
                if (!_subscribers.ContainsKey(eventType))
                {
                    _subscribers[eventType] = new List<Delegate>();
                }

                _subscribers[eventType].Add(handler);
            }
        }

        /// <summary>
        /// Unsubscribe from an event type.
        /// </summary>
        public void Unsubscribe<TEvent>(Action<TEvent> handler)
            where TEvent : IGameEvent
        {
            if (handler == null)
            {
                return;
            }

            lock (_lock)
            {
                var eventType = typeof(TEvent);
                if (_subscribers.ContainsKey(eventType))
                {
                    _subscribers[eventType].Remove(handler);
                }
            }
        }

        /// <summary>
        /// Publish an event to all subscribers.
        /// </summary>
        public void Publish<TEvent>(TEvent gameEvent)
            where TEvent : IGameEvent
        {
            if (gameEvent == null)
            {
                return;
            }

            List<Delegate> handlers;
            lock (_lock)
            {
                var eventType = typeof(TEvent);
                if (!_subscribers.ContainsKey(eventType))
                {
                    return;
                }

                // Create a copy to avoid modification during iteration
                handlers = new List<Delegate>(_subscribers[eventType]);
            }

            foreach (var handler in handlers)
            {
                try
                {
                    ((Action<TEvent>)handler)(gameEvent);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error handling event {typeof(TEvent).Name}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Clear all subscriptions.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _subscribers.Clear();
            }
        }

        /// <summary>
        /// Get subscriber count for a specific event type (for diagnostics).
        /// </summary>
        public int GetSubscriberCount<TEvent>()
            where TEvent : IGameEvent
        {
            lock (_lock)
            {
                var eventType = typeof(TEvent);
                return _subscribers.ContainsKey(eventType) ? _subscribers[eventType].Count : 0;
            }
        }
    }

    /// <summary>
    /// Base interface for all game events.
    /// </summary>
    public interface IGameEvent
    {
        /// <summary>
        /// Timestamp when the event was created.
        /// </summary>
        DateTime Timestamp { get; }
    }

    /// <summary>
    /// Base class for game events with automatic timestamp.
    /// </summary>
    public abstract class GameEvent : IGameEvent
    {
        public DateTime Timestamp { get; }

        protected GameEvent()
        {
            Timestamp = DateTime.UtcNow;
        }
    }

    // Example event types for standardized event publishing

    /// <summary>
    /// Event published when a character's state changes.
    /// </summary>
    public class CharacterStateChangedEvent : GameEvent
    {
        public Characters.CharacterInstance Character { get; set; }
        public string StateChange { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }
    }

    /// <summary>
    /// Event published when a character levels up.
    /// </summary>
    public class CharacterLevelUpEvent : GameEvent
    {
        public Characters.CharacterInstance Character { get; set; }
        public int NewLevel { get; set; }
        public Dictionary<string, int> StatGains { get; set; }
    }

    /// <summary>
    /// Event published when a character changes class.
    /// </summary>
    public class CharacterClassChangedEvent : GameEvent
    {
        public Characters.CharacterInstance Character { get; set; }
        public CharacterClassData OldClass { get; set; }
        public CharacterClassData NewClass { get; set; }
        public bool IsFirstTime { get; set; }
    }

    /// <summary>
    /// Event published when a character learns a skill.
    /// </summary>
    public class CharacterSkillLearnedEvent : GameEvent
    {
        public Characters.CharacterInstance Character { get; set; }
        public Skill Skill { get; set; }
        public string Source { get; set; }
    }

    /// <summary>
    /// Event published when a character is defeated.
    /// </summary>
    public class CharacterDefeatedEvent : GameEvent
    {
        public Characters.CharacterInstance Character { get; set; }
        public Characters.CharacterInstance DefeatedBy { get; set; }
    }

    /// <summary>
    /// Event published when battle starts.
    /// </summary>
    public class BattleStartedEvent : GameEvent
    {
        public string BattleId { get; set; }
        public List<Characters.CharacterInstance> PlayerTeam { get; set; }
        public List<Characters.CharacterInstance> EnemyTeam { get; set; }
    }

    /// <summary>
    /// Event published when battle ends.
    /// </summary>
    public class BattleEndedEvent : GameEvent
    {
        public string BattleId { get; set; }
        public BattleExitType ExitType { get; set; }
    }
}
