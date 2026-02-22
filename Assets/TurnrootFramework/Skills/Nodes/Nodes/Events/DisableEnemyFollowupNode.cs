using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    /// <summary>
    /// Prevents the target enemy from performing a follow-up attack during combat.
    /// </summary>
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
            if (context?.Participants?.Targets == null || context.Participants.Targets.Count == 0)
            {
#if UNITY_EDITOR
                Debug.LogWarning("DisableEnemyFollowup: No target in context");
#endif
                return;
            }

            bool shouldAffectAll = GetInputBool("affectAllTargets", testAffectAll);

            // Disable followup for all targeted enemies or just the first one
            if (shouldAffectAll)
            {
                foreach (var target in context.Participants.Targets)
                {
                    if (target != null)
                    {
                        context.SetCustomData($"DisableFollowup_{target.Id}", true);
                    }
                }

                $"DisableEnemyFollowup: Disabled followup for {context.Participants.Targets.Count} enemies"
            .LogInfo();
            }
            else
            {
                var target = context.Participants.Targets[0];
                if (target == null)
                {
                    Debug.LogWarning("DisableEnemyFollowup: Target is null");

                    return;
                }
                context.SetCustomData($"DisableFollowup_{target.Id}", true);

                "DisableEnemyFollowup: Disabled followup attack for target".LogInfo();
            }
        }
    }
}

