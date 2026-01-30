using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
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

        [Tooltip("Test value for damage in editor mode")]
        public float testDamage = 15f;

        [Tooltip("Test value for AoE radius in editor mode")]
        public float testRadius = 2f;

        public override void Execute(BattleContext context)
        {
            if (!ValidateHasTargets(context))
            {
                return;
            }

            float damage = GetInputFloat("damageAmount", testDamage);
            float radius = GetInputFloat("aoeRadius", testRadius);

            int affectedCount = ExecuteOnAllTargets(
                context,
                target => DealDamage(context, target, damage)
            );

            TurnrootLogger.Log(
                $"AreaOfEffectDamage: Dealt {damage} damage to {affectedCount} enemies in {radius} tile radius"
            );
        }
    }
}
