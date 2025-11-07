using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Neutral/Take Another Turn")]
    [NodeLabel("Allows the unit to take an additional turn immediately")]
    public class TakeAnotherTurn : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        public override void Execute(SkillExecutionContext context)
        {
            if (context?.UnitInstance == null)
            {
                Debug.LogWarning("TakeAnotherTurn: No unit instance in context");
                return;
            }

            // TODO: Integrate with actual turn order system
            context.SetCustomData("TakeAnotherTurn", true);
            Debug.Log($"TakeAnotherTurn: Unit will take another turn");
        }
    }
}
