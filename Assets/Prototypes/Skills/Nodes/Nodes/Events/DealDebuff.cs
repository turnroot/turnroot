using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Deal Debuff")]
    [NodeLabel("Applies a debuff to the target")]
    public class DealDebuff : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Input]
        [Tooltip("If true, applies debuff to all enemies; if false, only first target")]
        public BoolValue affectAllTargets;

        [Tooltip("Test value for affectAllTargets in editor mode")]
        public bool testAffectAll = false;

        [Tooltip(
            "Placeholder: The type of debuff to apply (will be replaced with DebuffType object)"
        )]
        public string debuffTypePlaceholder = "Poisoned";

        [Tooltip("Duration of the debuff in turns")]
        public int duration = 3;

        [Tooltip("Intensity/strength of the debuff")]
        public float intensity = 1f;

        public override void Execute(SkillExecutionContext context)
        {
            if (context?.Targets == null || context.Targets.Count == 0)
            {
                Debug.LogWarning("DealDebuff: No target in context");
                return;
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

            // Apply debuff to all targets or just the first one
            if (shouldAffectAll)
            {
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
                    $"DealDebuff: Applied {debuffTypePlaceholder} debuff to {affectedCount} enemies"
                );
            }
            else
            {
                var target = context.Targets[0];
                if (target == null)
                {
                    Debug.LogWarning("DealDebuff: Target is null");
                    return;
                }
                ApplyDebuff(target);
            }
        }

        private void ApplyDebuff(Assets.Prototypes.Characters.CharacterInstance target)
        {
            // TODO: Integrate with actual status effect/debuff system
            // TODO: Replace debuffTypePlaceholder with actual DebuffType object
            Debug.Log(
                $"DealDebuff: Applied {debuffTypePlaceholder} debuff (duration: {duration}, intensity: {intensity})"
            );
        }
    }
}
