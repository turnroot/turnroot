using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain.Commands
{
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
                return false;
            }

            UndoState["wasSpawned"] = true;

            var result = context.mapGrid.SetOccupied(
                unit.UnitPositionToMapGridPoint(SpawnPosition, context.mapGrid),
                unit
            );
            TurnrootLogger.Log(
                $"[SpawnCommand] Spawning Unit {UnitId} at {SpawnPosition}: Success={result.Success}"
            );
            if (result.Success)
            {
                // Mark unit as spawned during this battle so snapshot restore can identify reinforcements
                try
                {
                    unit.WasSpawnedDuringBattle = true;
                }
                catch (System.Exception) { }

                context.Brain?.Publish(new Events.UnitSpawnedEvent(unit, SpawnPosition));

                // Take a new snapshot to capture the spawn event immediately (helps testing and rollback)
                try
                {
                    context.Brain?.TakeSnapshot();
                }
                catch (System.Exception) { }
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

            var result = context.mapGrid.RemoveOccupied(
                unit.UnitPositionToMapGridPoint(SpawnPosition, context.mapGrid)
            );
            if (result.Success)
            {
                try
                {
                    unit.WasSpawnedDuringBattle = false;
                }
                catch (System.Exception) { }

                context.Brain?.Publish(new Events.UnitDespawnedEvent(unit, SpawnPosition));

                // Update snapshot to reflect removal
                try
                {
                    context.Brain?.TakeSnapshot();
                }
                catch (System.Exception) { }
            }
            return result.Success;
        }
    }
}
