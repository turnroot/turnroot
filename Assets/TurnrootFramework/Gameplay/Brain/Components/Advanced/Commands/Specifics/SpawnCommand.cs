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
                TurnrootLogger.Log(
                    $"[SpawnCommand] Could not find unit {UnitId} in context when executing spawn",
                    TurnrootLogger.LogLevel.Warning
                );
                return false;
            }

            // Record previous map position so Undo can restore it if needed
            UndoState["from"] = unit.MapGridPosition;
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
                catch (System.Exception)
                {
                    TurnrootLogger.Log(
                        $"[SpawnCommand] Warning: Could not set WasSpawnedDuringBattle for Unit {UnitId}",
                        TurnrootLogger.LogLevel.Warning
                    );
                }

                // Ensure the instance's logical map position matches the spawn location
                try
                {
                    unit.MapGridPosition = SpawnPosition;
                }
                catch (System.Exception)
                {
                    TurnrootLogger.Log(
                        $"[SpawnCommand] Warning: Could not set MapGridPosition for Unit {UnitId}",
                        TurnrootLogger.LogLevel.Warning
                    );
                }

                context.Brain?.Publish(new Events.UnitSpawnedEvent(unit, SpawnPosition));

                // Take a new snapshot to capture the spawn event immediately (helps testing and rollback)
                try
                {
                    context.Brain?.TakeSnapshot();
                }
                catch (System.Exception)
                {
                    TurnrootLogger.Log(
                        $"[SpawnCommand] Warning: Could not take snapshot after spawning Unit {UnitId}",
                        TurnrootLogger.LogLevel.Warning
                    );
                }
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
                catch (System.Exception)
                {
                    TurnrootLogger.Log(
                        $"[SpawnCommand] Warning: Could not unset WasSpawnedDuringBattle for Unit {UnitId}",
                        TurnrootLogger.LogLevel.Warning
                    );
                }

                // Restore the previous logical map position recorded during Execute
                try
                {
                    unit.MapGridPosition = (Vector2Int)from;
                }
                catch (System.Exception)
                {
                    TurnrootLogger.Log(
                        $"[SpawnCommand] Warning: Could not restore MapGridPosition for Unit {UnitId}",
                        TurnrootLogger.LogLevel.Warning
                    );
                }

                context.Brain?.Publish(new Events.UnitDespawnedEvent(unit, SpawnPosition));

                // Update snapshot to reflect removal
                try
                {
                    context.Brain?.TakeSnapshot();
                }
                catch (System.Exception)
                {
                    TurnrootLogger.Log(
                        $"[SpawnCommand] Warning: Could not take snapshot after despawning Unit {UnitId}",
                        TurnrootLogger.LogLevel.Warning
                    );
                }
            }
            return result.Success;
        }
    }
}
