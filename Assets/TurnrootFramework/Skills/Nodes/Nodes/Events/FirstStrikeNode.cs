using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Offensive/First Strike")]
    [NodeLabel("Attack first, prevent counterattack")]
    public class FirstStrikeNode : SkillNode
    {
        [Input]
        public ExecutionFlow input;

        public override void Execute(BattleContext context)
        {
            if (!ValidateContext(context))
            {
                return;
            }

            // Set flag that unit always attacks first regardless of speed
            // This is different from ChangeBattleOrder.AttackFirst which modifies order within combat
            // FirstStrike means this unit initiates combat before enemy can counterattack
            context.SetCustomData($"FirstStrike_{context.Unit.UnitInstance.Id}", true);

            TurnrootLogger.Log(
                "FirstStrike: Unit will attack first, preventing enemy counterattack"
            );
        }
    }
}
