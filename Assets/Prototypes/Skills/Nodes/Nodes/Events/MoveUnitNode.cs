using Assets.Prototypes.Gameplay.Combat.FundamentalComponents.Battles;
using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Neutral/Move Unit")]
    [NodeLabel("Moves the unit to a new position on the battlefield")]
    public class MoveUnitNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Input]
        [Tooltip("The number of tiles to move")]
        public FloatValue moveDistance;

        [Tooltip("Test value for move distance in editor mode")]
        public float testDistance = 1f;

        [Tooltip("Direction to move (requires integration with grid system)")]
        public string moveDirection = "Forward";

        public override void Execute(BattleContext context)
        {
            if (context?.UnitInstance == null)
            {
                Debug.LogWarning("MoveUnit: No unit instance in context");
                return;
            }

            float distance = GetInputFloat("moveDistance", testDistance);

            // TODO: Integrate with actual positioning/grid system
            context.SetCustomData("MoveDistance", distance);
            context.SetCustomData("MoveDirection", moveDirection);
            Debug.Log($"MoveUnit: Moving unit {distance} tiles {moveDirection}");
        }
    }
}
