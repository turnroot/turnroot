using System;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain
    {
        #region Battle Cursor Events

        public event Action<Vector2Int> OnBattleCursorMoved;

        public void PublishBattleCursorMoved(Vector2Int cursorPosition) =>
            OnBattleCursorMoved?.Invoke(cursorPosition);

        #endregion
    }
}
