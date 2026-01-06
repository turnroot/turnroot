using Turnroot.Gameplay.Brain;
using UnityEngine;
using UnityEngine.UI;

namespace TurnrootFramework.Gameplay.Brain.Segments
{
    public partial class UiBrain : BrainComponent
    {
        #region Battle Cursor Event Handlers
        public GameObject BattleCursorPrefab => uiSettings.BattleCursorPrefab;
        private bool _battleCursorInitialized = false;
        private GameObject _battleCursorInstance;

        public void InitializeBattleCursor()
        {
            if (_battleCursorInstance == null)
            {
                _battleCursorInstance = Instantiate(BattleCursorPrefab);
                _battleCursorInstance.name = "BattleCursor";
                // TODO: Figure out scale and positioning
                // TODO: Set Camera on prefab canvas to the battle camera
            }
        }

        public void HandleBattleCursorMoved(Vector2Int newPosition)
        {
            if (!_battleCursorInitialized)
            {
                InitializeBattleCursor();
                _battleCursorInitialized = true;
            }
            // TODO: Update the UI representation of the battle cursor
            Debug.Log($"Battle cursor moved to: {newPosition}");
        }
        #endregion
    }
}
