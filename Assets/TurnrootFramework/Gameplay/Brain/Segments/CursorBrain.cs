using System.Collections.Generic;
using System.Linq;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class CursorBrain : BrainComponent
    {
        private GameObject _cursorInstance;
        private MapGrid _currentMap;
        private List<Vector2Int> _allowedPositions;
        private int _currentPositionIndex;

        [HideInInspector]
        public MapGridPoint CursorPosition; // TODO: Initialize with constraints

        [HideInInspector]
        public MapGridPoint PotentialCursorPosition; // TODO: Use for preview effects

        public GamewideUiSettings uiSettings;

        public OperationResult SetUiSettingsReference(GamewideUiSettings u)
        {
            uiSettings = u;
            return uiSettings != null
                ? OperationResult.SuccessResult()
                : OperationResult.Failure("Invalid gamewideUiSettings");
        }

        protected override void Awake()
        {
            base.Awake();
            SetUiSettingsReference(Brain.uiBrain.uiSettings);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        public void InitializeCursor(MapGrid mapGrid, List<Vector2Int> allowedPositions = null)
        {
            _currentMap = mapGrid;
            _allowedPositions = allowedPositions; // null = all positions allowed

            if (_cursorInstance == null)
            {
                _cursorInstance = Instantiate(uiSettings.BattleCursorPrefab);
                // ... scale setup from your existing code
            }

            // Start at first allowed position or (0,0)
            Vector2Int startPos = allowedPositions?.FirstOrDefault() ?? Vector2Int.zero;
            MoveCursorTo(startPos);
        }

        public void MoveCursorTo(Vector2Int position)
        {
            if (_allowedPositions != null && !_allowedPositions.Contains(position))
            {
                return; // Restricted movement
            }

            _currentPositionIndex = _allowedPositions?.IndexOf(position) ?? -1;

            var battleContext = _brain.battleBrain.BattleObject.Context;
            var mapGrid = battleContext.mapGrid;
            var gridMovement = GetGridMovementFromDirection(direction);

            if (gridMovement == Vector2Int.zero)
            {
                return;
            }

            var targetPos = CursorPosition.CoordinatesInt + gridMovement;

            if (IsPositionWithinTraversableArea(targetPos, mapGrid))
            {
                var newCursorPos = mapGrid.GetGridPoint(targetPos.x, targetPos.y);
                if (newCursorPos != null)
                {
                    CursorPosition = newCursorPos;
                    _brain?.PublishBattleCursorMoved(CursorPosition.CoordinatesInt);
                }
            }
        }

        public void NavigateWithWrapping(Vector2Int direction)
        {
            if (_allowedPositions == null || _allowedPositions.Count == 0)
            {
                return;
            }

            _currentPositionIndex = (_currentPositionIndex + 1) % _allowedPositions.Count;
            MoveCursorTo(_allowedPositions[_currentPositionIndex]);
        }

        private Vector2Int GetGridMovementFromDirection(Vector2 direction, float threshold)
        {
            Vector2Int gridMovement = Vector2Int.zero;

            if (direction.x > threshold)
            {
                gridMovement.x = 1;
            }
            else if (direction.x < -threshold)
            {
                gridMovement.x = -1;
            }

            if (direction.y > threshold)
            {
                gridMovement.y = 1;
            }
            else if (direction.y < -threshold)
            {
                gridMovement.y = -1;
            }

            return gridMovement;
        }

        private bool IsPositionWithinTraversableArea(Vector2Int position, MapGrid mapGrid)
        {
            if (mapGrid == null)
            {
                return false;
            }

            var corners = mapGrid.TraversableAreaCorners;

            // Fallback to full grid if no traversable area defined
            if (corners == null || corners.Length != 4)
            {
                return position.x >= 0
                    && position.x < mapGrid.GridWidth
                    && position.y >= 0
                    && position.y < mapGrid.GridHeight;
            }

            // Calculate traversable area bounds
            int minX = int.MaxValue,
                maxX = int.MinValue;
            int minY = int.MaxValue,
                maxY = int.MinValue;

            foreach (var corner in corners)
            {
                if (corner.x < minX)
                {
                    minX = corner.x;
                }

                if (corner.x > maxX)
                {
                    maxX = corner.x;
                }

                if (corner.y < minY)
                {
                    minY = corner.y;
                }

                if (corner.y > maxY)
                {
                    maxY = corner.y;
                }
            }

            return position.x >= minX
                && position.x <= maxX
                && position.y >= minY
                && position.y <= maxY;
        }

        protected override void SubscribeToBrainEvents()
        {
            // Probably needs to subscribe to SOMETHING, not sure what yet
        }

        protected override void UnsubscribeFromBrainEvents() { }
    }
}
