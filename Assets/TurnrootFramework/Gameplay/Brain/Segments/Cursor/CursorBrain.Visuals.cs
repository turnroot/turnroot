using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class CursorBrain
    {
        #region Visual Updates (moved)

        private void UpdateCursorVisualPosition(Vector2Int position)
        {
            if (_cursorInstance == null || _currentMap == null)
            {
                return;
            }

            var worldPosition = _currentMap.GetTerrainAdjustedWorldPosition(position);
            _cursorInstance.transform.position = worldPosition + new Vector3(0, 1f, -2f);
        }

        private void CleanupCursor()
        {
            if (_cursorInstance != null)
            {
                Destroy(_cursorInstance);
                _cursorInstance = null;
            }

            IsInitialized = false;
            _currentMap = null;
            _allowedPositions = null;
            _currentPositionIndex = -1;
            _currentContext = CursorContext.None;
            CursorPosition = null;

#if UNITY_EDITOR
            Debug.Log("CursorBrain: Cursor cleaned up");
#endif
        }

        #endregion
    }
}