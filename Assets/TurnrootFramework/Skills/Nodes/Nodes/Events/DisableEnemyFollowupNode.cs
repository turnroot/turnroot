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

            bool shouldAffectAll = GetInputBool("affectAllTargets", false);

            int count = ExecuteOnTargets(
                context,
                shouldAffectAll,
                target => context.SetCustomData($"DisableFollowup_{target.Id}", true),
                "DisableEnemyFollowup"
            );

            if (count > 0)
            {
                $"DisableEnemyFollowup: Disabled followup for {count} {(count == 1 ? "enemy" : "enemies")}".LogInfo();
            }
        }
    }
}
