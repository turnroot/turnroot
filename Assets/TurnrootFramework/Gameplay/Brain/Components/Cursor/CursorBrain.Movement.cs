using System.Collections.Generic;
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
                TurnrootLogger.Log(
                    $"CursorBrain: Position {position} is INVALID! Reasons: _currentMap null? {_currentMap == null}; _allowedPositions null? {_allowedPositions == null}; contains? {_allowedPositions != null && _allowedPositions.Contains(position)}; contents: {(_allowedPositions != null ? string.Join(", ", _allowedPositions) : "<null>")}",
                    TurnrootLogger.LogLevel.Error
                );
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
                    _brain?.PublishBattleCursorMoved(position);
                }

                return true;
            }

            return false;
        }

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

            return IsPositionValid(targetPos) && MoveCursorTo(targetPos);
        }

        public bool NavigateWithWrapping(int direction)
        {
            if (_currentPositionIndex < 0 && _allowedPositions != null && CursorPosition != null)
            {
                _currentPositionIndex = _allowedPositions.IndexOf(CursorPosition.CoordinatesInt);
                if (_currentPositionIndex < 0)
                {
                    _currentPositionIndex = 0;
                }
            }

            if (_allowedPositions == null || _allowedPositions.Count == 0)
            {
                TurnrootLogger.Log(
                    "NavigateWithWrapping failed: no allowed positions!",
                    TurnrootLogger.LogLevel.Error
                );
                return false;
            }

            // Calculate new index
            int newIndex = (_currentPositionIndex + direction) % _allowedPositions.Count;
            if (newIndex < 0)
            {
                newIndex += _allowedPositions.Count;
            }

            // Try to move - if it fails, don't update the index
            bool success = MoveCursorTo(_allowedPositions[newIndex]);

            if (!success)
            {
                TurnrootLogger.Log(
                    $"NavigateWithWrapping: MoveCursorTo failed, NOT updating currentPositionIndex (stays at {_currentPositionIndex})",
                    TurnrootLogger.LogLevel.Warning
                );
            }
            else
            {
                _currentPositionIndex = newIndex;
            }

            return success;
        }

        public bool NavigateHorizontal(int dir)
        {
            if (_allowedPositions == null || _allowedPositions.Count == 0)
            {
                return false;
            }

            var cur = CursorPosition?.CoordinatesInt;
            if (cur == null)
            {
                // If cursor not initialized, fallback to wrapping behavior
                return NavigateWithWrapping(dir);
            }

            var candidates = new List<Vector2Int>();
            foreach (var p in _allowedPositions)
            {
                if (p.y == cur.Value.y)
                {
                    candidates.Add(p);
                }
            }

            if (candidates.Count == 0)
            {
                return NavigateWithWrapping(dir);
            }

            candidates.Sort((a, b) => a.x.CompareTo(b.x));
            if (dir < 0)
            {
                // Move left: find the largest x < cur.x
                Vector2Int? target = null;
                for (int i = candidates.Count - 1; i >= 0; i--)
                {
                    if (candidates[i].x < cur.Value.x)
                    {
                        target = candidates[i];
                        break;
                    }
                }

                if (!target.HasValue)
                {
                    // wrap to the rightmost in row
                    target = candidates[candidates.Count - 1];
                }

                bool success = MoveCursorTo(target.Value);
                if (success)
                {
                    _currentPositionIndex = _allowedPositions.IndexOf(target.Value);
                }
                return success;
            }

            // Move right: find smallest x > cur.x
            Vector2Int? rightTarget = null;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].x > cur.Value.x)
                {
                    rightTarget = candidates[i];
                    break;
                }
            }

            if (!rightTarget.HasValue)
            {
                // wrap to leftmost
                rightTarget = candidates[0];
            }

            bool successR = MoveCursorTo(rightTarget.Value);
            if (successR)
            {
                _currentPositionIndex = _allowedPositions.IndexOf(rightTarget.Value);
            }
            return successR;
        }

        public bool NavigateVertical(int dir)
        {
            if (_allowedPositions == null || _allowedPositions.Count == 0)
            {
                return false;
            }

            var cur = CursorPosition?.CoordinatesInt;
            if (cur == null)
            {
                return NavigateWithWrapping(dir);
            }

            var candidates = new List<Vector2Int>();
            foreach (var p in _allowedPositions)
            {
                if (p.x == cur.Value.x)
                {
                    candidates.Add(p);
                }
            }

            if (candidates.Count == 0)
            {
                return NavigateWithWrapping(dir);
            }

            candidates.Sort((a, b) => a.y.CompareTo(b.y));
            if (dir < 0)
            {
                // Move down (smaller y)
                Vector2Int? target = null;
                for (int i = candidates.Count - 1; i >= 0; i--)
                {
                    if (candidates[i].y < cur.Value.y)
                    {
                        target = candidates[i];
                        break;
                    }
                }

                if (!target.HasValue)
                {
                    // wrap to bottommost
                    target = candidates[candidates.Count - 1];
                }

                bool success = MoveCursorTo(target.Value);
                if (success)
                {
                    _currentPositionIndex = _allowedPositions.IndexOf(target.Value);
                }
                return success;
            }

            // Move up (larger y)
            Vector2Int? upTarget = null;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].y > cur.Value.y)
                {
                    upTarget = candidates[i];
                    break;
                }
            }

            if (!upTarget.HasValue)
            {
                // wrap to topmost
                upTarget = candidates[0];
            }

            bool successUp = MoveCursorTo(upTarget.Value);
            if (successUp)
            {
                _currentPositionIndex = _allowedPositions.IndexOf(upTarget.Value);
            }
            return successUp;
        }

        /// <summary>
        /// Restrict cursor movement to specific tiles (e.g., valid move/attack range, spawn points).
        /// </summary>
        public void SetAllowedPositions(List<Vector2Int> positions)
        {
            TurnrootLogger.Log(
                $"CursorBrain: SetAllowedPositions called with {positions?.Count ?? 0} positions"
            );
            _allowedPositions = positions;
            // Initialize current index from existing cursor position when possible to avoid skipping the starting tile
            _currentPositionIndex = -1;
            if (_allowedPositions != null && CursorPosition != null)
            {
                var idx = _allowedPositions.IndexOf(CursorPosition.CoordinatesInt);
                if (idx >= 0)
                {
                    _currentPositionIndex = idx;
                }
            }

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
                    // Update current index to the snapped position so wrapping navigation starts from it
                    _currentPositionIndex = _allowedPositions.IndexOf(nearest.Value);
                    TurnrootLogger.Log(
                        $"CursorBrain: Snapped cursor to nearest allowed position {nearest.Value} at index {_currentPositionIndex}"
                    );
                }
            }

            TurnrootLogger.Log($"CursorBrain: Set {positions?.Count ?? 0} allowed positions");
        }

        public void ClearAllowedPositions()
        {
            _allowedPositions = null;
            _currentPositionIndex = -1;

            TurnrootLogger.Log("CursorBrain: Cleared position restrictions");
        }

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
