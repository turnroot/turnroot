using Assets.Prototypes.Gameplay.Combat.FundamentalComponents.Battles;
using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Offensive/Deal Additional Damage")]
    [NodeLabel("Deals additional damage to the target")]
    public class DealAdditionalDamage : SkillNode
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
            if (context?.Targets == null || context.Targets.Count == 0)
            {
                Debug.LogWarning("DealAdditionalDamage: No target in context");
                return;
            }

            float damage = GetInputFloat("damageAmount", testDamage);
            bool shouldAffectAll = GetInputBool("affectAllTargets", testAffectAll);

            // Deal damage to all targeted enemies or just the first one
            if (shouldAffectAll)
            {
                int affectedCount = 0;
                foreach (var target in context.Targets)
                {
                    if (target != null)
                    {
                        DealDamage(target, damage);
                        affectedCount++;
                    }
                }
                Debug.Log(
                    $"DealAdditionalDamage: Dealt {damage} damage to {affectedCount} enemies"
                );
            }
            else
            {
                var target = context.Targets[0];
                if (target == null)
                {
                    Debug.LogWarning("DealAdditionalDamage: Target is null");
                    return;
                }
                DealDamage(target, damage);
            }
        }

        private void DealDamage(Assets.Prototypes.Characters.CharacterInstance target, float damage)
        {
            var healthStat = target.GetBoundedStat(
                Assets.Prototypes.Characters.Stats.BoundedStatType.Health
            );
            if (healthStat != null)
            {
                healthStat.SetCurrent(healthStat.Current - damage);
                Debug.Log(
                    $"DealAdditionalDamage: Dealt {damage} damage (new HP: {healthStat.Current})"
                );
            }
            else
            {
                Debug.LogWarning($"DealAdditionalDamage: Could not find health stat on target");
            }
        }
    }
}
