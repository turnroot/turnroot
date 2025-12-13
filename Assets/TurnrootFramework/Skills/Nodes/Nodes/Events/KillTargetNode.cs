using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
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
            if (!ValidateHasTargets(context))
            {
                return;
            }

            bool shouldKillAll = GetInputBool("affectAllTargets", testKillAll);

            int killedCount = ExecuteOnTargets(
                context,
                shouldKillAll,
                target => KillCharacter(target)
            );

            Debug.Log(
                $"KillTarget: Killed {killedCount} {(killedCount == 1 ? "enemy" : "enemies")}"
            );
        }
    }
}
