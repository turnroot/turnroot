using Turnroot.Gameplay.Brain;
using UnityEngine;

namespace TurnrootFramework.Gameplay.Brain.Segments
{
    public partial class UiBrain : BrainComponent
    {
        #region Battle Cursor Event Handlers
        public GameObject BattleCursorPrefab => uiSettings.BattleCursorPrefab;
        private bool _battleCursorInitialized = false;
        private GameObject _battleCursorInstance;

        public void HandleBattleUi()
        {
#if UNITY_EDITOR
            Debug.Log("UiBrain: Handling battle UI setup");
#endif
            // Battle UI initialization logic will be added here
            // For now, just log that we're in battle state
        }

        public void InitializeBattleCursor()
        {
            if (_battleCursorInstance == null)
            {
                _battleCursorInstance = Instantiate(BattleCursorPrefab);
                _battleCursorInstance.name = "BattleCursor";
                // TODO: Figure out scale
                // For now, the scale is hardcoded, I'll figure it out later
                _battleCursorInstance.transform.localScale = new Vector3(.5f, .5f, .5f);
            }
        }

        public void HandleBattleCursorMoved(Vector2Int newPosition)
        {
            if (!_battleCursorInitialized)
            {
                InitializeBattleCursor();
                _battleCursorInitialized = true;
            }

            if (_battleCursorInstance == null)
            {
                return;
            }

            var mapGrid = _brain.battleBrain.BattleObject.Context.mapGrid;
            if (mapGrid == null)
            {
                return;
            }

            var worldPosition = mapGrid.GetTerrainAdjustedWorldPosition(newPosition);
            _battleCursorInstance.transform.position = worldPosition + new Vector3(0, 1f, -2f); // Slightly above the ground
        }
        #endregion
    }
}
