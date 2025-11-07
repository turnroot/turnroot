using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Area of Effect Damage")]
    [NodeLabel("Deals damage to all enemies in an area")]
    public class AreaOfEffectDamage : SkillNode
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

        public override void Execute(SkillExecutionContext context)
        {
            if (context?.Targets == null || context.Targets.Count == 0)
            {
                Debug.LogWarning("AreaOfEffectDamage: No targets in context");
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

            // Get the radius value
            float radius = testRadius;
            var radiusPort = GetInputPort("aoeRadius");
            if (radiusPort != null && radiusPort.IsConnected)
            {
                var inputValue = radiusPort.GetInputValue();
                if (inputValue is FloatValue floatValue)
                {
                    radius = floatValue.value;
                }
            }

            // Deal damage to all targets in the AoE
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
                $"AreaOfEffectDamage: Dealt {damage} damage to {affectedCount} enemies in {radius} tile radius"
            );
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
                    $"AreaOfEffectDamage: Dealt {damage} damage (new HP: {healthStat.Current})"
                );
            }
            else
            {
                Debug.LogWarning($"AreaOfEffectDamage: Could not find health stat on target");
            }
        }
    }
}
