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

            var oldPoint = unit.UnitPositionToMapGridPoint(unit.MapGridPosition, context.MapGrid);

            UndoState["from"] = unit.MapGridPosition;

            // Move the unit (updates internal position)
            var result = unit.MoveToPosition(Target, context.MapGrid);

            if (result.Success)
            {
                // Update grid occupancy
                var newPoint = unit.UnitPositionToMapGridPoint(Target, context.MapGrid);

                // Diagnostic: snapshot occupant at old and new points before we mutate the grid.
                try
                {
                    var oldOccupier =
                        oldPoint == null
                            ? null
                            : context
                                .MapGrid.GetGridPoint(oldPoint.Row, oldPoint.Col)
                                ?.CurrentInstance;
                    var newOccupierBefore =
                        newPoint == null
                            ? null
                            : context
                                .MapGrid.GetGridPoint(newPoint.Row, newPoint.Col)
                                ?.CurrentInstance;
                    TurnrootLogger.Log(
                        $"MoveCommand: Before move - unit={unit.Id} old={unit.MapGridPosition} oldOccupier={oldOccupier?.Id ?? "<none>"} new={Target} newOccupier={newOccupierBefore?.Id ?? "<none>"}"
                    );
                }
                catch (System.Exception ex)
                {
                    TurnrootLogger.Log(
                        "MoveCommand: Diagnostic snapshot failed: " + ex.Message,
                        TurnrootLogger.LogLevel.Warning
                    );
                }

                context.MapGrid.RemoveOccupied(oldPoint);

                try
                {
                    var afterOld =
                        oldPoint == null
                            ? null
                            : context
                                .MapGrid.GetGridPoint(oldPoint.Row, oldPoint.Col)
                                ?.CurrentInstance;
                    TurnrootLogger.Log(
                        $"MoveCommand: After RemoveOccupied - old point occupant now={afterOld?.Id ?? "<none>"}"
                    );
                }
                catch (System.Exception ex)
                {
                    TurnrootLogger.Log(
                        "MoveCommand: Diagnostic after-RemoveOccupied failed: " + ex.Message,
                        TurnrootLogger.LogLevel.Warning
                    );
                }

                context.MapGrid.SetOccupied(newPoint, unit);

                try
                {
                    var afterNew =
                        newPoint == null
                            ? null
                            : context
                                .MapGrid.GetGridPoint(newPoint.Row, newPoint.Col)
                                ?.CurrentInstance;
                    TurnrootLogger.Log(
                        $"MoveCommand: After SetOccupied - new point occupant now={afterNew?.Id ?? "<none>"}"
                    );
                }
                catch (System.Exception ex)
                {
                    TurnrootLogger.Log(
                        "MoveCommand: Diagnostic after-SetOccupied failed: " + ex.Message,
                        TurnrootLogger.LogLevel.Warning
                    );
                }

                // NOTE: Do NOT write MapGridPosition directly here. `MapGrid.SetOccupied` is authoritative and will align the instance position.

                // Diagnostic: print occupancy snapshot for quick debugging
                try
                {
                    TurnrootLogger.Log("MoveCommand: Occupancy snapshot post-move:");
                    context.MapGrid.GetAllOccupiedPoints();
                }
                catch (System.Exception ex)
                {
                    TurnrootLogger.Log(
                        "MoveCommand: Occupancy snapshot failed: " + ex.Message,
                        TurnrootLogger.LogLevel.Warning
                    );
                }

                // Update battle participants after movement
                context.InvalidateUnitPositionCache();
                context.InvalidateUnitTileCache(unit);
                context.UpdateAdjacentUnits();
                context.UpdateTargetsInRange();

                // Sanity check: ensure no other units have invalid or duplicate positions after the move.
                var all = context.Participants.GetAllUnits();
                bool problemFound = false;
                var seen = new System.Collections.Generic.HashSet<Vector2Int>();
                foreach (var u in all)
                {
                    if (u == null)
                    {
                        continue;
                    }

                    var mgp = u.UnitPositionToMapGridPoint(u.MapGridPosition, context.MapGrid);
                    if (mgp == null)
                    {
                        var name = u.CharacterTemplate?.DisplayName ?? "<no-name>";
                        TurnrootLogger.Log(
                            "MoveCommand: Detected invalid MapGridPosition for "
                                + name
                                + ": "
                                + u.MapGridPosition,
                            TurnrootLogger.LogLevel.Warning
                        );
                        problemFound = true;
                    }
                    else if (seen.Contains(u.MapGridPosition))
                    {
                        var name = u.CharacterTemplate?.DisplayName ?? "<no-name>";
                        TurnrootLogger.Log(
                            "MoveCommand: Detected duplicate MapGridPosition for "
                                + name
                                + " at "
                                + u.MapGridPosition,
                            TurnrootLogger.LogLevel.Warning
                        );
                        problemFound = true;
                    }
                    else
                    {
                        seen.Add(u.MapGridPosition);
                    }
                }

                if (problemFound)
                {
                    // Minimal diagnostic + repair attempt (details may be logged elsewhere when needed)
                    TurnrootLogger.Log(
                        "MoveCommand: Problem detected after move; attempting repair",
                        TurnrootLogger.LogLevel.Warning
                    );
                    try
                    {
                        context.RepairUnitPositionsFromRoster();
                    }
                    catch (System.Exception ex)
                    {
                        TurnrootLogger.Log(
                            "MoveCommand: Repair failed: " + ex.Message,
                            TurnrootLogger.LogLevel.Warning
                        );
                    }

                    // Rebuild caches and adjacency after repair
                    context.InvalidateUnitPositionCache();
                    context.UpdateAdjacentUnits();
                    context.UpdateTargetsInRange();
                }

                // Publish event on the priority bus
                context.Brain?.Publish(
                    new Events.UnitMovedEvent(unit, (Vector2Int)UndoState["from"], Target)
                );

                // Also publish typed move events so other systems (UI/flow) can react immediately
                context.Brain?.PublishCharacterMoveCompleted(unit, newPoint);
                context.Brain?.PublishUnitMoved(unit, Target);
                context.Brain?.PublishMoveCompleted(unit, newPoint);

#if UNITY_EDITOR
                try
                {
                    context.DebugVerifyOccupancyAlignment();
                }
                catch (System.Exception ex)
                {
                    TurnrootLogger.Log(
                        "MoveCommand: DebugVerifyOccupancyAlignment failed: " + ex.Message,
                        TurnrootLogger.LogLevel.Warning
                    );
                }
#endif
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
