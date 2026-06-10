using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain.Commands
{
    /// <summary>
    /// Command to swap the battlefield positions of two units.
    /// Publishes OnSwapStarted before the swap and OnSwapLogicCompleted after,
    /// so animation systems can hook in at each phase.
    /// </summary>
    public class SwapCommand : CommandBase
    {
        public string UnitId { get; }
        public string TargetId { get; }

        public SwapCommand(string unitId, string targetId, int turn)
            : base(turn)
        {
            UnitId = unitId;
            TargetId = targetId;
        }

        public override bool Execute(BattleContext context)
        {
            var unit = FindUnit(context, UnitId);
            var target = FindUnit(context, TargetId);

            if (
                !ValidationHelper.ValidateNotNull(unit, nameof(unit))
                || !ValidationHelper.ValidateNotNull(target, nameof(target))
            )
            {
                return false;
            }

            var unitPos = unit.MapGridPosition;
            var targetPos = target.MapGridPosition;

            UndoState[UndoStateKeys.From] = unitPos;
            UndoState["TargetFrom"] = targetPos;

            context.Brain.PublishSwapStarted(unit, target);

            // Move unit to target's old position
            var unitOldPoint = unit.UnitPositionToMapGridPoint(unitPos, context.MapGrid);
            var targetOldPoint = target.UnitPositionToMapGridPoint(targetPos, context.MapGrid);

            var unitResult = unit.MoveToPosition(targetPos, context.MapGrid);
            if (!unitResult.Success)
            {
                return false;
            }

            var targetResult = target.MoveToPosition(unitPos, context.MapGrid);
            if (!targetResult.Success)
            {
                // Roll unit back
                unit.MoveToPosition(unitPos, context.MapGrid);
                return false;
            }

            // Update map grid occupancy
            if (unitOldPoint != null)
            {
                context.MapGrid.RemoveOccupied(unitOldPoint);
            }

            if (targetOldPoint != null)
            {
                context.MapGrid.RemoveOccupied(targetOldPoint);
            }

            var unitNewPoint = unit.UnitPositionToMapGridPoint(targetPos, context.MapGrid);
            var targetNewPoint = target.UnitPositionToMapGridPoint(unitPos, context.MapGrid);

            if (unitNewPoint != null)
            {
                context.MapGrid.SetOccupied(unitNewPoint, unit);
            }

            if (targetNewPoint != null)
            {
                context.MapGrid.SetOccupied(targetNewPoint, target);
            }

            context.InvalidateUnitTileCache(unit);
            context.InvalidateUnitTileCache(target);
            context.UpdateAdjacentUnits();
            context.UpdateTargetsInRange();

            context.Brain.PublishSwapLogicCompleted(unit, target);

            return true;
        }

        public override bool Undo(BattleContext context)
        {
            var unit = FindUnit(context, UnitId);
            var target = FindUnit(context, TargetId);

            if (
                unit == null
                || target == null
                || !UndoState.TryGetValue(UndoStateKeys.From, out var unitFromObj)
                || !UndoState.TryGetValue("TargetFrom", out var targetFromObj)
            )
            {
                return false;
            }

            var unitFrom = (Vector2Int)unitFromObj;
            var targetFrom = (Vector2Int)targetFromObj;

            // Current positions (post-swap) for grid cleanup
            var unitCurrentPoint = unit.UnitPositionToMapGridPoint(
                unit.MapGridPosition,
                context.MapGrid
            );
            var targetCurrentPoint = target.UnitPositionToMapGridPoint(
                target.MapGridPosition,
                context.MapGrid
            );

            unit.MoveToPosition(unitFrom, context.MapGrid);
            target.MoveToPosition(targetFrom, context.MapGrid);

            if (unitCurrentPoint != null)
            {
                context.MapGrid.RemoveOccupied(unitCurrentPoint);
            }

            if (targetCurrentPoint != null)
            {
                context.MapGrid.RemoveOccupied(targetCurrentPoint);
            }

            var unitRestoredPoint = unit.UnitPositionToMapGridPoint(unitFrom, context.MapGrid);
            var targetRestoredPoint = target.UnitPositionToMapGridPoint(
                targetFrom,
                context.MapGrid
            );

            if (unitRestoredPoint != null)
            {
                context.MapGrid.SetOccupied(unitRestoredPoint, unit);
            }

            if (targetRestoredPoint != null)
            {
                context.MapGrid.SetOccupied(targetRestoredPoint, target);
            }

            context.InvalidateUnitTileCache(unit);
            context.InvalidateUnitTileCache(target);
            context.UpdateAdjacentUnits();
            context.UpdateTargetsInRange();

            return true;
        }
    }
}
