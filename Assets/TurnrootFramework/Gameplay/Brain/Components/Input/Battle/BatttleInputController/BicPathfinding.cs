using System.Collections.Generic;
using Turnroot.Gameplay.Brain.Components.Battle;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.Maps;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class BattleInputControllerBrain : BrainComponent
    {
        private List<Vector2Int> HandlePathPreview()
        {
            if (!ShouldShowPathPreview())
            {
                ClearPathPreview();
                return new List<Vector2Int>();
            }

            var destination = GetValidDestination();
            if (destination == null)
            {
                ClearPathPreview();
                return new List<Vector2Int>();
            }

            return BuildPathPreview(destination);
        }

        private bool ShouldShowPathPreview()
        {
            var currentState = _playerTurnFlow?.GetCurrentState() ?? PlayerTurnStates.Inactive;

            if (currentState == PlayerTurnStates.ChoosingDestination)
            {
                return SelectedUnit != null
                    && BattleContext?.MapGrid != null
                    && CursorPosition != null
                    && _validMoveTiles != null
                    && _validMoveTiles.Count > 0;
            }

            if (currentState == PlayerTurnStates.UnitSelected)
            {
                if (
                    SelectedUnit != null
                    && BattleContext?.MapGrid != null
                    && CursorPosition != null
                    && _validMoveTiles != null
                    && _validMoveTiles.Count > 0
                    && _validMoveTiles.ContainsKey(CursorPosition)
                    && Brain.cursorBrain?.GetUnitAtCursor() == null
                )
                {
                    return true;
                }
            }

            return false;
        }

        private MapGridPoint GetValidDestination()
        {
            var targetPoint = CursorPosition;
            return _validMoveTiles.ContainsKey(targetPoint)
                ? targetPoint
                : FindClosestReachableTile(targetPoint);
        }

        private MapGridPoint FindClosestReachableTile(MapGridPoint target)
        {
            MapGridPoint closest = null;
            float bestDistSqr = float.MaxValue;
            Vector2 targetCoords = target.Coordinates();

            foreach (var point in _validMoveTiles.Keys)
            {
                float distSqr = (point.Coordinates() - targetCoords).sqrMagnitude;
                if (distSqr < bestDistSqr)
                {
                    bestDistSqr = distSqr;
                    closest = point;
                }
            }

            return closest;
        }

        private List<Vector2Int> BuildPathPreview(MapGridPoint destination)
        {
            var unit = SelectedUnit;
            var startPoint = unit.UnitPositionToMapGridPoint(
                unit.MapGridPosition,
                BattleContext.MapGrid
            );

            if (startPoint == null)
            {
                ClearPathPreview();
                return new List<Vector2Int>();
            }

            var astar = new AStarModified();
            var pathPoints = astar.GetPathThroughReachable(
                startPoint,
                destination,
                _validMoveTiles
            );

            return ConvertPathToCoordinates(pathPoints, startPoint);
        }

        private List<Vector2Int> ConvertPathToCoordinates(
            IEnumerable<MapGridPoint> pathPoints,
            MapGridPoint startPoint
        )
        {
            var result = new List<Vector2Int>();
            result.Add(startPoint.CoordinatesInt);

            foreach (var point in pathPoints)
            {
                if (point.Equals(startPoint))
                {
                    continue;
                }

                if (!_validMoveTiles.ContainsKey(point))
                {
                    break;
                }

                result.Add(point.CoordinatesInt);
            }
            return result;
        }

        private void ClearPathPreview() => _tileHighlighter.ClearPathPreview();
    }
}
