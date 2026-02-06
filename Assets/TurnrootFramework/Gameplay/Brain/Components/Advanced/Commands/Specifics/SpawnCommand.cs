using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain.Commands
{
    /// <summary>
    /// Command to spawn a unit at a specific position on the battle map.
    /// </summary>
    public class SpawnCommand : CommandBase
    {
        public string UnitId { get; }
        public Vector2Int SpawnPosition { get; }

        public SpawnCommand(string unitId, Vector2Int spawnPosition, int turn)
            : base(turn)
        {
            UnitId = unitId;
            SpawnPosition = spawnPosition;
        }

        public override bool Execute(BattleContext context)
        {
            var unit = FindUnit(context, UnitId);
            if (unit == null)
            {
                TurnrootLogger.Log(
                    $"[SpawnCommand] Could not find unit {UnitId} in context when executing spawn",
                    TurnrootLogger.LogLevel.Warning
                );
                return false;
            }

            // Record previous map position so Undo can restore it if needed
            UndoState["from"] = unit.MapGridPosition;
            UndoState["wasSpawned"] = true;

            var result = context.MapGrid.SetOccupied(
                unit.UnitPositionToMapGridPoint(SpawnPosition, context.MapGrid),
                unit
            );
            if (result.Success)
            {
                unit.WasSpawnedDuringBattle = true;
                unit.MapGridPosition = SpawnPosition;
                context.Brain?.Publish(new Events.UnitSpawnedEvent(unit, SpawnPosition));
                context.Brain?.TakeSnapshot();
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

            var result = context.MapGrid.RemoveOccupied(
                unit.UnitPositionToMapGridPoint(SpawnPosition, context.MapGrid)
            );
            if (result.Success)
            {
                unit.WasSpawnedDuringBattle = false;
                unit.MapGridPosition = (Vector2Int)from;
                context.Brain?.Publish(new Events.UnitDespawnedEvent(unit, SpawnPosition));
                context.Brain?.TakeSnapshot();
            }
            return result.Success;
        }
    }
}
