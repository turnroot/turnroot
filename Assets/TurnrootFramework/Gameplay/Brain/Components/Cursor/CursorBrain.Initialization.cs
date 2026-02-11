using System.Collections.Generic;
using Turnroot.Gameplay.Maps;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Handles cursor initialization for battle and pre-battle contexts.
    /// </summary>
    public partial class CursorBrain
    {
        private static WaitForSeconds _waitForSeconds0_1 = new(0.1f);

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
            if (Brain.battleBrain.BattleObject.Context?.MapGrid == null)
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

            // Use PreparationObject.MapGrid for consistency with visual positioning
            var mapGrid =
                Brain.battleBrain.PreparationObject?.MapGrid
                ?? Brain.battleBrain.BattleObject.Context?.MapGrid;
            CursorOffset = Brain.uiBrain.uiSettings?.BattleCursorOffset ?? Vector3.zero;

            // Determine allowed cursor start positions from actual roster placements (battle roster > prep placements > spawn points)
            List<Vector2Int> allowedPositions = null;
            var playerRoster = Brain.battleBrain.BattleObject.PlayerTeamRoster;
            var roPlacements = playerRoster?.GetPlacements();
            if (roPlacements != null && roPlacements.Length > 0)
            {
                allowedPositions = new List<Vector2Int>();
                foreach (var p in roPlacements)
                {
                    if (p != null)
                    {
                        allowedPositions.Add(p.SpawnPosition);
                    }
                }
            }

            if (allowedPositions == null || allowedPositions.Count == 0)
            {
                var prep = Brain.battleBrain.PreparationObject;
                if (prep?.placements != null && prep.placements.Count > 0)
                {
                    allowedPositions = new List<Vector2Int>(prep.placements.Keys);
                }
                else if (
                    prep?.PlayerTeamSpawnPoints != null
                    && prep.PlayerTeamSpawnPoints.Count > 0
                )
                {
                    allowedPositions = new List<Vector2Int>(prep.PlayerTeamSpawnPoints);
                }
            }

            InitializeCursor(mapGrid, allowedPositions);
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

            var prepObject = Brain.battleBrain.PreparationObject;
            if (prepObject != null)
            {
                // Get player spawn positions from preparation object
                var spawnPoints = prepObject.PlayerTeamSpawnPoints;
                if (spawnPoints != null && spawnPoints.Count > 0)
                {
                    validSpawnPositions = spawnPoints;
                }
            }
            CursorOffset = Brain.uiBrain.uiSettings?.BattleCursorOffset ?? Vector3.zero;

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
