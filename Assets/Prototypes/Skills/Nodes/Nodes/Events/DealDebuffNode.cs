using Assets.Prototypes.Gameplay.Combat.FundamentalComponents.Battles;
using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Offensive/Deal Debuff")]
    [NodeLabel("Applies a debuff to the target")]
    public class DealDebuffNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Input]
        [Tooltip("If true, applies debuff to all targeted enemies; if false, only first target")]
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

        public override void Execute(BattleContext context)
        {
            if (context?.Targets == null || context.Targets.Count == 0)
            {
                Debug.LogWarning("DealDebuff: No target in context");
                return;
            }

            bool shouldAffectAll = GetInputBool("affectAllTargets", testAffectAll);

            // Apply debuff to all targeted enemies or just the first one
            if (shouldAffectAll)
            {
                foreach (var target in context.Targets)
                {
                    if (target != null)
                    {
                        var debuffData = new
                        {
                            DebuffType = debuffTypePlaceholder,
                            Duration = duration,
                            Intensity = intensity,
                        };
                        context.SetCustomData($"ApplyDebuff_{target.Id}", debuffData);
                    }
                }
                Debug.Log(
                    $"DealDebuff: Applied {debuffTypePlaceholder} debuff to {context.Targets.Count} enemies"
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
                var debuffData = new
                {
                    DebuffType = debuffTypePlaceholder,
                    Duration = duration,
                    Intensity = intensity,
                };
                context.SetCustomData($"ApplyDebuff_{target.Id}", debuffData);
                Debug.Log(
                    $"DealDebuff: Applied {debuffTypePlaceholder} debuff (duration: {duration}, intensity: {intensity})"
                );
            }
        }
    }
}
