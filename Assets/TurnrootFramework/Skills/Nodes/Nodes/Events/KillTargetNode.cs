using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    /// <summary>
    /// Instantly kills the target enemy, bypassing normal combat damage calculations.
    /// </summary>
    [CreateNodeMenu("Events/Offensive/Kill Target")]
    [NodeLabel("Instantly kills the target enemy")]
    public class KillTargetNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Output]
        public ExecutionFlow OutFlow;

        [Input]
        [Tooltip(
            "If true, kills all targeted enemies in Targets list; if false, only kills first target"
        )]
        public BoolValue affectAllTargets;

        public override void Execute(BattleContext context)
        {
            if (!ValidateHasTargets(context))
            {
                return;
            }

            bool shouldKillAll = GetInputValue("affectAllTargets", affectAllTargets).value;
            int killedCount = ExecuteOnTargets(
                context,
                shouldKillAll,
                target => KillCharacter(context, target)
            );

            $"KillTarget: Killed {killedCount} {(killedCount == 1 ? "enemy" : "enemies")}".LogInfo();
        }
    }
}
