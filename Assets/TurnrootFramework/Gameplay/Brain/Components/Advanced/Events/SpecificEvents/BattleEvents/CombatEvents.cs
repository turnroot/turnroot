using Turnroot.Characters;
using Turnroot.GameSettings;

namespace Turnroot.Gameplay.Brain.Events
{
    /// <summary>
    /// Published when an attack is initiated (before damage calculation).
    /// </summary>
    public class AttackInitiatedEvent : BattleEvent
    {
        public CharacterInstance Attacker { get; }
        public CharacterInstance Defender { get; }
        public bool IsCritical { get; set; }
        public bool WillMiss { get; set; }

        public AttackInitiatedEvent(CharacterInstance attacker, CharacterInstance defender)
        {
            Attacker = attacker;
            Defender = defender;
        }
    }

    /// <summary>
    /// Published when a critical hit occurs.
    /// </summary>
    public class CriticalHitEvent : BattleEvent
    {
        public CharacterInstance Attacker { get; }
        public CharacterInstance Target { get; }
        public float DamageMultiplier { get; }

        // Use configured multiplier from GameplayGeneralSettings when not explicitly provided.
        public CriticalHitEvent(CharacterInstance attacker, CharacterInstance target)
        {
            Attacker = attacker;
            Target = target;
            var settings = GameplayGeneralSettings.Instance;
            DamageMultiplier = settings != null ? settings.GetCriticalHitMultiplier() : 2f;
        }

        public CriticalHitEvent(
            CharacterInstance attacker,
            CharacterInstance target,
            float multiplier
        )
        {
            Attacker = attacker;
            Target = target;
            DamageMultiplier = multiplier;
        }
    }
}
