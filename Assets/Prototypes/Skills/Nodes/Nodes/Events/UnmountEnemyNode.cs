using Assets.Prototypes.Gameplay.Combat.FundamentalComponents.Battles;
using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
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
            if (context?.Targets == null || context.Targets.Count == 0)
            {
                Debug.LogWarning("UnmountEnemy: No target in context");
                return;
            }

            bool shouldAffectAll = GetInputBool("affectAllTargets", testAffectAll);

            // Unmount all targeted enemies or just the first one
            if (shouldAffectAll)
            {
                foreach (var target in context.Targets)
                {
                    if (target != null)
                    {
                        context.SetCustomData($"ForceUnmount_{target.Id}", true);
                    }
                }
                Debug.Log($"UnmountEnemy: Unmounted {context.Targets.Count} enemies");
            }
            else
            {
                var target = context.Targets[0];
                if (target == null)
                {
                    Debug.LogWarning("UnmountEnemy: Target is null");
                    return;
                }
                context.SetCustomData($"ForceUnmount_{target.Id}", true);
                Debug.Log("UnmountEnemy: Forced target to dismount");
            }
        }
    }
}
