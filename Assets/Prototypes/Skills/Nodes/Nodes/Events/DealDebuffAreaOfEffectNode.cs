using Assets.Prototypes.Gameplay.Combat.FundamentalComponents.Battles;
using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Offensive/Deal Debuff Area Of Effect")]
    [NodeLabel("Applies a debuff to all targeted enemies in an area")]
    public class DealDebuffAreaOfEffectNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Input]
        [Tooltip("The radius of the area of effect")]
        public FloatValue aoeRadius;

        [Tooltip("Test value for AoE radius in editor mode")]
        public float testRadius = 2f;

        [Tooltip(
            "Placeholder: The type of debuff to apply (will be replaced with DebuffType object)"
        )]
        public string debuffTypePlaceholder = "Slowed";

        [Tooltip("Duration of the debuff in turns")]
        public int duration = 2;

        [Tooltip("Intensity/strength of the debuff")]
        public float intensity = 1f;

        public override void Execute(BattleContext context)
        {
            if (context?.Targets == null || context.Targets.Count == 0)
            {
                Debug.LogWarning("DealDebuffAreaOfEffect: No targets in context");
                return;
            }

            float radius = GetInputFloat("aoeRadius", testRadius);

            // Apply debuff to all targeted enemies in the AoE
            foreach (var target in context.Targets)
            {
                if (target != null)
                {
                    var debuffData = new
                    {
                        DebuffType = debuffTypePlaceholder,
                        Duration = duration,
                        Intensity = intensity,
                        Radius = radius,
                    };
                    context.SetCustomData($"ApplyDebuff_{target.Id}", debuffData);
                }
            }

            Debug.Log(
                $"DealDebuffAreaOfEffect: Applied {debuffTypePlaceholder} debuff to {context.Targets.Count} enemies in {radius} tile radius"
            );
        }
    }
}
