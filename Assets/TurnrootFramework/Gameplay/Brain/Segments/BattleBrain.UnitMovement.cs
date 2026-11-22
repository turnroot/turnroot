using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components.Battle;
using Turnroot.Gameplay.Combat;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.Maps;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Turnroot.Gameplay.Brain
{
    public partial class BattleBrain : BrainComponent
    {
        #region Unit Movement

        /// <summary>
        /// Internal helper to move a unit on the grid and publish movement events.
        /// This should only be called by command implementations (e.g., MoveCommand) so that movement is undoable/redoable.
        /// Use <see cref="BattleContext.MoveUnitToPoint"/> to perform a commanded move.
        /// </summary>
        internal bool MoveUnit(CharacterInstance unit, Vector2Int target, MapGrid mapGrid)
        {
            if (unit == null || mapGrid == null)
            {
                return false;
            }

            var from = unit.MapGridPosition;
            var oldPoint = unit.UnitPositionToMapGridPoint(from, mapGrid);

            // Fire movement-start before mutating data so UnitAppearanceBrain can build a path
            // from the pre-move position (including Undo-driven moves).
            var destinationPoint = mapGrid.GetGridPoint(target.x, target.y);
            if (destinationPoint != null)
            {
                Brain.PublishCharacterMoveStarted(unit, destinationPoint);
            }

            var result = unit.MoveToPosition(target, mapGrid);
            if (result.Success)
            {
                var newPoint = unit.UnitPositionToMapGridPoint(target, mapGrid);
                mapGrid.RemoveOccupied(oldPoint);
                mapGrid.SetOccupied(newPoint, unit);
                BattleObject.Context.InvalidateUnitTileCache(unit);

                if (BattleObject.Context.Unit?.UnitInstance == unit)
                {
                    BattleObject.Context.UpdateAdjacentUnits();
                    BattleObject.Context.UpdateTargetsInRange();
                }

                // Keep event order aligned with MoveCommand.Execute to avoid subtle listener regressions.
                Brain.Publish(new Events.UnitMovedEvent(unit, from, target));
                Brain.PublishCharacterMoveCompleted(unit, newPoint);
                Brain.PublishUnitMoved(unit, target);
                Brain.PublishMoveCompleted(unit, newPoint);
            }
            return result.Success;
        }

        #endregion
    }
}