using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Locations;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    /// <summary>
    /// Moves an adjacent ally to a different tile relative to the caster's position.
    /// </summary>
    [CreateNodeMenu("Events/Neutral/Reposition")]
    [NodeLabel("Move ally to adjacent tile")]
    public class RepositionNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        public override void Execute(BattleContext context)
        {
            if (!ValidateContext(context))
            {
                return;
            }

            // Get the direction from custom data (set by player during gameplay)
            Direction allyDirection = context.GetCustomData("SelectedDirection", Direction.Center);

            // Get the move direction from custom data (set by player during gameplay)
            RepositionDirection moveDirection = context.GetCustomData(
                "SelectedMoveDirection",
                RepositionDirection.Behind
            );

            // Get the unit in the specified direction
            if (context.Participants.AdjacentUnits == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("Reposition: No adjacent units data");
#endif
                return;
            }

            var ally = context.Participants.AdjacentUnits.GetUnit(allyDirection);
            if (ally == null)
            {
                TurnrootLogger.Log(
                    $"Reposition: No unit at {allyDirection}",
                    TurnrootLogger.LogLevel.Warning
                );
                return;
            }

            // Store reposition command in CustomData
            var repositionData = new
            {
                AllyId = ally.Id,
                MoveDirection = moveDirection,
                CasterId = context.Unit.UnitInstance.Id,
            };

            context.SetCustomData("Reposition", repositionData);

            TurnrootLogger.Log(
                $"Reposition: Will move ally from {allyDirection} to {moveDirection} relative to caster"
            );
        }
    }

    /// <summary>
    /// Defines the relative position where an ally will be moved during repositioning.
    /// </summary>
    public enum RepositionDirection
    {
        Behind, // Move ally to tile behind caster
        InFront, // Move ally to tile in front of caster
        Left, // Move ally to left of caster
        Right, // Move ally to right of caster
        Swap, // Swap positions with ally
    }
}
