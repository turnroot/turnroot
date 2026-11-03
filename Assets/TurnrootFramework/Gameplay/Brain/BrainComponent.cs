using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Base class for all brain components that need to subscribe to Brain events.
    /// Handles common initialization and cleanup patterns.
    /// </summary>
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
        /// Subscribe to Brain events. Override in derived classes to add specific subscriptions.
        /// </summary>
        protected abstract void SubscribeToBrainEvents();

        /// <summary>
        /// Unsubscribe from Brain events. Override in derived classes to remove specific subscriptions.
        /// </summary>
        protected abstract void UnsubscribeFromBrainEvents();
    }
}
