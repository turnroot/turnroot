using System.Collections.Generic;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class CursorBrain
    {
        #region Cursor Movement API (moved)

        /// <summary>
        /// Initialize cursor with optional movement restrictions.
        /// </summary>
        public void InitializeCursor(MapGrid mapGrid, List<Vector2Int> allowedPositions = null)
        {
#if UNITY_EDITOR
            Debug.Log(
                $"CursorBrain.InitializeCursor: Starting initialization. IsInitialized was: {IsInitialized}"
            );
#endif

            _currentMap = mapGrid;
            _allowedPositions = allowedPositions;

            // Create cursor instance if needed
            if (_cursorInstance == null && uiSettings?.BattleCursorPrefab != null)
            {
                _cursorInstance = Instantiate(uiSettings.BattleCursorPrefab);
                _cursorInstance.name = "BattleCursor";

                var mapGridScale = _currentMap.GridScale;
                var scaleFactor = mapGridScale / 10f;
                _cursorInstance.transform.localScale = new Vector3(
                    scaleFactor,
                    scaleFactor,
                    scaleFactor
                );
#if UNITY_EDITOR
                Debug.Log($"CursorBrain: Created cursor instance with scale {scaleFactor}");
#endif
            }

            Vector2Int startPos = GetInitialCursorPosition(allowedPositions);

            var startPoint = _currentMap.GetGridPoint(startPos.x, startPos.y);
            if (startPoint != null)
            {
                CursorPosition = startPoint;
                UpdateCursorVisualPosition(startPos);
                IsInitialized = true;

#if UNITY_EDITOR
                Debug.Log($"CursorBrain: IsInitialized set to TRUE. Cursor at {startPos}");
#endif

                _brain?.PublishCursorPositionChanged(startPos, _currentMap);
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogError(
                    $"CursorBrain: Could not get grid point for position {startPos}! IsInitialized remains: {IsInitialized}"
                );
#endif
            }
        }

        /// <summary>
        /// Move cursor to specific position (validates against restrictions).
        /// </summary>
        public bool MoveCursorTo(Vector2Int position, bool updateBrain = true)
        {
            if (!IsPositionValid(position))
            {
#if UNITY_EDITOR
                Debug.Log($"CursorBrain: Position {position} is not valid for cursor movement");
#endif
                return false;
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

            if (IsPositionValid(targetPos))
            {
                return MoveCursorTo(targetPos);
            }

            return false;
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

#if UNITY_EDITOR
            Debug.Log($"CursorBrain: Set {positions?.Count ?? 0} allowed positions");
#endif
        }

        /// <summary>
        /// Clear movement restrictions (allow cursor to move anywhere on map).
        /// </summary>
        public void ClearAllowedPositions()
        {
            _allowedPositions = null;
            _currentPositionIndex = -1;

#if UNITY_EDITOR
            Debug.Log("CursorBrain: Cleared position restrictions");
#endif
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
