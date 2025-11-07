using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Neutral/Reposition")]
    [NodeLabel("Move ally to adjacent tile")]
    public class Reposition : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        public override void Execute(SkillExecutionContext context)
        {
            if (context == null)
            {
                Debug.LogWarning("Reposition: No context provided");
                return;
            }

            // Get the direction from custom data (set by player during gameplay)
            Direction allyDirection = context.GetCustomData<Direction>(
                "SelectedDirection",
                Direction.Center
            );

            // Get the move direction from custom data (set by player during gameplay)
            RepositionDirection moveDirection = context.GetCustomData<RepositionDirection>(
                "SelectedMoveDirection",
                RepositionDirection.Behind
            );

            if (context.AdjacentUnits == null || !context.AdjacentUnits.ContainsKey(allyDirection))
            {
                Debug.LogWarning($"Reposition: No unit at {allyDirection}");
                return;
            }

            var ally = context.AdjacentUnits[allyDirection];
            if (ally == null)
            {
                Debug.LogWarning($"Reposition: Ally at {allyDirection} is null");
                return;
            }

            // Store reposition command in CustomData
            var repositionData = new
            {
                AllyId = ally.Id,
                MoveDirection = moveDirection,
                CasterId = context.UnitInstance.Id,
            };

            context.SetCustomData("Reposition", repositionData);

            Debug.Log(
                $"Reposition: Will move ally from {allyDirection} to {moveDirection} relative to caster"
            );
        }
    }

    public enum RepositionDirection
    {
        Behind, // Move ally to tile behind caster
        InFront, // Move ally to tile in front of caster
        Left, // Move ally to left of caster
        Right, // Move ally to right of caster
        Swap, // Swap positions with ally
    }
}
