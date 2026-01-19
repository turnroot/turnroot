using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;

namespace Turnroot.Gameplay.Brain.Commands
{
    /// <summary>
    /// Command to deal damage to a unit.
    /// </summary>
    public class DamageCommand : CommandBase
    {
        public string AttackerId { get; }
        public string TargetId { get; }
        public int Damage { get; }

        public DamageCommand(string attackerId, string targetId, int damage, int turn)
            : base(turn)
        {
            AttackerId = attackerId;
            TargetId = targetId;
            Damage = damage;
        }

        public override bool Execute(BattleContext context)
        {
            var target = FindUnit(context, TargetId);
            if (target == null)
            {
                return false;
            }

            var health = target.GetBoundedStat(Characters.Stats.BoundedStatType.Health);
            if (health == null)
            {
                return false;
            }

            UndoState["prevHP"] = health.Current;
            UndoState["wasDefeated"] = target.IsDefeatedInCurrentBattle;

            health.SetCurrent(health.Current - Damage);

            var attacker = FindUnit(context, AttackerId);

            // Track last attacked target on the attacker for this battle
            if (attacker != null)
            {
                // Save previous value for undo
                UndoState["prevLastTarget"] = attacker.LastAttackedTarget;
                attacker.LastAttackedTarget = target;
            }

            // Track last attacker per target in the BattleContext
            if (context != null)
            {
                UndoState["prevLastAttackerOfTarget"] = context.GetLastAttacker(target);
                // Also save and set target's own LastAttacker field for convenience
                UndoState["prevTargetLastAttacker"] = target.LastAttacker;
                // Use BattleBrain wrapper to ensure events are published and context mapping updated
                context
                    .Brain?.GetComponent<BattleBrain>()
                    ?.SetLastAttacker(context, target, attacker);
            }

            context.Brain?.Publish(
                new Events.UnitDamagedEvent(target, attacker, Damage, (int)health.Current)
            );

            if (health.Current <= 0)
            {
                target.IsDefeatedInCurrentBattle = true;
                context.Brain?.Publish(new Events.UnitDefeatedEvent(target, attacker));
            }

            return true;
        }

        public override bool Undo(BattleContext context)
        {
            var target = FindUnit(context, TargetId);
            if (target == null)
            {
                return false;
            }

            var health = target.GetBoundedStat(Characters.Stats.BoundedStatType.Health);
            if (health == null || !UndoState.TryGetValue("prevHP", out var prev))
            {
                return false;
            }

            health.SetCurrent((float)prev);
            target.IsDefeatedInCurrentBattle = (bool)UndoState["wasDefeated"];

            // Restore previous LastAttackedTarget on the attacker if present
            var attacker = FindUnit(context, AttackerId);
            if (attacker != null && UndoState.TryGetValue("prevLastTarget", out var prevLast))
            {
                attacker.LastAttackedTarget = prevLast as CharacterInstance;
            }

            // Restore previous last-attacker mapping for the target and the target's own LastAttacker
            if (target != null)
            {
                if (UndoState.TryGetValue("prevLastAttackerOfTarget", out var prevLastAttacker))
                {
                    var bb = context.Brain?.GetComponent<BattleBrain>();
                    bb?.SetLastAttacker(context, target, prevLastAttacker as CharacterInstance);
                }
                else if (UndoState.TryGetValue("prevTargetLastAttacker", out var prevTargetLast))
                {
                    var bb = context.Brain?.GetComponent<BattleBrain>();
                    bb?.SetLastAttacker(context, target, prevTargetLast as CharacterInstance);
                }
            }

            return true;
        }
    }
}
