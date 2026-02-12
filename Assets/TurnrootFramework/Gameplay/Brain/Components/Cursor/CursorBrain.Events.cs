using System.Collections.Generic;
using Turnroot.Gameplay.Maps;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Handles brain event subscriptions and event handlers for cursor operations.
    /// </summary>
    public partial class CursorBrain
    {
        #region Brain Event Subscriptions (moved)

        protected override void SubscribeToBrainEvents()
        {
            _brain.OnCursorInitializeRequested += HandleCursorInitializeRequested;
            _brain.OnCursorMoveRequested += HandleCursorMoveRequested;
            _brain.OnCursorRestrictionsRequested += HandleCursorRestrictionsRequested;
            _brain.OnCursorRestrictionsClearRequested += HandleCursorRestrictionsClearRequested;
            _brain.OnCursorHideRequested += HandleCursorHideRequested;
            _brain.OnCursorShowRequested += HandleCursorShowRequested;

            _brain.OnStateChanged += HandleStateChanged;

            _brain.OnBattleStarted += HandleBattleStarted;
            _brain.OnBattleMapReady += HandleBattleMapReady;
            _brain.OnBattleCompleted += HandleBattleCompleted;

            _brain.OnPreBattlePrepare += HandlePreBattlePrepare;
            _brain.OnPreBattleMapReady += HandlePreBattleMapReady;
            _brain.OnPreBattleCompleted += HandlePreBattleCompleted;

            _brain.OnPlayerControlledUnitActivated += HandlePlayerUnitActivated;

            _brain.OnPlacementsInitialized += HandlePlacementsInitialized;

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
            _brain.OnBattleMapReady -= HandleBattleMapReady;
            _brain.OnBattleCompleted -= HandleBattleCompleted;
            _brain.OnPreBattlePrepare -= HandlePreBattlePrepare;
            _brain.OnPreBattleMapReady -= HandlePreBattleMapReady;
            _brain.OnPreBattleCompleted -= HandlePreBattleCompleted;
            _brain.OnPlayerControlledUnitActivated -= HandlePlayerUnitActivated;
            _brain.OnPlacementsInitialized -= HandlePlacementsInitialized;
        }

        #endregion

        #region Event Handlers

        private void HandlePlacementsInitialized()
        {
            if (_currentContext != CursorContext.PreBattle)
            {
                return;
            }

            var mapGrid = Brain.battleBrain.PreparationObject?.MapGrid;

            if (mapGrid == null)
            {
                "HandlePlacementsInitialized: No MapGrid found in PreparationObject".LogError(
                    "CursorBrain"
                );
                return;
            }

            InitializePreBattleCursor(mapGrid);
        }

        private void HandleStateChanged(BrainState newState)
        {
            var stateName = newState?.Name ?? string.Empty;

            switch (stateName)
            {
                case BrainStateNames.Battle:
                    if (!IsInitialized || _currentContext != CursorContext.Battle)
                    {
                        _currentContext = CursorContext.Battle;
                        ClearAllowedPositions();
                        // Try to initialize immediately if the battle map is ready; otherwise wait for OnBattleMapReady
                        if (Brain.battleBrain.BattleObject.Context?.MapGrid != null)
                        {
                            InitializeBattleCursor();
                        }
                        else
                        {
                            "Battle map not ready yet - waiting for OnBattleMapReady".LogInfo(
                                "CursorBrain"
                            );
                        }

                        // Reset repeat state so navigation starts fresh when entering Battle context
                        ResetNavigationCooldown();
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
                // If the map is already ready, initialize; otherwise we'll initialize when OnBattleMapReady fires
                if (Brain.battleBrain.BattleObject.Context?.MapGrid != null)
                {
                    InitializeBattleCursor();
                }
            }
        }

        private void HandleBattleMapReady(MapGrid mapGrid)
        {
            // Initialize the battle cursor when the battle map becomes available, but only if we're in Battle context
            if (_currentContext == CursorContext.Battle)
            {
                InitializeBattleCursor();
            }
        }

        private void HandleBattleCompleted(Combat.BattleExitType exitType)
        {
            if (_currentContext == CursorContext.Battle)
            {
                CleanupCursor();
            }
        }

        private void HandlePreBattlePrepare() => _currentContext = CursorContext.PreBattle;

        private void HandlePreBattleMapReady(MapGrid mapGrid)
        {
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
                // Publish position changes so other systems (camera, input previews) are updated immediately.
                MoveCursorTo(unitPos, updateBrain: true);
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
    }
}
