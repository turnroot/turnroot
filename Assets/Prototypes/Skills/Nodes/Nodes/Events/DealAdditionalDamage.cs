using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Deal Additional Damage")]
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
            "If true, deals damage to all enemies in Targets list; if false, only first target"
        )]
        public BoolValue affectAllTargets;

        [Tooltip("Test value for damage in editor mode")]
        public float testDamage = 10f;

        [Tooltip("Test value for affectAllTargets in editor mode")]
        public bool testAffectAll = false;

        public override void Execute(SkillExecutionContext context)
        {
            if (context?.Targets == null || context.Targets.Count == 0)
            {
                Debug.LogWarning("DealAdditionalDamage: No target in context");
                return;
            }

            // Get the damage value
            float damage = testDamage;
            var damagePort = GetInputPort("damageAmount");
            if (damagePort != null && damagePort.IsConnected)
            {
                var inputValue = damagePort.GetInputValue();
                if (inputValue is FloatValue floatValue)
                {
                    damage = floatValue.value;
                }
            }

            // Get the affectAllTargets value
            bool shouldAffectAll = testAffectAll;
            var affectAllPort = GetInputPort("affectAllTargets");
            if (affectAllPort != null && affectAllPort.IsConnected)
            {
                var inputValue = affectAllPort.GetInputValue();
                if (inputValue is BoolValue boolValue)
                {
                    shouldAffectAll = boolValue.value;
                }
            }

            // Deal damage to all targets or just the first one
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
