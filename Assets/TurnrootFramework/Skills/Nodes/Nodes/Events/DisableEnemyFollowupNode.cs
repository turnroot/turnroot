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

        public override void Execute(BattleContext context)
        {
            if (context?.Participants?.Targets == null || context.Participants.Targets.Count == 0)
            {
#if UNITY_EDITOR
                "DisableEnemyFollowup: No target in context".LogWarning();
#endif
                return;
            }

            bool shouldAffectAll = false;
            var allPort = GetInputPort("affectAllTargets");
            if (allPort != null && allPort.IsConnected)
            {
                shouldAffectAll = GetInputBool("affectAllTargets", false);
            }

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

                $"DisableEnemyFollowup: Disabled followup for {context.Participants.Targets.Count} enemies".LogInfo();
            }
            else
            {
                var target = context.Participants.Targets[0];
                if (target == null)
                {
                    "DisableEnemyFollowup: Target is null".LogWarning();

                    return;
                }
                context.SetCustomData($"DisableFollowup_{target.Id}", true);

                "DisableEnemyFollowup: Disabled followup attack for target".LogInfo();
            }
        }
    }
}
