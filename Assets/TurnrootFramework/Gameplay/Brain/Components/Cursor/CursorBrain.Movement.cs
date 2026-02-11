using System.Collections.Generic;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    // Cursor movement and navigation helpers.
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
                    Brain.PublishCursorPositionChanged(position, _currentMap);
                    Brain.PublishBattleCursorMoved(position);
                }

                return true;
            }

            return false;
        }

        // Battle navigation repeat state
        private float _lastBattleNavTime = -999f;
        private Vector2 _lastBattleDirection = Vector2.zero;

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

        // Navigate with cooldown/repeat handling for battle input (uses InputSettingsHelper)
        public bool TryNavigateWithCooldown(Vector2 direction)
        {
            // Ensure cursor and map are available
            if (_currentMap == null || CursorPosition == null)
            {
                return false;
            }

            // Use the same input threshold to filter small stick noise
            if (direction.magnitude < inputThreshold)
            {
                _lastBattleDirection = Vector2.zero;
                return false;
            }

            // Snap to primary axis (up/down/left/right)
            Vector2 snapped =
                Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
                    ? new Vector2(Mathf.Sign(direction.x), 0f)
                    : new Vector2(0f, Mathf.Sign(direction.y));

            float cooldown = InputSettingsHelper.GetInputCooldown();
            float timeNow = Time.time;

            // If direction changed, move immediately and reset timer; otherwise respect cooldown
            if (snapped != _lastBattleDirection)
            {
                _lastBattleDirection = snapped;
                _lastBattleNavTime = timeNow;
                // Execute navigation
                if (Mathf.Abs(snapped.x) > 0f)
                {
                    return NavigateHorizontal(snapped.x > 0f ? 1 : -1);
                }
                else
                {
                    return NavigateVertical(snapped.y > 0f ? 1 : -1);
                }
            }

            if (timeNow - _lastBattleNavTime >= cooldown)
            {
                _lastBattleNavTime = timeNow;
                if (Mathf.Abs(snapped.x) > 0f)
                {
                    return NavigateHorizontal(snapped.x > 0f ? 1 : -1);
                }
                else
                {
                    return NavigateVertical(snapped.y > 0f ? 1 : -1);
                }
            }

            return false;
        }

        // Reset navigation repeat state (call when entering/leaving battle contexts)
        public void ResetNavigationCooldown()
        {
            _lastBattleDirection = Vector2.zero;
            _lastBattleNavTime = -999f;
        }

        public bool NavigateWithWrapping(int direction)
        {
            EnsurePositionIndex();

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
                return NavigateWithWrapping(dir);
            }

            var candidates = GetRowCandidates(cur.Value.y);
            if (candidates.Count == 0)
            {
                return NavigateWithWrapping(dir);
            }

            candidates.Sort((a, b) => a.x.CompareTo(b.x));

            Vector2Int target =
                dir < 0
                    ? FindLeftOrWrap(candidates, cur.Value.x)
                    : FindRightOrWrap(candidates, cur.Value.x);
            return MoveToAndUpdateIndex(target);
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

            var candidates = GetColumnCandidates(cur.Value.x);
            if (candidates.Count == 0)
            {
                return NavigateWithWrapping(dir);
            }

            candidates.Sort((a, b) => a.y.CompareTo(b.y));
            Vector2Int target =
                dir < 0
                    ? FindDownOrWrap(candidates, cur.Value.y)
                    : FindUpOrWrap(candidates, cur.Value.y);
            return MoveToAndUpdateIndex(target);
        }

        // Restrict cursor movement to specific tiles (e.g., valid move/attack range, spawn points).
        public void SetAllowedPositions(List<Vector2Int> positions)
        {
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
        }

        public void ClearAllowedPositions()
        {
            _allowedPositions = null;
            _currentPositionIndex = -1;
        }

        private void EnsurePositionIndex()
        {
            if (_currentPositionIndex < 0 && _allowedPositions != null && CursorPosition != null)
            {
                _currentPositionIndex = _allowedPositions.IndexOf(CursorPosition.CoordinatesInt);
                if (_currentPositionIndex < 0)
                {
                    _currentPositionIndex = 0;
                }
            }
        }

        private List<Vector2Int> GetRowCandidates(int y)
        {
            var list = new List<Vector2Int>();
            foreach (var p in _allowedPositions)
            {
                if (p.y == y)
                {
                    list.Add(p);
                }
            }
            return list;
        }

        private List<Vector2Int> GetColumnCandidates(int x)
        {
            var list = new List<Vector2Int>();
            foreach (var p in _allowedPositions)
            {
                if (p.x == x)
                {
                    list.Add(p);
                }
            }
            return list;
        }

        private Vector2Int FindLeftOrWrap(List<Vector2Int> candidates, int curX)
        {
            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                if (candidates[i].x < curX)
                {
                    return candidates[i];
                }
            }
            return candidates[^1];
        }

        private Vector2Int FindRightOrWrap(List<Vector2Int> candidates, int curX)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].x > curX)
                {
                    return candidates[i];
                }
            }
            return candidates[0];
        }

        private Vector2Int FindDownOrWrap(List<Vector2Int> candidates, int curY)
        {
            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                if (candidates[i].y < curY)
                {
                    return candidates[i];
                }
            }
            return candidates[^1];
        }

        private Vector2Int FindUpOrWrap(List<Vector2Int> candidates, int curY)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].y > curY)
                {
                    return candidates[i];
                }
            }
            return candidates[0];
        }

        private bool MoveToAndUpdateIndex(Vector2Int target)
        {
            var success = MoveCursorTo(target);
            if (success)
            {
                _currentPositionIndex = _allowedPositions.IndexOf(target);
            }
            return success;
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
