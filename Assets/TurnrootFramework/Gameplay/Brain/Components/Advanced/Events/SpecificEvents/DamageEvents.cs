using Turnroot.Characters;

namespace Turnroot.Gameplay.Brain.Events
{
    /// <summary>
    /// Published when any unit takes damage.
    /// </summary>
    public class UnitDamagedEvent : BattleEvent
    {
        public CharacterInstance Target { get; }
        public CharacterInstance Source { get; }
        public int DamageAmount { get; }
        public int RemainingHP { get; }
        public bool WasLethal { get; }
        public string DamageSource { get; }

        public UnitDamagedEvent(
            CharacterInstance target,
            CharacterInstance source,
            int damage,
            int remainingHP,
            string damageSource = null
        )
        {
            Target = target;
            Source = source;
            DamageAmount = damage;
            RemainingHP = remainingHP;
            WasLethal = remainingHP <= 0;
            DamageSource = damageSource ?? "Attack";
        }
    }

    /// <summary>
    /// Published when a unit is healed.
    /// </summary>
    public class UnitHealedEvent : BattleEvent
    {
        public CharacterInstance Target { get; }
        public CharacterInstance Healer { get; }
        public int HealAmount { get; }
        public int NewHP { get; }

        public UnitHealedEvent(
            CharacterInstance target,
            CharacterInstance healer,
            int heal,
            int newHP
        )
        {
            Target = target;
            Healer = healer;
            HealAmount = heal;
            NewHP = newHP;
        }
    }
}
