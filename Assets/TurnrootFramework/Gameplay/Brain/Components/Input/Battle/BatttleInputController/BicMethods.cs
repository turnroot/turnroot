using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components.Battle;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.Maps;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class BattleInputControllerBrain : BrainComponent
    {
        #region Player Turn Management

        private MapGridPoint _pendingDestination;

        private void HandlePlayerUnitActivated(CharacterInstance unit) => ComputeValidTiles(unit);

        private void HandlePlayerTurnStateChanged(PlayerTurnStates newState)
        {
            switch (newState)
            {
                case PlayerTurnStates.NoUnitSelected:
                    _validMoveTiles.Clear();
                    _validAttackTiles.Clear();
                    _brain.cursorBrain?.ClearAllowedPositions();
                    _tileHighlighter?.ClearAll();
                    break;

                case PlayerTurnStates.UnitSelected:
                    {
                        // A unit has been selected but no action chosen yet — highlight available moves/attacks
                        var movePositionsLocal = new List<Vector2Int>(
                            _validMoveTiles?.Keys.Select(k => k.CoordinatesInt)
                                ?? System.Array.Empty<Vector2Int>()
                        );
                        var attackPositionsLocal = new List<Vector2Int>(
                            _validAttackTiles?.Keys.Select(k => k.CoordinatesInt)
                                ?? System.Array.Empty<Vector2Int>()
                        );

                        if (_tileHighlighter != null)
                        {
                            _tileHighlighter.HighlightTiles(
                                movePositionsLocal,
                                TileHighlighter.HighlightType.Move
                            );
                            _tileHighlighter.HighlightTiles(
                                attackPositionsLocal,
                                TileHighlighter.HighlightType.Attack
                            );
                        }

                        _brain.cursorBrain.SetAllowedPositions(movePositionsLocal);
                    }
                    break;

                case PlayerTurnStates.ChoosingDestination:
                    var movePositions = new List<Vector2Int>(
                        _validMoveTiles.Keys.Select(k => k.CoordinatesInt)
                    );
                    _tileHighlighter.HighlightTiles(
                        movePositions,
                        TileHighlighter.HighlightType.Move
                    );
                    _brain.cursorBrain?.SetAllowedPositions(movePositions);
                    break;

                case PlayerTurnStates.AttackActionChosenChoosingTarget:
                    var attackPositions = new List<Vector2Int>(
                        _validAttackTiles.Keys.Select(k => k.CoordinatesInt)
                    );
                    _tileHighlighter.HighlightTiles(
                        attackPositions,
                        TileHighlighter.HighlightType.Attack
                    );
                    _brain.cursorBrain?.SetAllowedPositions(attackPositions);
                    break;

                case PlayerTurnStates.ChoosingAction:
                    {
                        // Recompute valid tiles for the unit now that it has moved and caches were invalidated by MoveUnit
                        var unit = BattleContext?.Unit?.UnitInstance;
                        if (unit != null)
                        {
                            ComputeValidTiles(unit);
                        }
                        // After moving, allow player to pick an action at the new position
                        OpenActionMenu();
                    }
                    break;

                case PlayerTurnStates.DestinationSelected:
                    if (_pendingDestination == null)
                    {
                        TurnrootLogger.Log(
                            "DestinationSelected: No pending destination - reverting to UnitSelected",
                            TurnrootLogger.LogLevel.Warning
                        );
                        _playerTurnFlow.CancelTargetOrDestinationChoice(
                            PlayerTurnStates.UnitSelected
                        );
                        break;
                    }

                    {
                        var unit = BattleContext.Unit.UnitInstance;
                        // Start executing the move and lock input until move/animation completes
                        _playerTurnFlow.StartMove();
                        _brain.PublishCharacterMoveStarted(unit, _pendingDestination);
                        var moveRes = BattleContext.MoveUnitToPoint(unit, _pendingDestination);
                        if (moveRes.Success)
                        {
                            TurnrootLogger.Log(
                                $"Started moving unit to {_pendingDestination.CoordinatesInt}"
                            );
                            // wait for OnUnitFinishedMovingAfterAction (model layer) to call flow.CompleteMove()
                        }
                        else
                        {
                            TurnrootLogger.Log(
                                "Failed to start move to the selected destination",
                                TurnrootLogger.LogLevel.Warning
                            );
                            _playerTurnFlow.CancelTargetOrDestinationChoice(
                                PlayerTurnStates.UnitSelected
                            );
                        }
                        _pendingDestination = null;
                    }
                    break;

                case PlayerTurnStates.TurnEnded:
                    CompletePlayerTurn();
                    break;
            }
        }

        private void CompletePlayerTurn()
        {
            _validMoveTiles.Clear();
            _validAttackTiles.Clear();
            _brain.cursorBrain?.ClearAllowedPositions();
            _brain.PublishPlayerTurnEnded();
            _playerTurnFlow?.EndTurn();
        }

        #endregion

        #region Navigation Helpers

        private static Vector2 RotateVectorBy90StepsCW(Vector2 v, int steps)
        {
            // Normalize steps to 0..3
            steps = ((steps % 4) + 4) % 4;
            // Apply clockwise 90° rotation steps using integer math to avoid trig imprecision
            return steps switch
            {
                0 => v,
                // 90° clockwise: (x,y) -> (y, -x)
                1 => new Vector2(v.y, -v.x),
                // 180°: (x,y) -> (-x, -y)
                2 => new Vector2(-v.x, -v.y),
                // 270° clockwise (or 90° ccw): (x,y) -> (-y, x)
                3 => new Vector2(-v.y, v.x),
                _ => v,
            };
        }

        private static Vector2 SnapDirectionToFour(Vector2 v)
        {
            if (v.magnitude < 0.0001f)
            {
                return Vector2.zero;
            }

            var angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            // Snap to nearest 45 degrees (8 directions including diagonals)
            var snapped = Mathf.Round(angle / 45f) * 45f;
            var rad = snapped * Mathf.Deg2Rad;
            // Round cosine/sine to avoid floating point imprecision and yield exact integer direction vectors
            return new Vector2(Mathf.Round(Mathf.Cos(rad)), Mathf.Round(Mathf.Sin(rad)));
        }

        #endregion

        #region Validation

        public bool ValidateTileSelection(MapGridPoint point)
        {
            var currentState = _playerTurnFlow?.GetCurrentState() ?? PlayerTurnStates.Inactive;

            return currentState switch
            {
                PlayerTurnStates.ChoosingDestination => _validMoveTiles.ContainsKey(point),
                PlayerTurnStates.AttackActionChosenChoosingTarget => _validAttackTiles.ContainsKey(
                    point
                ),
                _ => false,
            };
        }

        public bool ValidateTargetSelection(CharacterInstance target)
        {
            if (target == null)
            {
                return false;
            }

            var currentState = _playerTurnFlow?.GetCurrentState() ?? PlayerTurnStates.Inactive;

            return currentState switch
            {
                PlayerTurnStates.AttackActionChosenChoosingTarget => BattleContext.IsTarget(target),
                PlayerTurnStates.HealActionChosenChoosingTarget => BattleContext.IsAlly(target),
                _ => false,
            };
        }

        #endregion

        #region Action Methods

        public void ConfirmTileSelection()
        {
            if (CursorPosition == null || !ValidateTileSelection(CursorPosition))
            {
                return;
            }

            var currentState = _playerTurnFlow?.GetCurrentState() ?? PlayerTurnStates.Inactive;

            switch (currentState)
            {
                case PlayerTurnStates.ChoosingDestination:
                    var destinationPoint = CursorPosition;
                    if (destinationPoint != null)
                    {
                        _pendingDestination = destinationPoint;
                        _playerTurnFlow.SelectTargetOrDestination(
                            PlayerTurnStates.DestinationSelected
                        );

                        // If destination is the current unit tile, skip moving and go straight to choosing action
                        var unit = BattleContext.Unit.UnitInstance;
                        var unitPoint = unit?.UnitPositionToMapGridPoint(
                            unit.MapGridPosition,
                            _brain?.battleBrain?.BattleObject?.Context?.mapGrid
                        );
                        if (unitPoint != null && unitPoint.Equals(destinationPoint))
                        {
                            // Directly enter ChoosingAction (no move required)
                            _playerTurnFlow.CancelTargetOrDestinationChoice(
                                PlayerTurnStates.ChoosingAction
                            );
                            _pendingDestination = null;
                            break;
                        }
                    }
                    break;
                case PlayerTurnStates.AttackActionChosenChoosingTarget:
                    if (_brain.cursorBrain.IsCursorOnUnit(out var targetUnit))
                    {
                        if (ValidateTargetSelection(targetUnit))
                        {
                            _playerTurnFlow.SelectTargetOrDestination(
                                PlayerTurnStates.AttackActionChosenTargetSelected
                            );
                        }
                    }
                    break;
            }
        }

        public void ChangeSelectedUnit(CharacterInstance unit)
        {
            if (unit == null || BattleContext == null)
            {
                TurnrootLogger.Log(
                    "ChangeSelectedUnit: unit or BattleContext null - aborting",
                    TurnrootLogger.LogLevel.Warning
                );
                return;
            }

            // Only allow selecting player-controlled units
            if (!BattleContext.IsPlayerControlledUnit(unit))
            {
                TurnrootLogger.Log(
                    "ChangeSelectedUnit: unit is not player-controlled - aborting",
                    TurnrootLogger.LogLevel.Warning
                );
                return;
            }

            // Avoid redundant work if already selected
            if (BattleContext.Unit.UnitInstance == unit)
            {
                TurnrootLogger.Log("ChangeSelectedUnit: unit already active - skipping");
                return;
            }

            BattleContext.Unit.UnitInstance = unit;
            _brain.PublishPlayerControlledUnitActivated(unit);
            var res = ComputeValidTiles(unit);

            TurnrootLogger.Log(
                $"ChangeSelectedUnit: Valid move tiles count: {_validMoveTiles?.Count ?? 0}, attack tiles count: {_validAttackTiles?.Count ?? 0}"
            );

            if (_tileHighlighter == null)
            {
                TurnrootLogger.Log(
                    "ChangeSelectedUnit: _tileHighlighter is null - cannot highlight tiles",
                    TurnrootLogger.LogLevel.Warning
                );
            }
            else
            {
                _tileHighlighter.ClearAll();
                _tileHighlighter.HighlightTiles(
                    new List<Vector2Int>(_validMoveTiles.Keys.Select(k => k.CoordinatesInt)),
                    TileHighlighter.HighlightType.Move
                );
                _tileHighlighter.HighlightTiles(
                    new List<Vector2Int>(_validAttackTiles.Keys.Select(k => k.CoordinatesInt)),
                    TileHighlighter.HighlightType.Attack
                );
            }

            _brain.cursorBrain.ClearAllowedPositions();
            _brain.cursorBrain.SetAllowedPositions(
                new List<Vector2Int>(_validMoveTiles.Keys.Select(k => k.CoordinatesInt))
            );
        }

        public void RequestUndo() => _brain?.PublishPlayerUndoAction();

        public void OpenActionMenu() { }

        public void OpenMenu()
        {
            // TODO: Battle pause menu
        }

        private OperationResult ComputeValidTiles(CharacterInstance unit)
        {
            if (unit == null)
            {
                TurnrootLogger.Log(
                    "BattleInputControllerBrain: Cannot compute tiles for null unit",
                    TurnrootLogger.LogLevel.Warning
                );
                return OperationResult.Failure("No unit provided");
            }

            var context = _brain.battleBrain.BattleObject.Context;
            if (context == null)
            {
                TurnrootLogger.Log(
                    "BattleInputControllerBrain: BattleContext is null",
                    TurnrootLogger.LogLevel.Error
                );
                return OperationResult.Failure("BattleContext not available");
            }

            if (!context.TryGetValidTilesForUnit(unit, out var moveTiles, out var attackTiles))
            {
                TurnrootLogger.Log(
                    $"BattleInputControllerBrain: Failed to get valid tiles for unit {unit.CharacterTemplate.DisplayName}",
                    TurnrootLogger.LogLevel.Warning
                );
                return OperationResult.Failure("Failed to compute tiles");
            }

            _validMoveTiles = moveTiles;
            _validAttackTiles = attackTiles;
            _brain.PublishValidTilesComputed(moveTiles, attackTiles);

            return OperationResult.Successful();
        }

        #endregion
    }
}
