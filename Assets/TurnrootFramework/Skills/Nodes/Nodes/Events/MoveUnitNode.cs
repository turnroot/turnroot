using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    /// <summary>
    /// Moves the unit to a specified position on the battlefield grid.
    /// </summary>
    [CreateNodeMenu("Events/Neutral/Move Unit")]
    [NodeLabel("Moves the unit to a new position on the battlefield")]
    public class MoveUnitNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Input]
        [Tooltip("Target position (Vector2Int) to move to")]
        public Vector2Int targetPosition;

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

            // Get target position from input port (requires connection)
            var port = GetInputPort("targetPosition");
            if (port == null || !port.IsConnected)
            {
                "MoveUnitNode: 'targetPosition' input not connected — skipping move".LogWarning();
                return;
            }

            Vector2Int newPosition = Vector2Int.zero;
            var inputValue = port.GetInputValue();
            if (inputValue is Vector2Int vec2Int)
            {
                newPosition = vec2Int;
            }

            // Execute move through BattleContext (always uses commands)
            context.MoveUnitToPointInt(context.Unit.UnitInstance, newPosition);
        }
    }
}
