using System.Collections.Generic;
using Turnroot.Gameplay.Maps;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class CursorBrain
    {
        #region Cursor Movement API
        public bool MoveCursorTo(Vector2Int position, bool updateBrain = true)
        {
            if (!IsPositionValid(position))
            {
                return OperationResult
                    .Failure($"CursorBrain: Position {position} is not valid for cursor movement")
                    .Success;
            }

            // Update position index if using restricted movement
            if (_allowedPositions != null)
            {
                _currentPositionIndex = _allowedPositions.IndexOf(position);
            }

            // Update cursor position
            var newGridPoint = _currentMap?.GetGridPoint(position.x, position.y);
            if (newGridPoint != null)
            {
                CursorPosition = newGridPoint;
                UpdateCursorVisualPosition(position);

                // Publish movement events
                if (updateBrain)
                {
                    _brain?.PublishCursorPositionChanged(position, _currentMap);
                    _brain?.PublishBattleCursorMoved(position); // Legacy event
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Navigate cursor by directional input (e.g., from input systems).
        /// </summary>
        public bool NavigateCursor(Vector2 direction)
        {
            if (_currentMap == null || CursorPosition == null)
            {
                return false;
            }

            var gridMovement = GetGridMovementFromDirection(direction, inputThreshold);

            if (gridMovement == Vector2Int.zero)
            {
                return false;
            }

            var targetPos = CursorPosition.CoordinatesInt + gridMovement;

            return IsPositionValid(targetPos) ? MoveCursorTo(targetPos) : false;
        }

        /// <summary>
        /// Navigate with wrapping (for restricted tile lists).
        /// </summary>
        public bool NavigateWithWrapping(int direction)
        {
            if (_allowedPositions == null || _allowedPositions.Count == 0)
            {
                return false;
            }

            // Wrap around the list
            _currentPositionIndex = (_currentPositionIndex + direction) % _allowedPositions.Count;
            if (_currentPositionIndex < 0)
            {
                _currentPositionIndex += _allowedPositions.Count;
            }

            return MoveCursorTo(_allowedPositions[_currentPositionIndex]);
        }

        /// <summary>
        /// Restrict cursor movement to specific tiles (e.g., valid move/attack range, spawn points).
        /// </summary>
        public void SetAllowedPositions(List<Vector2Int> positions)
        {
            _allowedPositions = positions;
            _currentPositionIndex = -1;

            // If current position is no longer valid, snap to nearest valid position
            if (
                _allowedPositions != null
                && CursorPosition != null
                && !_allowedPositions.Contains(CursorPosition.CoordinatesInt)
            )
            {
                var nearest = FindNearestValidPosition(CursorPosition.CoordinatesInt);
                if (nearest.HasValue)
                {
                    MoveCursorTo(nearest.Value);
                }
            }

            TurnrootLogger.Log($"CursorBrain: Set {positions?.Count ?? 0} allowed positions");
        }

        /// <summary>
        /// Clear movement restrictions (allow cursor to move anywhere on map).
        /// </summary>
        public void ClearAllowedPositions()
        {
            _allowedPositions = null;
            _currentPositionIndex = -1;

            TurnrootLogger.Log("CursorBrain: Cleared position restrictions");
        }

        /// <summary>
        /// Show or hide the cursor visual.
        /// </summary>
        public void SetCursorVisibility(bool visible)
        {
            IsVisible = visible;
            if (_cursorInstance != null)
            {
                _cursorInstance.SetActive(visible);
            }
        }

        #endregion
    }
}
