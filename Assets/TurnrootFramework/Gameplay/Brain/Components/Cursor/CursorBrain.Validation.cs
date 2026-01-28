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
            return _allowedPositions == null || _allowedPositions.Contains(position);
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
