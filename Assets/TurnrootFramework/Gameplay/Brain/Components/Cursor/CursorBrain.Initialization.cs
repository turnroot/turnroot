using System.Collections.Generic;
using Turnroot.Gameplay.Maps;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class CursorBrain
    {
        private static WaitForSeconds _waitForSeconds0_1 = new WaitForSeconds(0.1f);

        #region Cursor Initialization

        [HideInInspector]
        public Vector3 CursorOffset;

        public OperationResult InitializeCursor(
            MapGrid mapGrid,
            List<Vector2Int> allowedPositions = null
        )
        {
            TurnrootLogger.Log(
                $"CursorBrain.InitializeCursor: Starting initialization. IsInitialized was: {IsInitialized}"
            );

            _currentMap = mapGrid;
            _allowedPositions = allowedPositions;

            // Create cursor instance if needed
            if (_cursorInstance == null && uiSettings?.BattleCursorPrefab != null)
            {
                _cursorInstance = Instantiate(uiSettings.BattleCursorPrefab);
                _cursorInstance.name = "BattleCursor";
            }

            // ALWAYS update scale based on current map, not just on creation
            if (_cursorInstance != null && _currentMap != null)
            {
                var mapGridScale = _currentMap.GridScale;
                var scaleFactor = mapGridScale;
                _cursorInstance.transform.localScale = new Vector3(
                    scaleFactor,
                    scaleFactor,
                    scaleFactor
                );
            }

            Vector2Int startPos = GetInitialCursorPosition(allowedPositions);

            var startPoint = _currentMap.GetGridPoint(startPos.x, startPos.y);
            if (startPoint != null)
            {
                CursorPosition = startPoint;
                UpdateCursorVisualPosition(startPos);
                IsInitialized = true;

                TurnrootLogger.Log($"CursorBrain: IsInitialized set to TRUE. Cursor at {startPos}");

                _brain?.PublishCursorPositionChanged(startPos, _currentMap);
            }
            else
            {
                return OperationResult.Failure(
                    $"InitializeCursor: Could not find valid start grid point at {startPos}."
                );
            }
            return OperationResult.SuccessResult();
        }

        private void InitializeBattleCursor()
        {
            if (_brain?.battleBrain?.BattleObject?.Context?.mapGrid == null)
            {
                StartCoroutine(RetryInitializeBattleCursor());
                return;
            }

            var battleContext = _brain.battleBrain.BattleObject.Context;
            CursorOffset = _brain.uiBrain?.uiSettings?.BattleCursorOffset ?? Vector3.zero;
            InitializeCursor(battleContext.mapGrid);
        }

        private OperationResult InitializePreBattleCursor(MapGrid mapGrid)
        {
            if (mapGrid == null)
            {
                return OperationResult.Failure(
                    "CursorBrain: Cannot initialize pre-battle cursor - no MapGrid"
                );
            }

            // Get valid spawn positions from BattlePreparationObject
            List<Vector2Int> validSpawnPositions = null;

            var prepObject = _brain?.battleBrain?.PreparationObject;
            if (prepObject != null)
            {
                // Get player spawn positions from preparation object
                var spawnPoints = prepObject.PlayerTeamSpawnPoints;
                if (spawnPoints != null && spawnPoints.Count > 0)
                {
                    validSpawnPositions = spawnPoints;
                }
            }
            CursorOffset = _brain.uiBrain?.uiSettings?.BattleCursorOffset ?? Vector3.zero;

            InitializeCursor(mapGrid, validSpawnPositions);
            return OperationResult.SuccessResult();
        }

        private System.Collections.IEnumerator RetryInitializeBattleCursor()
        {
            yield return _waitForSeconds0_1;
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
