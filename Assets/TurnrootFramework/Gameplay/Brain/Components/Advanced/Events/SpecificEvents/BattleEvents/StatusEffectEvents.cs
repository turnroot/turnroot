using Turnroot.Characters;

namespace Turnroot.Gameplay.Brain.Events
{
    /// <summary>
    /// Published when a status effect is applied to a unit.
    /// </summary>
    public class StatusEffectAppliedEvent : BattleEvent
    {
        public CharacterInstance Target { get; }
        public CharacterInstance Source { get; }
        public string EffectId { get; }
        public int Duration { get; }

        public StatusEffectAppliedEvent(
            CharacterInstance target,
            CharacterInstance source,
            string effectId,
            int duration
        )
        {
            Target = target;
            Source = source;
            EffectId = effectId;
            Duration = duration;
        }
    }

    /// <summary>
    /// Published when a status effect expires or is removed.
    /// </summary>
    public class StatusEffectRemovedEvent : BattleEvent
    {
        public CharacterInstance Target { get; }
        public string EffectId { get; }
        public bool WasDispelled { get; }

        public StatusEffectRemovedEvent(
            CharacterInstance target,
            string effectId,
            bool dispelled = false
        )
        {
            Target = target;
            EffectId = effectId;
            WasDispelled = dispelled;
        }
    }
}
