using Assets.Prototypes.Gameplay.Combat.FundamentalComponents.Battles;
using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Offensive/Disable Enemy Followup")]
    [NodeLabel("Prevents the enemy from performing a follow-up attack")]
    public class DisableEnemyFollowupNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Input]
        [Tooltip(
            "If true, disables followup for all targeted enemies; if false, only first target"
        )]
        public BoolValue affectAllTargets;

        [Tooltip("Test value for affectAllTargets in editor mode")]
        public bool testAffectAll = false;

        public override void Execute(BattleContext context)
        {
            if (context?.Targets == null || context.Targets.Count == 0)
            {
                Debug.LogWarning("DisableEnemyFollowup: No target in context");
                return;
            }

            bool shouldAffectAll = GetInputBool("affectAllTargets", testAffectAll);

            // Disable followup for all targeted enemies or just the first one
            if (shouldAffectAll)
            {
                foreach (var target in context.Targets)
                {
                    if (target != null)
                    {
                        context.SetCustomData($"DisableFollowup_{target.Id}", true);
                    }
                }
                Debug.Log(
                    $"DisableEnemyFollowup: Disabled followup for {context.Targets.Count} enemies"
                );
            }
            else
            {
                var target = context.Targets[0];
                if (target == null)
                {
                    Debug.LogWarning("DisableEnemyFollowup: Target is null");
                    return;
                }
                context.SetCustomData($"DisableFollowup_{target.Id}", true);
                Debug.Log("DisableEnemyFollowup: Disabled followup attack for target");
            }
        }
    }
}
