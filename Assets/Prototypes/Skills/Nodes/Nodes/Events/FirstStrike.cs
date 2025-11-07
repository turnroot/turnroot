using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Offensive/First Strike")]
    [NodeLabel("Attack first, prevent counterattack")]
    public class FirstStrike : SkillNode
    {
        [Input]
        public ExecutionFlow input;

        public override void Execute(SkillExecutionContext context)
        {
            if (context == null)
            {
                Debug.LogWarning("FirstStrike: No context provided");
                return;
            }

            // Set flag that unit always attacks first regardless of speed
            // This is different from ChangeBattleOrder.AttackFirst which modifies order within combat
            // FirstStrike means this unit initiates combat before enemy can counterattack
            context.SetCustomData($"FirstStrike_{context.UnitInstance.Id}", true);

            Debug.Log("FirstStrike: Unit will attack first, preventing enemy counterattack");
        }
    }
}
