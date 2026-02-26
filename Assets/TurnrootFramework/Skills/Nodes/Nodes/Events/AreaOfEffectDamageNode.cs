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
            var radPort = GetInputPort("aoeRadius");
            if (radPort == null || !radPort.IsConnected)
            {
                "AreaOfEffectDamageNode: 'aoeRadius' input not provided".LogWarning();
                return;
            }
            float radius = GetInputFloat("aoeRadius", 0f);

            int affectedCount = ExecuteOnAllTargets(
                context,
                target => DealDamage(context, target, damage)
            );

            $"AreaOfEffectDamage: Dealt {damage} damage to {affectedCount} enemies in {radius} tile radius".LogInfo();
        }
    }
}
