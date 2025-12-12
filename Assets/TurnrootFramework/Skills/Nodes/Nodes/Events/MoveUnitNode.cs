using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Skills.Nodes;
using Turnroot.Utilities;
using UnityEngine;
using XNode;

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
                    context?.UnitInstance,
                    nameof(context.UnitInstance)
                )
            )
            {
                return;
            }

            if (!ValidationHelper.ValidateNotNull(context.mapGrid, nameof(context.mapGrid)))
            {
                return;
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

            // Move the unit
            var result = context.UnitInstance.MoveToPosition(newPosition, context.mapGrid);

            if (result.Success)
            {
                // Invoke the Brain event to notify listeners
                var brain = GetBrain.Get();
                if (brain != null)
                {
                    brain.InvokeUnitMoved(context.UnitInstance, newPosition);
                    Debug.Log(
                        $"MoveUnit: Moved {context.UnitInstance.CharacterTemplate.DisplayName} to {newPosition}"
                    );
                }
                else
                {
                    Debug.LogWarning("MoveUnit: Could not find Brain to invoke event");
                }
            }
            else
            {
                Debug.LogWarning($"MoveUnit: Failed to move unit");
            }
        }
    }
}
