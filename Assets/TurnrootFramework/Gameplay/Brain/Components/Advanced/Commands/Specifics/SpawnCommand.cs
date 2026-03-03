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
                (
                    $"[SpawnCommand] Could not find unit {UnitId} in context when executing spawn. "
                    + "Make sure the instance has been registered with the BattleContext (e.g. calling "
                    + "BattleContext.SpawnAtPosition which handles registration for you)."
                ).LogWarning();

                return false;
            }

            // Record previous map position so Undo can restore it if needed
            UndoState[UndoStateKeys.From] = unit.MapGridPosition;
            UndoState[UndoStateKeys.WasSpawned] = true;

            var result = context.MapGrid.SetOccupied(
                unit.UnitPositionToMapGridPoint(SpawnPosition, context.MapGrid),
                unit
            );
            if (result.Success)
            {
                unit.WasSpawnedDuringBattle = true;
                context.Brain.Publish(new Events.UnitSpawnedEvent(unit, SpawnPosition));

                // Only take snapshot if battle is fully initialized (not during initial setup)
                if (!context.Brain.battleBrain.IsInitializing)
                {
                    context.Brain.TakeSnapshot();
                }
            }
            return result.Success;
        }

        public override bool Undo(BattleContext context)
        {
            var unit = FindUnit(context, UnitId);
            if (unit == null || !UndoState.TryGetValue(UndoStateKeys.From, out var from))
            {
                return false;
            }

            var result = context.MapGrid.RemoveOccupied(
                unit.UnitPositionToMapGridPoint(SpawnPosition, context.MapGrid)
            );
            if (result.Success)
            {
                unit.WasSpawnedDuringBattle = false;

                // Attempt to restore previous occupancy via MapGrid for authoritative consistency.
                var fromPos = (Vector2Int)from;
                var prevMgp = context.MapGrid.GetGridPoint(fromPos.x, fromPos.y);
                if (prevMgp != null)
                {
                    context.MapGrid.SetOccupied(prevMgp, unit);
                }
                else
                {
                    "SpawnCommand.Undo: Prev grid point missing; falling back to direct MapGridPosition assignment".LogWarning();
                    unit.MapGridPosition = fromPos; // fallback
                }

                context.Brain.Publish(new Events.UnitDespawnedEvent(unit, SpawnPosition));

                // Only take snapshot if battle is fully initialized
                if (!context.Brain.battleBrain.IsInitializing)
                {
                    context.Brain.TakeSnapshot();
                }
            }
            return result.Success;
        }
    }
}
