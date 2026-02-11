using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    /// <summary>
    /// Grants the unit an additional turn immediately after the current action.
    /// </summary>
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
                    context?.Unit.UnitInstance,
                    nameof(context.Unit.UnitInstance)
                )
            )
            {
                return;
            }

            context.Brain.PublishUnitTakesAnotherTurn(context.Unit.UnitInstance);
            TurnrootLogger.Log(
                $"TakeAnotherTurn: {context.Unit.UnitInstance.CharacterTemplate.DisplayName} will take another turn"
            );
        }
    }
}
