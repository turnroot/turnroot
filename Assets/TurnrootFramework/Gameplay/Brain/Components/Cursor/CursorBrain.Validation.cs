using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class CursorBrain
    {
        #region Validation Helpers

        private bool IsPositionValid(Vector2Int position)
        {
            if (_currentMap == null)
            {
                return false;
            }

            // Check allowed positions list if restricted
            if (_allowedPositions != null && !_allowedPositions.Contains(position))
            {
                return false;
            }

            // Check if position is within traversable area
            return IsPositionWithinTraversableArea(position, _currentMap);
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
                minX = Mathf.Min(minX, corner.x);
                maxX = Mathf.Max(maxX, corner.x);
                minY = Mathf.Min(minY, corner.y);
                maxY = Mathf.Max(maxY, corner.y);
            }

            return position.x >= minX
                && position.x <= maxX
                && position.y >= minY
                && position.y <= maxY;
        }

        private Vector2Int? FindNearestValidPosition(Vector2Int from)
        {
            if (_allowedPositions == null || _allowedPositions.Count == 0)
            {
                return null;
            }

            Vector2Int? nearest = null;
            float minDist = float.MaxValue;

            foreach (var pos in _allowedPositions)
            {
                float dist = Vector2Int.Distance(from, pos);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = pos;
                }
            }

            return nearest;
        }

        #endregion
    }
}
