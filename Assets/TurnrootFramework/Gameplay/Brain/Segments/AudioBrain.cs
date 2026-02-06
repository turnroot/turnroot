using Turnroot.Gameplay.Brain.Events;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Manages audio systems and sound playback within the brain framework.
    /// </summary>
    public class AudioBrain : BrainComponent
    {
        protected override EventPriority GetSubscriptionPriority() => EventPriority.Low;

        protected override void SubscribeToBrainEvents() { }

        protected override void UnsubscribeFromBrainEvents() { }

        protected override void Awake() => base.Awake();
    }
}
