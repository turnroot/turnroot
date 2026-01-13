using System.Collections.Generic;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Manages cursor positioning and movement across different game states.
    /// Decoupled from input handling - responds to brain events rather than processing input directly.
    /// Works in Battle, PreBattle, WorldMap, and other states that need cursor interaction.
    /// </summary>
    public partial class CursorBrain : BrainComponent
    {
        #region Fields

        private GameObject _cursorInstance;
        private MapGrid _currentMap;
        private List<Vector2Int> _allowedPositions;
        private int _currentPositionIndex;
        private CursorContext _currentContext = CursorContext.None;

        [HideInInspector]
        public MapGridPoint CursorPosition;

        [HideInInspector]
        public GamewideUiSettings uiSettings;

        private float inputThreshold = 0.5f;

        private enum CursorContext
        {
            None,
            Battle,
            PreBattle,
        }

        #endregion

        #region Initialization

        public OperationResult SetUiSettingsReference(GamewideUiSettings u)
        {
            uiSettings = u;
            return uiSettings != null
                ? OperationResult.SuccessResult()
                : OperationResult.Failure("Invalid gamewideUiSettings");
        }

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnDestroy()
        {
            CleanupCursor();
            base.OnDestroy();
        }

        #endregion

        #region Brain Event Subscriptions

        protected override void SubscribeToBrainEvents()
        {
            // Generic cursor events (work across all states)
            _brain.OnCursorInitializeRequested += HandleCursorInitializeRequested;
            _brain.OnCursorMoveRequested += HandleCursorMoveRequested;
            _brain.OnCursorRestrictionsRequested += HandleCursorRestrictionsRequested;
            _brain.OnCursorRestrictionsClearRequested += HandleCursorRestrictionsClearRequested;
            _brain.OnCursorHideRequested += HandleCursorHideRequested;
            _brain.OnCursorShowRequested += HandleCursorShowRequested;

            // State change events
            _brain.OnStateChanged += HandleStateChanged;

            // Battle lifecycle events
            _brain.OnBattleStarted += HandleBattleStarted;
            _brain.OnBattleCompleted += HandleBattleCompleted;

            // Pre-battle events
            _brain.OnPreBattlePrepare += HandlePreBattlePrepare;
            _brain.OnPreBattleMapReady += HandlePreBattleMapReady;
            _brain.OnPreBattleCompleted += HandlePreBattleCompleted;

            // Player unit activation
            _brain.OnPlayerControlledUnitActivated += HandlePlayerUnitActivated;

            if (Brain?.gamewideContextBrain?.PlayerSettings != null)
            {
                inputThreshold =
                    Brain.gamewideContextBrain.PlayerSettings.PreferredInputControl
                    == PlayerSettings.GameplayPlayerSettings.InputControlType.Keyboard
                        ? .1f
                        : .5f;
            }
            else
            {
                inputThreshold = 0.3f; // Safe default
            }
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            _brain.OnCursorInitializeRequested -= HandleCursorInitializeRequested;
            _brain.OnCursorMoveRequested -= HandleCursorMoveRequested;
            _brain.OnCursorRestrictionsRequested -= HandleCursorRestrictionsRequested;
            _brain.OnCursorRestrictionsClearRequested -= HandleCursorRestrictionsClearRequested;
            _brain.OnCursorHideRequested -= HandleCursorHideRequested;
            _brain.OnCursorShowRequested -= HandleCursorShowRequested;
            _brain.OnStateChanged -= HandleStateChanged;
            _brain.OnBattleStarted -= HandleBattleStarted;
            _brain.OnBattleCompleted -= HandleBattleCompleted;
            _brain.OnPreBattlePrepare -= HandlePreBattlePrepare;
            _brain.OnPreBattleMapReady -= HandlePreBattleMapReady;
            _brain.OnPreBattleCompleted -= HandlePreBattleCompleted;
            _brain.OnPlayerControlledUnitActivated -= HandlePlayerUnitActivated;
        }

        #endregion

        #region Event Handlers

        private void HandleStateChanged(BrainState newState)
        {
            var stateName = newState?.Name ?? string.Empty;

            switch (stateName)
            {
                case BrainStateNames.Battle:
                    if (!IsInitialized || _currentContext != CursorContext.Battle)
                    {
                        _currentContext = CursorContext.Battle;
                        InitializeBattleCursor();
                    }
                    break;

                case BrainStateNames.PreBattle:
                    if (!IsInitialized || _currentContext != CursorContext.PreBattle)
                    {
                        _currentContext = CursorContext.PreBattle;
                        // Pre-battle cursor initialization happens when map is ready
                    }
                    break;

                default:
                    // Clean up cursor when leaving states that use it
                    if (
                        IsInitialized
                        && stateName != BrainStateNames.Battle
                        && stateName != BrainStateNames.PreBattle
                    )
                    {
                        CleanupCursor();
                    }
                    break;
            }
        }

        private void HandleBattleStarted()
        {
            if (_currentContext != CursorContext.Battle)
            {
                _currentContext = CursorContext.Battle;
                // Don't initialize immediately - wait for map to be confirmed ready
                StartCoroutine(WaitForBattleMapReady());
            }
        }

        private System.Collections.IEnumerator WaitForBattleMapReady()
        {
            int retries = 0;
            while (_brain?.battleBrain?.BattleObject?.Context?.mapGrid == null && retries < 20)
            {
                retries++;
                yield return new WaitForSeconds(0.1f);
            }

            if (_brain?.battleBrain?.BattleObject?.Context?.mapGrid != null)
            {
                InitializeBattleCursor();
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogError("CursorBrain: Battle map never became ready");
#endif
            }
        }

        private void HandleBattleCompleted(Combat.BattleExitType exitType)
        {
            if (_currentContext == CursorContext.Battle)
            {
                CleanupCursor();
            }
        }

        private void HandlePreBattlePrepare()
        {
#if UNITY_EDITOR
            Debug.Log("CursorBrain: Pre-battle prepare event received");
#endif
            _currentContext = CursorContext.PreBattle;
        }

        private void HandlePreBattleMapReady(MapGrid mapGrid)
        {
#if UNITY_EDITOR
            Debug.Log($"CursorBrain: Pre-battle map ready with grid {mapGrid?.name}");
#endif
            if (_currentContext == CursorContext.PreBattle && mapGrid != null)
            {
                InitializePreBattleCursor(mapGrid);
            }
        }

        private void HandlePreBattleCompleted()
        {
            if (_currentContext == CursorContext.PreBattle)
            {
                _currentContext = CursorContext.Battle;
            }
        }

        private void HandlePlayerUnitActivated(Characters.CharacterInstance unit)
        {
            if (unit == null || _currentMap == null || _currentContext != CursorContext.Battle)
            {
                return;
            }

            // Optionally snap cursor to activated unit's position in battle
            var unitPos = unit.MapGridPosition;
            var gridPoint = _currentMap.GetGridPoint(unitPos.x, unitPos.y);

            if (gridPoint != null && IsPositionValid(unitPos))
            {
                MoveCursorTo(unitPos, updateBrain: false);
            }
        }

        private void HandleCursorInitializeRequested(
            MapGrid mapGrid,
            List<Vector2Int> allowedPositions
        ) => InitializeCursor(mapGrid, allowedPositions);

        private void HandleCursorMoveRequested(Vector2Int position) => MoveCursorTo(position);

        private void HandleCursorRestrictionsRequested(List<Vector2Int> allowedPositions) =>
            SetAllowedPositions(allowedPositions);

        private void HandleCursorRestrictionsClearRequested() => ClearAllowedPositions();

        private void HandleCursorHideRequested() => SetCursorVisibility(false);

        private void HandleCursorShowRequested() => SetCursorVisibility(true);

        #endregion

        #region Cursor Initialization

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

        #endregion

        #region Cursor Movement API

        /// <summary>
        /// Initialize cursor with optional movement restrictions.
        /// </summary>
        public void InitializeCursor(MapGrid mapGrid, List<Vector2Int> allowedPositions = null)
        {
            _currentMap = mapGrid;
            _allowedPositions = allowedPositions;

            // Create cursor instance if needed
            if (_cursorInstance == null && uiSettings?.BattleCursorPrefab != null)
            {
                _cursorInstance = Instantiate(uiSettings.BattleCursorPrefab);
                _cursorInstance.name = "BattleCursor";

                var mapGridScale = _currentMap.GridScale;
                var scaleFactor = mapGridScale / 10f;
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

                _brain?.PublishCursorPositionChanged(startPos, _currentMap);
            }
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

                return startPos;
#endif
            }
            return Vector2Int.zero;
        }

        /// <summary>
        /// Move cursor to specific position (validates against restrictions).
        /// </summary>
        public bool MoveCursorTo(Vector2Int position, bool updateBrain = true)
        {
            if (!IsPositionValid(position))
            {
#if UNITY_EDITOR
                Debug.Log($"CursorBrain: Position {position} is not valid for cursor movement");
#endif
                return false;
            }

            // Update position index if using restricted movement
            if (_allowedPositions != null)
            {
                _currentPositionIndex = _allowedPositions.IndexOf(position);
            }

            // Update cursor position
            var newGridPoint = _currentMap?.GetGridPoint(position.x, position.y);
            if (newGridPoint != null)
            {
                CursorPosition = newGridPoint;
                UpdateCursorVisualPosition(position);

                // Publish movement events
                if (updateBrain)
                {
                    _brain?.PublishCursorPositionChanged(position, _currentMap);
                    _brain?.PublishBattleCursorMoved(position); // Legacy event
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Navigate cursor by directional input (e.g., from input systems).
        /// </summary>
        public bool NavigateCursor(Vector2 direction)
        {
            if (_currentMap == null || CursorPosition == null)
            {
                return false;
            }

            var gridMovement = GetGridMovementFromDirection(direction, inputThreshold);

            if (gridMovement == Vector2Int.zero)
            {
                return false;
            }

            var targetPos = CursorPosition.CoordinatesInt + gridMovement;

            if (IsPositionValid(targetPos))
            {
                return MoveCursorTo(targetPos);
            }

            return false;
        }

        /// <summary>
        /// Navigate with wrapping (for restricted tile lists).
        /// </summary>
        public bool NavigateWithWrapping(int direction)
        {
            if (_allowedPositions == null || _allowedPositions.Count == 0)
            {
                return false;
            }

            // Wrap around the list
            _currentPositionIndex = (_currentPositionIndex + direction) % _allowedPositions.Count;
            if (_currentPositionIndex < 0)
            {
                _currentPositionIndex += _allowedPositions.Count;
            }

            return MoveCursorTo(_allowedPositions[_currentPositionIndex]);
        }

        /// <summary>
        /// Restrict cursor movement to specific tiles (e.g., valid move/attack range, spawn points).
        /// </summary>
        public void SetAllowedPositions(List<Vector2Int> positions)
        {
            _allowedPositions = positions;
            _currentPositionIndex = -1;

            // If current position is no longer valid, snap to nearest valid position
            if (
                _allowedPositions != null
                && CursorPosition != null
                && !_allowedPositions.Contains(CursorPosition.CoordinatesInt)
            )
            {
                var nearest = FindNearestValidPosition(CursorPosition.CoordinatesInt);
                if (nearest.HasValue)
                {
                    MoveCursorTo(nearest.Value);
                }
            }

#if UNITY_EDITOR
            Debug.Log($"CursorBrain: Set {positions?.Count ?? 0} allowed positions");
#endif
        }

        /// <summary>
        /// Clear movement restrictions (allow cursor to move anywhere on map).
        /// </summary>
        public void ClearAllowedPositions()
        {
            _allowedPositions = null;
            _currentPositionIndex = -1;

#if UNITY_EDITOR
            Debug.Log("CursorBrain: Cleared position restrictions");
#endif
        }

        /// <summary>
        /// Show or hide the cursor visual.
        /// </summary>
        public void SetCursorVisibility(bool visible)
        {
            IsVisible = visible;
            if (_cursorInstance != null)
            {
                _cursorInstance.SetActive(visible);
            }
        }

        #endregion

        #region Validation Helpers

        private bool IsPositionValid(Vector2Int position)
        {
            if (_currentMap == null)
            {
                return false;
            }

            // Check allowed positions list if restricted
            if (_allowedPositions != null && !_allowedPositions.Contains(position))
            {
                return false;
            }

            // Check if position is within traversable area
            return IsPositionWithinTraversableArea(position, _currentMap);
        }

        private bool IsPositionWithinTraversableArea(Vector2Int position, MapGrid mapGrid)
        {
            if (mapGrid == null)
            {
                return false;
            }

            var corners = mapGrid.TraversableAreaCorners;

            // Fallback to full grid if no traversable area defined
            if (corners == null || corners.Length != 4)
            {
                return position.x >= 0
                    && position.x < mapGrid.GridWidth
                    && position.y >= 0
                    && position.y < mapGrid.GridHeight;
            }

            // Calculate traversable area bounds
            int minX = int.MaxValue,
                maxX = int.MinValue;
            int minY = int.MaxValue,
                maxY = int.MinValue;

            foreach (var corner in corners)
            {
                minX = Mathf.Min(minX, corner.x);
                maxX = Mathf.Max(maxX, corner.x);
                minY = Mathf.Min(minY, corner.y);
                maxY = Mathf.Max(maxY, corner.y);
            }

            return position.x >= minX
                && position.x <= maxX
                && position.y >= minY
                && position.y <= maxY;
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

        #region Visual Updates

        private void UpdateCursorVisualPosition(Vector2Int position)
        {
            if (_cursorInstance == null || _currentMap == null)
            {
                return;
            }

            var worldPosition = _currentMap.GetTerrainAdjustedWorldPosition(position);
            _cursorInstance.transform.position = worldPosition + new Vector3(0, 1f, -2f);
        }

        #endregion

        #region Utility Methods

        private Vector2Int GetGridMovementFromDirection(Vector2 direction, float threshold)
        {
            Vector2Int gridMovement = Vector2Int.zero;

            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                // Horizontal movement dominates
                if (direction.x > threshold)
                {
                    gridMovement.x = 1;
                }
                else if (direction.x < -threshold)
                {
                    gridMovement.x = -1;
                }
            }
            else
            {
                // Vertical movement dominates
                if (direction.y > threshold)
                {
                    gridMovement.y = 1;
                }
                else if (direction.y < -threshold)
                {
                    gridMovement.y = -1;
                }
            }

            return gridMovement;
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

        #region Public Query API

        /// <summary>
        /// Get the character instance at the current cursor position.
        /// </summary>
        public Characters.CharacterInstance GetUnitAtCursor()
        {
            if (CursorPosition == null)
            {
                return null;
            }

            // In battle context
            if (
                _currentContext == CursorContext.Battle
                && _brain?.battleBrain?.BattleObject?.Context != null
            )
            {
                var cache = _brain.battleBrain.BattleObject.Context.GetCurrentUnitPositions();
                return cache.TryGetValue(CursorPosition.CoordinatesInt, out var unit) ? unit : null;
            }

            // In pre-battle context, check preparation object
            if (
                _currentContext == CursorContext.PreBattle
                && _brain?.battleBrain?.PreparationObject != null
            )
            {
                // TODO: Query pre-battle unit placements
                return null;
            }

            return null;
        }

        public bool IsCursorOnUnit(out Characters.CharacterInstance unit)
        {
            unit = GetUnitAtCursor();
            return unit != null;
        }

        public bool IsCursorOnValidSpawnPoint()
        {
            return _currentContext != CursorContext.PreBattle || CursorPosition == null
                ? false
                : _allowedPositions?.Contains(CursorPosition.CoordinatesInt) ?? false;
        }

        public bool IsInitialized { get; private set; } = false;
        public bool IsVisible { get; private set; } = true;

        public string GetCurrentContext() => _currentContext.ToString();

#if UNITY_EDITOR
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                Debug.Log($"=== CursorBrain Debug ===");
                Debug.Log($"IsInitialized: {IsInitialized}");
                Debug.Log($"Context: {_currentContext}");
                Debug.Log($"CursorInstance: {(_cursorInstance != null ? "EXISTS" : "NULL")}");
                Debug.Log($"CurrentMap: {(_currentMap != null ? _currentMap.name : "NULL")}");
                Debug.Log($"CursorPosition: {CursorPosition?.CoordinatesInt}");
                Debug.Log(
                    $"BattleContext.mapGrid: {(_brain?.battleBrain?.BattleObject?.Context?.mapGrid != null ? "EXISTS" : "NULL")}"
                );
            }
        }
#endif

        #endregion
    }
}
