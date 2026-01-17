using System;
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
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

            var oldPoint = unit.UnitPositionToMapGridPoint(unit.MapGridPosition, context.mapGrid);

            UndoState["from"] = unit.MapGridPosition;

            // Move the unit (updates internal position)
            var result = unit.MoveToPosition(Target, context.mapGrid);

            if (result.Success)
            {
                // Update grid occupancy
                var newPoint = unit.UnitPositionToMapGridPoint(Target, context.mapGrid);
                context.mapGrid.RemoveOccupied(oldPoint);
                context.mapGrid.SetOccupied(newPoint, unit);
                unit.MapGridPosition = Target;

                // Publish event
                context.Brain?.Publish(
                    new Events.UnitMovedEvent(unit, (Vector2Int)UndoState["from"], Target)
                );
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
            var moved = bb != null && bb.MoveUnit(unit, (Vector2Int)from, context.mapGrid);
            return moved;
        }
    }
}
