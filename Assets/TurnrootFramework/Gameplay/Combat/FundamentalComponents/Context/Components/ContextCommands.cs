using Turnroot.Characters;
using Turnroot.Characters.Stats;
using Turnroot.Gameplay.Brain.Commands;
using Turnroot.Gameplay.Maps;
using Turnroot.Gameplay.Objects;
using Turnroot.GameSettings;
using Turnroot.Skills.Nodes.Events;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    public partial class BattleContext : MonoBehaviour
    {
        #region Command-Based Actions

        /// <summary>
        /// Moves a unit to a position using the command pattern.
        /// </summary>
        /// <param name="unit">The unit to move.</param>
        /// <param name="targetPosition">The target position.</param>
        /// <returns>True if the move succeeded.</returns>
        public OperationResult MoveUnitToPointInt(CharacterInstance unit, Vector2Int CoordinatesInt)
        {
            var command = new MoveCommand(
                unit.Id,
                CoordinatesInt,
                Brain?.battleBrain.CurrentTurnNumber ?? 0
            );
            return Brain.ExecuteCommand(command)
                ? OperationResult.Successful()
                : OperationResult.Failure("Move command failed to execute");
        }

        public bool SpawnAtPosition(CharacterInstance unit, Vector2Int spawnPosition)
        {
            if (unit == null)
            {
                this.LogWarning("SpawnAtPosition called with null unit");
                return false;
            }

            // make sure the unit is registered with the context so the
            // SpawnCommand can locate it by id during execution.  This also
            // establishes the unit's allegiance for targeting logic.
            EnsureUnitIsParticipant(unit);

            var command = new SpawnCommand(
                unit.Id,
                spawnPosition,
                Brain?.battleBrain.CurrentTurnNumber ?? 0
            );
            bool success = Brain.ExecuteCommand(command);
            if (success)
            {
                // Invalidate all tile caches when new unit spawns
                InvalidateAllTileCaches();
            }
            return success;
        }

        public OperationResult MoveUnitToPoint(CharacterInstance unit, MapGridPoint targetPoint)
        {
            var command = new MoveCommand(
                unit.Id,
                targetPoint.CoordinatesInt,
                Brain.battleBrain.CurrentTurnNumber
            );
            var t = Brain.ExecuteCommand(command)
                ? OperationResult.Successful()
                : OperationResult.Failure("Move command failed to execute");
            if (t.Success)
            {
                this.LogInfo(
                    $"Moved {unit.CharacterTemplate.DisplayName} to {targetPoint.CoordinatesInt}"
                );
            }
            return t;
        }

        #endregion
    }
}
