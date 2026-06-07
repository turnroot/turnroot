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

            // Fire before the data move so UnitAppearanceBrain.HandleCharacterMoveStarted can
            // read the unit's current (pre-move) position when building the animation path.
            // This replaces the previous call site in BattleInputControllerBrain so all callers
            // (player input, skill nodes, AI) get the walk animation. If no path can be found
            // (e.g. a teleport outside movement range) HandleCharacterMoveStarted falls back to
            // PublishMoveAnimationCompleted immediately, which is the same as the old behaviour.
            // Animation is fire-and-forget; callers do not block on OnMoveAnimationCompleted.
            var destinationPoint = context.MapGrid.GetGridPoint(Target.x, Target.y);
            if (destinationPoint != null)
            {
                context.Brain.PublishCharacterMoveStarted(unit, destinationPoint);
            }

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
