using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Deal Debuff Area of Effect")]
    [NodeLabel("Applies a debuff to all enemies in an area")]
    public class DealDebuffAreaOfEffect : SkillNode
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

        public override void Execute(SkillExecutionContext context)
        {
            if (context?.Targets == null || context.Targets.Count == 0)
            {
                Debug.LogWarning("DealDebuffAreaOfEffect: No targets in context");
                return;
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

            // Apply debuff to all targets in the AoE
            int affectedCount = 0;
            foreach (var target in context.Targets)
            {
                if (target != null)
                {
                    ApplyDebuff(target);
                    affectedCount++;
                }
            }

            Debug.Log(
                $"DealDebuffAreaOfEffect: Applied {debuffTypePlaceholder} debuff to {affectedCount} enemies in {radius} tile radius"
            );
        }

        private void ApplyDebuff(Assets.Prototypes.Characters.CharacterInstance target)
        {
            // TODO: Integrate with actual status effect/debuff system
            // TODO: Replace debuffTypePlaceholder with actual DebuffType object
            Debug.Log(
                $"DealDebuffAreaOfEffect: Applied {debuffTypePlaceholder} debuff (duration: {duration}, intensity: {intensity})"
            );
        }
    }
}
