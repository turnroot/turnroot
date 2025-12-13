using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Skills.Nodes;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
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
            bool shouldAffectAll = GetInputBool("affectAllTargets", testAffectAll);

            int affected = ExecuteOnTargets(
                context,
                shouldAffectAll,
                target =>
                {
                    var debuffData = new
                    {
                        DebuffType = debuffTypePlaceholder,
                        Duration = duration,
                        Intensity = intensity,
                    };
                    context.SetCustomData($"ApplyDebuff_{target.Id}", debuffData);
                },
                "DealDebuff"
            );

            if (affected > 0)
            {
                Debug.Log(
                    $"DealDebuff: Applied {debuffTypePlaceholder} debuff to {affected} target(s)"
                );
            }
        }
    }
}
