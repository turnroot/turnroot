using System;
using Turnroot.Events;
using UnityEngine;

namespace Turnroot.Utilities
{
    /// <summary>
    /// Helper for tracking and publishing state mutations consistently.
    /// Ensures all state changes that affect game state publish events at the point of mutation.
    /// </summary>
    public static class StateMutationHelper
    {
        /// <summary>
        /// Wraps a state mutation with automatic event publishing.
        /// </summary>
        /// <typeparam name="TEvent">The event type to publish</typeparam>
        /// <param name="mutation">The mutation action to perform</param>
        /// <param name="eventFactory">Factory to create the event after mutation</param>
        /// <param name="validateBefore">Optional validation before mutation</param>
        /// <returns>True if mutation succeeded</returns>
        public static bool MutateAndPublish<TEvent>(
            Action mutation,
            Func<TEvent> eventFactory,
            Func<bool> validateBefore = null
        )
            where TEvent : IGameEvent
        {
            if (mutation == null || eventFactory == null)
            {
                Debug.LogWarning("StateMutationHelper: mutation or eventFactory is null");
                return false;
            }

            // Optional validation
            if (validateBefore != null && !validateBefore())
            {
                return false;
            }

            try
            {
                // Perform mutation
                mutation();

                // Create and publish event
                var gameEvent = eventFactory();
                EventAggregator.Instance.Publish(gameEvent);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"StateMutationHelper: Error during mutation: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Wraps a state mutation that returns a value with automatic event publishing.
        /// </summary>
        public static (bool success, TResult result) MutateAndPublish<TEvent, TResult>(
            Func<TResult> mutation,
            Func<TResult, TEvent> eventFactory,
            Func<bool> validateBefore = null
        )
            where TEvent : IGameEvent
        {
            if (mutation == null || eventFactory == null)
            {
                Debug.LogWarning("StateMutationHelper: mutation or eventFactory is null");
                return (false, default);
            }

            // Optional validation
            if (validateBefore != null && !validateBefore())
            {
                return (false, default);
            }

            try
            {
                // Perform mutation
                var result = mutation();

                // Create and publish event
                var gameEvent = eventFactory(result);
                EventAggregator.Instance.Publish(gameEvent);

                return (true, result);
            }
            catch (Exception ex)
            {
                Debug.LogError($"StateMutationHelper: Error during mutation: {ex.Message}");
                return (false, default);
            }
        }

        /// <summary>
        /// Tracks a value change and publishes event if value changed.
        /// </summary>
        public static bool ChangeValueAndPublish<TValue, TEvent>(
            ref TValue currentValue,
            TValue newValue,
            Func<TValue, TValue, TEvent> eventFactory
        )
            where TEvent : IGameEvent
        {
            if (Equals(currentValue, newValue))
            {
                return false; // No change
            }

            var oldValue = currentValue;
            currentValue = newValue;

            var gameEvent = eventFactory(oldValue, newValue);
            EventAggregator.Instance.Publish(gameEvent);

            return true;
        }

        /// <summary>
        /// Creates a state change event with before/after values.
        /// </summary>
        public static CharacterStateChangedEvent CreateStateChangeEvent(
            Characters.CharacterInstance character,
            string stateChange,
            object oldValue,
            object newValue
        )
        {
            return new CharacterStateChangedEvent
            {
                Character = character,
                StateChange = stateChange,
                OldValue = oldValue,
                NewValue = newValue
            };
        }
    }
}
