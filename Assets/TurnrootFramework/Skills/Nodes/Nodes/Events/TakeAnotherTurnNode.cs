using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Skills.Nodes;
using Turnroot.Utilities;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Neutral/Take Another Turn")]
    [NodeLabel("Allows the unit to take an additional turn immediately")]
    public class TakeAnotherTurnNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        public override void Execute(BattleContext context)
        {
            if (context?.UnitInstance == null)
            {
                Debug.LogWarning("TakeAnotherTurn: No unit instance in context");
                return;
            }

            var brain = GetBrain.Get();
            if (brain != null)
            {
                brain.InvokeUnitTakesAnotherTurn(context.UnitInstance);
                Debug.Log(
                    $"TakeAnotherTurn: {context.UnitInstance.CharacterTemplate.DisplayName} will take another turn"
                );
            }
            else
            {
                Debug.LogWarning("TakeAnotherTurn: Could not find Brain to invoke event");
            }
        }
    }
}
