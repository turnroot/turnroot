using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using UnityEngine;

namespace Turnroot.Gameplay.Brain.Commands
{
    /// <summary>
    /// Command to move a unit to a new position.
    /// </summary>
    public class MoveCommand : CommandBase
    {
        public string UnitId { get; }
        public Vector2Int Target { get; }

        public MoveCommand(string unitId, Vector2Int target, int turn)
            : base(turn)
        {
            UnitId = unitId;
            Target = target;
        }

        public override bool Execute(BattleContext context)
        {
            var unit = FindUnit(context, UnitId);
            if (unit == null)
            {
                return false;
            }

            var oldPoint = unit.UnitPositionToMapGridPoint(unit.MapGridPosition, context.MapGrid);
            UndoState[UndoStateKeys.From] = unit.MapGridPosition;

            // TODO: Fire PublishCharacterMoveStarted(unit, newPoint) HERE (before the data move)
            // so that UnitAppearanceBrain.HandleCharacterMoveStarted can start the spline animation
            // coroutine for skill-triggered and AI-triggered moves (WarpNode, RepositionNode,
            // MoveUnitNode, AI ExecuteGoal). Currently only the player input controller fires
            // OnCharacterMoveStarted, so those moves teleport the model instead of animating.
            // After firing, the command should await OnMoveAnimationCompleted before returning true
            // (or use a callback pattern) so callers block until the visual is done.
            var result = unit.MoveToPosition(Target, context.MapGrid);
            if (!result.Success)
            {
                return false;
            }

            var newPoint = unit.UnitPositionToMapGridPoint(Target, context.MapGrid);

            if (oldPoint != null)
            {
                context.MapGrid.RemoveOccupied(oldPoint);
            }

            context.MapGrid.SetOccupied(newPoint, unit);

            context.InvalidateUnitTileCache(unit);
            context.UpdateAdjacentUnits();
            context.UpdateTargetsInRange();

            context.Brain.Publish(
                new Events.UnitMovedEvent(unit, (Vector2Int)UndoState[UndoStateKeys.From], Target)
            );
            context.Brain.PublishCharacterMoveCompleted(unit, newPoint);
            context.Brain.PublishUnitMoved(unit, Target);
            context.Brain.PublishMoveCompleted(unit, newPoint);

            return true;
        }

        public override bool Undo(BattleContext context)
        {
            var unit = FindUnit(context, UnitId);
            if (unit == null || !UndoState.TryGetValue(UndoStateKeys.From, out var from))
            {
                return false;
            }

            var bb = context.Brain.battleBrain;
            var moved = bb != null && bb.MoveUnit(unit, (Vector2Int)from, context.MapGrid);
            return moved;
        }
    }
}
