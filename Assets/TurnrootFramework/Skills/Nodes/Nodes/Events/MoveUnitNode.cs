using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Neutral/Move Unit")]
    [NodeLabel("Moves the unit to a new position on the battlefield")]
    public class MoveUnitNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Input]
        [Tooltip("Target position (Vector2Int) to move to")]
        public Vector2Int targetPosition;

        [Tooltip("Test value for target position in editor mode")]
        public Vector2Int testPosition = Vector2Int.zero;

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

            if (!ValidationHelper.ValidateNotNull(context.MapGrid, nameof(context.MapGrid)))
            {
                return;
            }

            if (context.Brain == null)
            {
                throw new System.InvalidOperationException(
                    "MoveUnitNode requires BattleContext.Brain to be set."
                );
            }

            // Get target position from input port or use test value
            var port = GetInputPort("targetPosition");
            Vector2Int newPosition = testPosition;

            if (port != null && port.IsConnected)
            {
                var inputValue = port.GetInputValue();
                if (inputValue is Vector2Int vec2Int)
                {
                    newPosition = vec2Int;
                }
            }

            // Execute move through BattleContext (always uses commands)
            context.MoveUnitToPointInt(context.Unit.UnitInstance, newPosition);
        }
    }
}
