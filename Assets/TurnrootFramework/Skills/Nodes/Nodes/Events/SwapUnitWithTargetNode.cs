using Turnroot.Gameplay.Brain.Commands;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    /// <summary>
    /// Swaps the battlefield positions of the caster and the target unit
    /// via the command system, so it is undoable and triggers animation events.
    /// </summary>
    [CreateNodeMenu("Events/Neutral/Swap Unit With Target")]
    [NodeLabel("Swaps the position of the unit with the target")]
    public class SwapUnitWithTargetNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Output]
        public ExecutionFlow OutFlow;

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

            if (
                !ValidationHelper.ValidateNotNullOrEmpty(
                    context.Participants.Targets,
                    nameof(context.Participants.Targets)
                )
            )
            {
                return;
            }

            var target = context.Participants.Targets[0];
            if (!ValidationHelper.ValidateNotNull(target, nameof(target)))
            {
                return;
            }

            var unit = context.Unit.UnitInstance;
            var turn = context.Brain?.battleBrain?.CurrentTurnNumber ?? 0;
            var command = new SwapCommand(unit.Id, target.Id, turn);
            context.Brain.ExecuteCommand(command);

            $"SwapUnitWithTarget: Swapping {unit.Id} <-> {target.Id}".LogInfo();
        }
    }
}
