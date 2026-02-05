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
                    HandleNoUnitSelectedState();
                    break;

                case PlayerTurnStates.UnitSelected:
                    HandleUnitSelectedState();
                    break;

                case PlayerTurnStates.ChoosingDestination:
                    HandleChoosingDestinationState();
                    break;

                case PlayerTurnStates.AttackActionChosenChoosingTarget:
                    HandleAttackActionChoosingTargetState();
                    break;

                case PlayerTurnStates.ChoosingAction:
                    HandleChoosingActionState();
                    break;

                case PlayerTurnStates.DestinationSelected:
                    HandleDestinationSelectedState();
                    break;

                case PlayerTurnStates.TurnEnded:
                    HandleTurnEndedState();
                    break;
            }
        }

        private void HandleNoUnitSelectedState()
        {
            _validMoveTiles.Clear();
            _validAttackTiles.Clear();
            Brain.cursorBrain.ClearAllowedPositions();
            _tileHighlighter.ClearAll();
        }

        private void HandleUnitSelectedState()
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

            _tileHighlighter.HighlightTiles(movePositionsLocal, TileHighlighter.HighlightType.Move);
            _tileHighlighter.HighlightTiles(
                attackPositionsLocal,
                TileHighlighter.HighlightType.Attack
            );

            Brain.cursorBrain.SetAllowedPositions(movePositionsLocal);
        }

        private void HandleChoosingDestinationState()
        {
            var movePositions = new List<Vector2Int>(
                _validMoveTiles.Keys.Select(k => k.CoordinatesInt)
            );

            _tileHighlighter.HighlightTiles(movePositions, TileHighlighter.HighlightType.Move);

            Brain.cursorBrain.SetAllowedPositions(movePositions);
        }

        private void HandleAttackActionChoosingTargetState()
        {
            var attackPositions = new List<Vector2Int>(
                _validAttackTiles.Keys.Select(k => k.CoordinatesInt)
            );

            _tileHighlighter.HighlightTiles(attackPositions, TileHighlighter.HighlightType.Attack);

            Brain.cursorBrain.SetAllowedPositions(attackPositions);
        }

        private void HandleChoosingActionState() => OpenActionMenu();

        private void HandleDestinationSelectedState()
        {
            if (_pendingDestination == null)
            {
                TurnrootLogger.Log(
                    "DestinationSelected: No pending destination - reverting to UnitSelected",
                    TurnrootLogger.LogLevel.Warning
                );
                _playerTurnFlow.CancelTargetOrDestinationChoice(PlayerTurnStates.UnitSelected);
                return;
            }

            var unit = BattleContext.Unit.UnitInstance;
            // Start executing the move and lock input until move/animation completes
            _playerTurnFlow.StartMove();
            Brain.PublishCharacterMoveStarted(unit, _pendingDestination);
            var moveRes = BattleContext.MoveUnitToPoint(unit, _pendingDestination);
            if (moveRes.Success)
            {
                TurnrootLogger.Log($"Started moving unit to {_pendingDestination.CoordinatesInt}");
                // wait for OnMoveAnimationCompleted (visual layer) to call flow.CompleteMove()
            }
            else
            {
                TurnrootLogger.Log(
                    "Failed to start move to the selected destination",
                    TurnrootLogger.LogLevel.Warning
                );
                // Re-enable input since the attempted move did not start
                Brain.battleBrain.IsInputEnabled = true;
                _playerTurnFlow.CancelTargetOrDestinationChoice(PlayerTurnStates.UnitSelected);
            }
            _pendingDestination = null;
        }

        private void HandleTurnEndedState() => CompletePlayerTurn();

        private void CompletePlayerTurn()
        {
            _validMoveTiles.Clear();
            _validAttackTiles.Clear();
            Brain.cursorBrain.ClearAllowedPositions();
            Brain.PublishPlayerTurnEnded();
            _playerTurnFlow.EndTurn();
        }

        #endregion

        #region Navigation Helpers

        private static Vector2 RotateVectorBy90StepsCW(Vector2 v, int steps)
        {
            steps = ((steps % 4) + 4) % 4;
            return steps switch
            {
                0 => v,
                1 => new Vector2(v.y, -v.x),
                2 => new Vector2(-v.x, -v.y),
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
            var currentState = _playerTurnFlow.GetCurrentState();

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

            var currentState = _playerTurnFlow.GetCurrentState();

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

            var currentState = _playerTurnFlow.GetCurrentState();

            switch (currentState)
            {
                case PlayerTurnStates.ChoosingDestination:
                    HandleDestinationSelection(CursorPosition);
                    break;
                case PlayerTurnStates.AttackActionChosenChoosingTarget:
                    HandleTargetSelection();
                    break;
            }
        }

        private void HandleDestinationSelection(MapGridPoint destinationPoint)
        {
            if (destinationPoint == null)
            {
                return;
            }

            _pendingDestination = destinationPoint;
            _playerTurnFlow.SelectTargetOrDestination(PlayerTurnStates.DestinationSelected);

            // If destination is the current unit tile, skip moving and go straight to choosing action
            if (IsDestinationSameAsUnitPosition(destinationPoint))
            {
                _playerTurnFlow.CancelTargetOrDestinationChoice(PlayerTurnStates.ChoosingAction);
                _pendingDestination = null;
            }
        }

        private bool IsDestinationSameAsUnitPosition(MapGridPoint destinationPoint)
        {
            var unit = BattleContext.Unit.UnitInstance;
            if (unit == null)
            {
                return false;
            }

            var unitPoint = unit.UnitPositionToMapGridPoint(
                unit.MapGridPosition,
                Brain.battleBrain?.BattleObject?.Context?.MapGrid
            );

            return unitPoint != null && unitPoint.Equals(destinationPoint);
        }

        private void HandleTargetSelection()
        {
            if (!Brain.cursorBrain.IsCursorOnUnit(out var targetUnit))
            {
                return;
            }

            if (ValidateTargetSelection(targetUnit))
            {
                _playerTurnFlow.SelectTargetOrDestination(
                    PlayerTurnStates.AttackActionChosenTargetSelected
                );
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

            if (BattleContext.Unit.UnitInstance == unit)
            {
                return;
            }

            BattleContext.Unit.UnitInstance = unit;
            if (BattleContext.Flags?.ActiveUnitFlags == null)
            {
                BattleContext.Flags.ActiveUnitFlags = new UnitFlag();
            }
            BattleContext.Flags.ActiveUnitFlags.Unit = unit;
            Brain.PublishPlayerControlledUnitActivated(unit);
            var res = ComputeValidTiles(unit);

            TurnrootLogger.Log(
                $"ChangeSelectedUnit: Valid move tiles count: {_validMoveTiles?.Count ?? 0}, attack tiles count: {_validAttackTiles?.Count ?? 0}"
            );

            _tileHighlighter.ClearAll();
            _tileHighlighter.HighlightTiles(
                new List<Vector2Int>(_validMoveTiles.Keys.Select(k => k.CoordinatesInt)),
                TileHighlighter.HighlightType.Move
            );

            _tileHighlighter.ClearAll();
            _tileHighlighter.HighlightTiles(
                new List<Vector2Int>(_validMoveTiles.Keys.Select(k => k.CoordinatesInt)),
                TileHighlighter.HighlightType.Move
            );
            _tileHighlighter.HighlightTiles(
                new List<Vector2Int>(_validAttackTiles.Keys.Select(k => k.CoordinatesInt)),
                TileHighlighter.HighlightType.Attack
            );

            Brain.cursorBrain.ClearAllowedPositions();
            Brain.cursorBrain.SetAllowedPositions(
                new List<Vector2Int>(_validMoveTiles.Keys.Select(k => k.CoordinatesInt))
            );
        }

        public void RequestUndo() => Brain.PublishPlayerUndoAction();

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

            var context = Brain.battleBrain.BattleObject.Context;
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
            Brain.PublishValidTilesComputed(moveTiles, attackTiles);

            return OperationResult.Successful();
        }

        #endregion
    }
}
