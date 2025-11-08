using Assets.Prototypes.Gameplay.Combat.FundamentalComponents.Battles;
using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
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
            if (context?.Targets == null || context.Targets.Count == 0)
            {
                Debug.LogWarning("AreaOfEffectDamage: No targets in context");
                return;
            }

            float damage = GetInputFloat("damageAmount", testDamage);
            float radius = GetInputFloat("aoeRadius", testRadius);

            // Deal damage to all targeted enemies in the AoE
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
