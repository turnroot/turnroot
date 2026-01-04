using Turnroot.Gameplay.Brain.Events;

namespace Turnroot.Gameplay.Brain
{
    public class PlayerInputBrain : BrainComponent
    {
        protected override EventPriority GetSubscriptionPriority() => EventPriority.Low;

        protected override void SubscribeToBrainEvents()
        {
            // Subscribe to events if needed
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            // No subscriptions to clean up yet
        }
    }
}
