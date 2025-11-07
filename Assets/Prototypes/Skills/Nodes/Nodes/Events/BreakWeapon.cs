using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Offensive/Break Weapon")]
    [NodeLabel("Break enemy's equipped weapon")]
    public class BreakWeapon : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Input]
        [Tooltip("If true, breaks all targeted enemies' weapons; if false, only first target")]
        public BoolValue affectAllTargets;

        [Tooltip("Test value for affectAllTargets in editor mode")]
        public bool testAffectAll = false;

        public override void Execute(SkillExecutionContext context)
        {
            if (context?.Targets == null || context.Targets.Count == 0)
            {
                Debug.LogWarning("BreakWeapon: No target in context");
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

            if (shouldAffectAll)
            {
                foreach (var target in context.Targets)
                {
                    // Store break weapon command in CustomData
                    context.SetCustomData($"BreakWeapon_{target.Id}", true);
                }
                Debug.Log($"BreakWeapon: Would break weapon for {context.Targets.Count} targets");
            }
            else
            {
                var target = context.Targets[0];
                // Store break weapon command in CustomData
                context.SetCustomData($"BreakWeapon_{target.Id}", true);
                Debug.Log("BreakWeapon: Would break weapon for first target");
            }
        }
    }
}
