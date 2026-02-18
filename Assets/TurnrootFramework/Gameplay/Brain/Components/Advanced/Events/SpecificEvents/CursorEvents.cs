using System;
using System.Collections.Generic;
using Turnroot.Gameplay.Maps;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain
    {
        #region Cursor Events

        public event Action<MapGrid, List<Vector2Int>> OnCursorInitializeRequested;
        public event Action<Vector2Int> OnCursorMoveRequested;
        public event Action<List<Vector2Int>> OnCursorRestrictionsRequested;
        public event Action OnCursorRestrictionsClearRequested;
        public event Action OnCursorHideRequested;
        public event Action OnCursorShowRequested;
        public event Action<Vector2Int, MapGrid> OnCursorPositionChanged;

        public void PublishCursorInitializeRequested(
            MapGrid mapGrid,
            List<Vector2Int> allowedPositions = null
        ) => OnCursorInitializeRequested?.Invoke(mapGrid, allowedPositions);

        public void PublishCursorMoveRequested(Vector2Int position) =>
            OnCursorMoveRequested?.Invoke(position);

        public void PublishCursorRestrictionsRequested(List<Vector2Int> allowedPositions) =>
            OnCursorRestrictionsRequested?.Invoke(allowedPositions);

        public void PublishCursorRestrictionsClearRequested() =>
            OnCursorRestrictionsClearRequested?.Invoke();

        public void PublishCursorHideRequested() => OnCursorHideRequested?.Invoke();

        public void PublishCursorShowRequested() => OnCursorShowRequested?.Invoke();

        public void PublishCursorPositionChanged(Vector2Int position, MapGrid mapGrid) =>
            OnCursorPositionChanged?.Invoke(position, mapGrid);

        #endregion
    }
}
