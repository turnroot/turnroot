using Turnroot.Gameplay.Brain.Events;

namespace Turnroot.Gameplay.Brain
{
    public class UnitAppearanceBrain : BrainComponent
    {
        protected override EventPriority GetSubscriptionPriority() => EventPriority.Low;

        protected override void SubscribeToBrainEvents() { }

        protected override void UnsubscribeFromBrainEvents() { }

        protected override void Awake() => base.Awake();
    }
}
