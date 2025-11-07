using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Unmount Enemy")]
    [NodeLabel("Force an enemy to dismount from riding/flying. They can remount on their turn")]
    public class UnmountEnemy : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Input]
        [Tooltip("If true, unmounts all enemies; if false, only first target")]
        public BoolValue affectAllTargets;

        [Tooltip("Test value for affectAllTargets in editor mode")]
        public bool testAffectAll = false;

        public override void Execute(SkillExecutionContext context)
        {
            if (context?.Targets == null || context.Targets.Count == 0)
            {
                Debug.LogWarning("UnmountEnemy: No target in context");
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

            // Unmount all targets or just the first one
            if (shouldAffectAll)
            {
                int affectedCount = 0;
                foreach (var target in context.Targets)
                {
                    if (target != null)
                    {
                        UnmountCharacter(target);
                        affectedCount++;
                    }
                }
                Debug.Log($"UnmountEnemy: Unmounted {affectedCount} enemies");
            }
            else
            {
                var target = context.Targets[0];
                if (target == null)
                {
                    Debug.LogWarning("UnmountEnemy: Target is null");
                    return;
                }
                UnmountCharacter(target);
            }
        }

        private void UnmountCharacter(Assets.Prototypes.Characters.CharacterInstance target)
        {
            // TODO: Integrate with actual mount/movement system
            Debug.Log($"UnmountEnemy: Forced target to dismount");
        }
    }
}
