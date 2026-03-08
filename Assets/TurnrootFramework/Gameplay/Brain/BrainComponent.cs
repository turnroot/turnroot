using Turnroot.Gameplay.Brain.Events;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Base class for all brain components that need to subscribe to Brain events.
    /// Handles common initialization and cleanup patterns.
    /// </summary>
    /// <remarks>
    /// Components can specify their default event priority by overriding GetSubscriptionPriority().
    /// State-changing components should use EventPriority.Highest.
    /// UI components should use EventPriority.Low.
    /// Analytics/logging should use EventPriority.Lowest.
    /// </remarks>
    [RequireComponent(typeof(Brain))]
    public abstract class BrainComponent : MonoBehaviour
    {
        protected Brain _brain;

        public Brain Brain => _brain;

        protected virtual void Awake()
        {
            _brain = GetComponent<Brain>();
            if (!ValidationHelper.ValidateNotNull(_brain, nameof(_brain)))
            {
                $"{GetType().Name}: Brain component not found!".LogError();
                return;
            }
            SubscribeToBrainEvents();
        }

        protected virtual void OnDestroy()
        {
            if (_brain != null)
            {
                UnsubscribeFromBrainEvents();
            }
        }

        /// <summary>
        /// Gets the default subscription priority for this component.
        /// Override this in derived classes to specify custom priority.
        /// </summary>
        /// <returns>The default event priority for this component's subscriptions.</returns>
        /// <remarks>
        /// Priority guidelines:
        /// - Highest: State persistence, data validation, critical game logic
        /// - High: Core gameplay mechanics, combat calculations
        /// - Normal: Standard gameplay logic (default)
        /// - Low: UI updates, visual effects, audio cues
        /// - Lowest: Analytics, achievement tracking, debug logging
        /// </remarks>
        protected virtual EventPriority GetSubscriptionPriority() => EventPriority.Normal;

        /// <summary>
        /// Subscribe to Brain events. Override in derived classes to add specific subscriptions.
        /// Use GetSubscriptionPriority() when subscribing to the priority event bus.
        /// </summary>
        protected abstract void SubscribeToBrainEvents();

        /// <summary>
        /// Unsubscribe from Brain events. Override in derived classes to remove specific subscriptions.
        /// </summary>
        protected abstract void UnsubscribeFromBrainEvents();
    }
}
