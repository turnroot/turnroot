using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using UnityEngine;

namespace Turnroot.Gameplay.Brain.Commands
{
    public class HealCommand : CommandBase
    {
        public string HealerId { get; }
        public string TargetId { get; }

        public HealCommand(string healerId, string targetId, int turn)
            : base(turn)
        {
            HealerId = healerId;
            TargetId = targetId;
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
            return true;
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

            // TODO: Calculate heal amount based on stats, items, etc.
            int healAmount = 20;
            health.SetCurrent(Mathf.Min(health.Max, health.Current + healAmount));

            var healer = FindUnit(context, HealerId);

            context.Brain?.Publish(
                new Events.UnitHealedEvent(target, healer, healAmount, (int)health.Current)
            );

            return true;
        }
    }
}
