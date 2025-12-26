using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
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

        [Tooltip("Test value for damage in editor mode")]
        public float testDamage = 10f;

        [Tooltip("Test value for affectAllTargets in editor mode")]
        public bool testAffectAll = false;

        public override void Execute(BattleContext context)
        {
            if (!ValidateHasTargets(context))
            {
                return;
            }

            float damage = GetInputFloat("damageAmount", testDamage);
            bool shouldAffectAll = GetInputBool("affectAllTargets", testAffectAll);

            int affected = ExecuteOnTargets(
                context,
                shouldAffectAll,
                target => DealDamage(context, target, damage)
            );

#if UNITY_EDITOR
            Debug.Log($"DealAdditionalDamage: Dealt {damage} damage to {affected} target(s)");
#endif
        }
    }
}
