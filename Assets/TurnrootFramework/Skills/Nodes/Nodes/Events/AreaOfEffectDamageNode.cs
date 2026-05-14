using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    /// <summary>
    /// Deals damage to all enemies within an area of effect radius.
    /// </summary>
    [CreateNodeMenu("Events/Offensive/Area Of Effect Damage")]
    [NodeLabel("Deals damage to all targeted enemies in an area")]
    public class AreaOfEffectDamageNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Input]
        [Tooltip("The amount of damage to deal to each target")]
        public FloatValue damageAmount;

        [Input]
        [Tooltip("The radius of the area of effect")]
        public FloatValue aoeRadius;

        public override void Execute(BattleContext context)
        {
            if (!ValidateHasTargets(context))
            {
                return;
            }

            var dmgPort = GetInputPort("damageAmount");
            if (dmgPort == null || !dmgPort.IsConnected)
            {
                "AreaOfEffectDamageNode: 'damageAmount' input not provided".LogWarning();
                return;
            }
            float damage = GetInputFloat("damageAmount", 0f);

            // radius defaults to 1 if the port is not connected
            float radius = GetInputFloat("aoeRadius", 1f);

            if (context.Unit.UnitInstance == null)
            {
                "AreaOfEffectDamageNode: No caster unit in context".LogWarning();
                return;
            }

            var casterPos = context.Unit.UnitInstance.MapGridPosition;

            int affectedCount = 0;
            foreach (var target in context.Participants.Targets)
            {
                if (target == null)
                {
                    continue;
                }

                var targetPos = target.MapGridPosition;
                int distance =
                    Mathf.Abs(casterPos.x - targetPos.x) + Mathf.Abs(casterPos.y - targetPos.y);
                if (distance <= radius)
                {
                    DealDamage(context, target, damage);
                    affectedCount++;
                }
            }

            $"AreaOfEffectDamage: Dealt {damage} damage to {affectedCount} enemies within {radius} tile radius".LogInfo();
        }
    }
}
