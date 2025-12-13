using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Skills.Nodes;
using Turnroot.Utilities;
using UnityEngine;

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
            if (
                !ValidationHelper.ValidateNotNull(
                    context?.UnitInstance,
                    nameof(context.UnitInstance)
                )
            )
            {
                return;
            }

            var brain = GetBrain.Get();
            if (brain != null)
            {
                brain.PublishUnitTakesAnotherTurn(context.UnitInstance);
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
