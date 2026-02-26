using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    /// <summary>
    /// Deals additional damage to one or more target enemies.
    /// </summary>
    [CreateNodeMenu("Events/Offensive/Deal Additional Damage")]
    [NodeLabel("Deals additional damage to the target")]
    public class DealAdditionalDamageNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Input]
        [Tooltip("The amount of additional damage to deal")]
        public FloatValue damageAmount;

        [Input]
        [Tooltip(
            "If true, deals damage to all targeted enemies in Targets list; if false, only first target"
        )]
        public BoolValue affectAllTargets;

        public override void Execute(BattleContext context)
        {
            if (!ValidateHasTargets(context))
            {
                return;
            }

            float damage = GetInputFloat("damageAmount", 0f);
            bool shouldAffectAll = GetInputBool("affectAllTargets", false);
            var dmgPort = GetInputPort("damageAmount");
            if (dmgPort == null || !dmgPort.IsConnected)
            {
                "DealAdditionalDamageNode: 'damageAmount' input not provided".LogWarning();
                return;
            }

            int affected = ExecuteOnTargets(
                context,
                shouldAffectAll,
                target => DealDamage(context, target, damage)
            );

            $"DealAdditionalDamage: Dealt {damage} damage to {affected} target(s)".LogInfo();
        }
    }
}
