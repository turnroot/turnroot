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

        [Output]
        public ExecutionFlow OutFlow;

        [Input]
        [Tooltip("If true, unmounts all targeted enemies; if false, only first target")]
        public BoolValue affectAllTargets;

        public override void Execute(BattleContext context)
        {
            if (context == null)
            {
                "UnmountEnemy: No target in context".LogWarning();
                return;
            }
            var targets = GetCurrentTargets(context);
            if (targets.Count == 0)
            {
                "UnmountEnemy: No target in context".LogWarning();
                return;
            }

            bool shouldAffectAll = GetInputValue("affectAllTargets", affectAllTargets).value;

            // Unmount all targeted enemies or just the first one
            if (shouldAffectAll)
            {
                foreach (var target in targets)
                {
                    if (target != null)
                    {
                        // TODO: Replace SetCustomData stub once CharacterInstance has a mount/dismount
                        // system (e.g. IsMounted flag + MovementType enum swap on dismount).
                        // Suggested API: target.ForceUnmount();
                        // AttackTarget in ContextCommands should consume "ForceUnmount_{target.Id}"
                        // after a hit connects and call the same API, then remove the key.
                        context.SetCustomData($"ForceUnmount_{target.Id}", true);
                    }
                }
                $"UnmountEnemy: Unmounted {targets.Count} enemies".LogInfo();
            }
            else
            {
                var target = targets[0];
                if (target == null)
                {
                    "UnmountEnemy: Target is null".LogWarning();
                    return;
                }
                // TODO: same as above — call target.ForceUnmount() once implemented.
                context.SetCustomData($"ForceUnmount_{target.Id}", true);
                "UnmountEnemy: Forced target to dismount".LogInfo();
            }
        }
    }
}
