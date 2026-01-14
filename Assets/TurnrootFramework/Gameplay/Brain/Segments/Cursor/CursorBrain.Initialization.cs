using System.Collections.Generic;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class CursorBrain
    {
        #region Cursor Initialization (moved)

        private void InitializeBattleCursor()
        {
            if (_brain?.battleBrain?.BattleObject?.Context?.mapGrid == null)
            {
                StartCoroutine(RetryInitializeBattleCursor());
                return;
            }

            var battleContext = _brain.battleBrain.BattleObject.Context;
            InitializeCursor(battleContext.mapGrid);

#if UNITY_EDITOR
            Debug.Log(
                $"CursorBrain: Initialized battle cursor at {CursorPosition?.CoordinatesInt}"
            );
#endif
        }

        private void InitializePreBattleCursor(MapGrid mapGrid)
        {
            if (mapGrid == null)
            {
#if UNITY_EDITOR
                Debug.LogError("CursorBrain: Cannot initialize pre-battle cursor - no MapGrid");
#endif
                return;
            }

            // Get valid spawn positions from BattlePreparationObject
            List<Vector2Int> validSpawnPositions = null;

            var prepObject = _brain?.battleBrain?.PreparationObject;
            if (prepObject != null)
            {
                // Get player spawn positions from preparation object
                var spawnPoints = prepObject.PlayerTeamSpawnPoints;
#if UNITY_EDITOR
                Debug.Log(
                    $"CursorBrain.InitializePreBattleCursor: prep={prepObject.name}, spawnPoints.Count={spawnPoints?.Count ?? 0}"
                );
#endif
                if (spawnPoints != null && spawnPoints.Count > 0)
                {
                    validSpawnPositions = spawnPoints;
                }
            }

            InitializeCursor(mapGrid, validSpawnPositions);

#if UNITY_EDITOR
            Debug.Log(
                $"CursorBrain: Initialized pre-battle cursor with {validSpawnPositions?.Count ?? 0} valid spawn positions"
            );
#endif
        }

        private System.Collections.IEnumerator RetryInitializeBattleCursor()
        {
            yield return new WaitForSeconds(0.1f);
            InitializeBattleCursor();
        }

        private Vector2Int GetInitialCursorPosition(List<Vector2Int> allowedPositions)
        {
            // If we have restricted positions, start at the first one
            if (allowedPositions != null && allowedPositions.Count > 0)
            {
                _currentPositionIndex = 0;
                return allowedPositions[0];
            }

            // Otherwise, get camera center from CameraBrain
            if (_brain?.cameraBrain != null)
            {
                var neutralCenter = _brain.cameraBrain.SetBattleGridCameraNeutralCenter();
                var startPos = new Vector2Int(
                    Mathf.RoundToInt(neutralCenter.x),
                    Mathf.RoundToInt(neutralCenter.y)
                );

#if UNITY_EDITOR
                Debug.Log(
                    $"CursorBrain: Using camera neutral center as start position: {startPos}"
                );
#endif

                return startPos;
            }
            return Vector2Int.zero;
        }

        #endregion
    }
}
