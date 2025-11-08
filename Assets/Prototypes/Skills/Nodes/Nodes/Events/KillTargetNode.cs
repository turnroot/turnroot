using Assets.Prototypes.Gameplay.Combat.FundamentalComponents.Battles;
using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Offensive/Kill Target")]
    [NodeLabel("Instantly kills the target enemy")]
    public class KillTargetNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Input]
        [Tooltip(
            "If true, kills all targeted enemies in Targets list; if false, only kills first target"
        )]
        public BoolValue affectAllTargets;

        [Tooltip("Test value for killAllTargets in editor mode")]
        public bool testKillAll = false;

        public override void Execute(BattleContext context)
        {
            if (context?.Targets == null || context.Targets.Count == 0)
            {
                Debug.LogWarning("KillTarget: No target in context");
                return;
            }

            bool shouldKillAll = GetInputBool("killAllTargets", testKillAll);

            // Kill all targeted enemies or just the first one
            if (shouldKillAll)
            {
                int killedCount = 0;
                foreach (var target in context.Targets)
                {
                    if (target != null)
                    {
                        KillCharacter(target);
                        killedCount++;
                    }
                }
                Debug.Log($"KillTarget: Killed {killedCount} enemies");
            }
            else
            {
                var target = context.Targets[0];
                if (target == null)
                {
                    Debug.LogWarning("KillTarget: Target is null");
                    return;
                }
                KillCharacter(target);
            }
        }

        private void KillCharacter(Assets.Prototypes.Characters.CharacterInstance target)
        {
            var healthStat = target.GetBoundedStat(
                Assets.Prototypes.Characters.Stats.BoundedStatType.Health
            );
            if (healthStat != null)
            {
                healthStat.SetCurrent(0);
                Debug.Log($"KillTarget: Set target health to 0");
            }
            else
            {
                Debug.LogWarning($"KillTarget: Could not find health stat on target");
            }
        }
    }
}
