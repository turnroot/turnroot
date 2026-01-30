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

            UndoState["from"] = unit.MapGridPosition;

            // Move the unit (updates internal position)
            var result = unit.MoveToPosition(Target, context.MapGrid);

            if (result.Success)
            {
                // Update grid occupancy
                var newPoint = unit.UnitPositionToMapGridPoint(Target, context.MapGrid);
                context.MapGrid.RemoveOccupied(oldPoint);
                context.MapGrid.SetOccupied(newPoint, unit);
                unit.MapGridPosition = Target;

                // Publish event on the priority bus
                context.Brain?.Publish(
                    new Events.UnitMovedEvent(unit, (Vector2Int)UndoState["from"], Target)
                );

                // Also publish typed move events so other systems (UI/flow) can react immediately
                context.Brain?.PublishCharacterMoveCompleted(unit, newPoint);
                context.Brain?.PublishUnitMoved(unit, Target);
                context.Brain?.PublishMoveCompleted(unit, newPoint);
            }

            return result.Success;
        }

        public override bool Undo(BattleContext context)
        {
            var unit = FindUnit(context, UnitId);
            if (unit == null || !UndoState.TryGetValue("from", out var from))
            {
                return false;
            }

            var bb = context.Brain?.battleBrain;
            var moved = bb != null && bb.MoveUnit(unit, (Vector2Int)from, context.MapGrid);
            return moved;
        }
    }
}
