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

        private void HandlePlayerUnitActivated(CharacterInstance unit) => ComputeValidTiles(unit);

        private void HandlePlayerTurnStateChanged(PlayerTurnStates newState)
        {
            TurnrootLogger.Log(
                $"BattleInputControllerBrain notes that Player turn state changed to {newState}"
            );

            switch (newState)
            {
                case PlayerTurnStates.NoUnitSelected:
                    _validMoveTiles.Clear();
                    _validAttackTiles.Clear();
                    _brain.cursorBrain?.ClearAllowedPositions();
                    _tileHighlighter?.ClearAll();
                    break;

                case PlayerTurnStates.NoActionChosen:
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

                        TurnrootLogger.Log(
                            $"HandlePlayerTurnStateChanged: Highlighting {movePositionsLocal.Count} move tiles and {attackPositionsLocal.Count} attack tiles"
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

                        _brain.cursorBrain?.SetAllowedPositions(movePositionsLocal);
                    }
                    break;

                case PlayerTurnStates.MoveActionChosenChoosingDestination:
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
                PlayerTurnStates.MoveActionChosenChoosingDestination => _validMoveTiles.ContainsKey(
                    point
                ),
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
                case PlayerTurnStates.MoveActionChosenChoosingDestination:
                    _playerTurnFlow.SelectTargetOrDestination(
                        PlayerTurnStates.MoveActionChosenDestinationSelected
                    );
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
            TurnrootLogger.Log(
                $"ChangeSelectedUnit: called for unit {unit?.CharacterTemplate?.DisplayName}"
            );
            // Validate inputs and context
            if (unit == null || BattleContext == null)
            {
                TurnrootLogger.Log(
                    "ChangeSelectedUnit: unit or BattleContext null - aborting",
                    TurnrootLogger.LogLevel.Warning
                );
                return;
            }

            // Only allow selecting player-controlled units here
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

            // Set the active unit in the battle context so other systems read the correct unit
            BattleContext.Unit.UnitInstance = unit;

            // Notify subscribers that the player's active unit changed (triggers tile recomputation elsewhere)
            _brain.PublishPlayerControlledUnitActivated(unit);

            // Recompute valid tiles for input handling and update visuals
            var res = ComputeValidTiles(unit);
            TurnrootLogger.Log($"ChangeSelectedUnit: ComputeValidTiles result: {res.Success}");

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

        public void OpenActionMenu() => _playerTurnFlow?.SelectUnit();

        public void RequestUndo() => _brain?.PublishPlayerUndoAction();

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

            // Store references for UI purposes
            _validMoveTiles = moveTiles;
            _validAttackTiles = attackTiles;

            // Publish for UI highlighting
            _brain.PublishValidTilesComputed(moveTiles, attackTiles);

            return OperationResult.Successful();
        }

        #endregion
    }
}
