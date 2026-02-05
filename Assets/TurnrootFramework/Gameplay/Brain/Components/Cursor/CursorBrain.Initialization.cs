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
            _currentMap = mapGrid;
            _allowedPositions = allowedPositions;

            // Create cursor instance if needed
            if (_cursorInstance == null && uiSettings?.BattleCursorPrefab != null)
            {
                _cursorInstance = Instantiate(uiSettings.BattleCursorPrefab);
                _cursorInstance.name = "BattleCursor";
            }

            Vector2Int startPos = GetInitialCursorPosition(allowedPositions);

            var startPoint = _currentMap.GetGridPoint(startPos.x, startPos.y);
            if (startPoint != null)
            {
                CursorPosition = startPoint;
                UpdateCursorVisualPosition(startPos);
                IsInitialized = true;
                Brain.PublishCursorPositionChanged(startPos, _currentMap);
            }
            else
            {
                return OperationResult.Failure(
                    $"InitializeCursor: Could not find valid start grid point at {startPos}."
                );
            }
            return OperationResult.Successful();
        }

        private void InitializeBattleCursor()
        {
            // This is called from HandleBattleMapReady event, so MapGrid should be ready
            // If it's not, that's a real bug we should fix rather than retry
            if (Brain.battleBrain?.BattleObject?.Context?.MapGrid == null)
            {
                TurnrootLogger.Log(
                    "CursorBrain: Cannot initialize battle cursor - MapGrid is null even after OnBattleMapReady event. This indicates an initialization order bug.",
                    TurnrootLogger.LogLevel.Error
                );
                return;
            }

            if (_cursorInstance != null)
            {
                return;
            }

            var battleContext = Brain.battleBrain.BattleObject.Context;
            CursorOffset = Brain.uiBrain?.uiSettings?.BattleCursorOffset ?? Vector3.zero;
            InitializeCursor(battleContext.MapGrid);
        }

        private OperationResult InitializePreBattleCursor(MapGrid mapGrid)
        {
            if (mapGrid == null)
            {
                return OperationResult.Failure(
                    "CursorBrain: Cannot initialize pre-battle cursor - no MapGrid"
                );
            }

            if (_cursorInstance != null)
            {
                // No need to re-initialize if already done
                return OperationResult.Successful();
            }

            // Get valid spawn positions from BattlePreparationObject
            List<Vector2Int> validSpawnPositions = null;

            var prepObject = Brain.battleBrain?.PreparationObject;
            if (prepObject != null)
            {
                // Get player spawn positions from preparation object
                var spawnPoints = prepObject.PlayerTeamSpawnPoints;
                if (spawnPoints != null && spawnPoints.Count > 0)
                {
                    validSpawnPositions = spawnPoints;
                }
            }
            CursorOffset = Brain.uiBrain?.uiSettings?.BattleCursorOffset ?? Vector3.zero;

            InitializeCursor(mapGrid, validSpawnPositions);
            return OperationResult.Successful();
        }

        private Vector2Int GetInitialCursorPosition(List<Vector2Int> allowedPositions)
        {
            if (allowedPositions != null && allowedPositions.Count > 0)
            {
                _currentPositionIndex = 0;
                return allowedPositions[0];
            }

            if (Brain.cameraBrain != null)
            {
                var neutralCenter = Brain.cameraBrain.SetBattleGridCameraNeutralCenter();
                var startPos = new Vector2Int(
                    Mathf.RoundToInt(neutralCenter.x),
                    Mathf.RoundToInt(neutralCenter.y)
                );

                return startPos;
            }
            return Vector2Int.zero;
        }

        #endregion
    }
}
