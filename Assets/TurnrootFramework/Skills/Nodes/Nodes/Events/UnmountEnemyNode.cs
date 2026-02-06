using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    /// <summary>
    /// Forces the target enemy to dismount from their mount or flying state.
    /// </summary>
    [CreateNodeMenu("Events/Offensive/Unmount Enemy")]
    [NodeLabel("Force an enemy to dismount from riding/flying. They can remount on their turn")]
    public class UnmountEnemyNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Input]
        [Tooltip("If true, unmounts all targeted enemies; if false, only first target")]
        public BoolValue affectAllTargets;

        [Tooltip("Test value for affectAllTargets in editor mode")]
        public bool testAffectAll = false;

        public override void Execute(BattleContext context)
        {
            if (context?.Participants?.Targets == null || context.Participants.Targets.Count == 0)
            {
                TurnrootLogger.Log(
                    "UnmountEnemy: No target in context",
                    TurnrootLogger.LogLevel.Warning
                );
                return;
            }

            bool shouldAffectAll = GetInputBool("affectAllTargets", testAffectAll);

            // Unmount all targeted enemies or just the first one
            if (shouldAffectAll)
            {
                foreach (var target in context.Participants.Targets)
                {
                    if (target != null)
                    {
                        context.SetCustomData($"ForceUnmount_{target.Id}", true);
                    }
                }
                TurnrootLogger.Log(
                    $"UnmountEnemy: Unmounted {context.Participants.Targets.Count} enemies"
                );
            }
            else
            {
                var target = context.Participants.Targets[0];
                if (target == null)
                {
                    TurnrootLogger.Log(
                        "UnmountEnemy: Target is null",
                        TurnrootLogger.LogLevel.Warning
                    );

                    return;
                }
                context.SetCustomData($"ForceUnmount_{target.Id}", true);
                TurnrootLogger.Log("UnmountEnemy: Forced target to dismount");
            }
        }
    }
}
