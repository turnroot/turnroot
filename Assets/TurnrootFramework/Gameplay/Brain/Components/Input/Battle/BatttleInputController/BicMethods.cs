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
            // Validate inputs and context
            if (unit == null || BattleContext == null)
            {
                return;
            }

            // Only allow selecting player-controlled units here
            if (!BattleContext.IsPlayerControlledUnit(unit))
            {
                return;
            }

            // Avoid redundant work if already selected
            if (BattleContext.Unit.UnitInstance == unit)
            {
                return;
            }

            // Set the active unit in the battle context so other systems read the correct unit
            BattleContext.Unit.UnitInstance = unit;

            // Notify subscribers that the player's active unit changed (triggers tile recomputation elsewhere)
            _brain.PublishPlayerControlledUnitActivated(unit);

            // Recompute valid tiles for input handling and update visuals
            ComputeValidTiles(unit);
            _tileHighlighter.ClearAll();
            _tileHighlighter.HighlightTiles(
                new List<Vector2Int>(_validMoveTiles.Keys.Select(k => k.CoordinatesInt)),
                TileHighlighter.HighlightType.Move
            );
            _tileHighlighter.HighlightTiles(
                new List<Vector2Int>(_validAttackTiles.Keys.Select(k => k.CoordinatesInt)),
                TileHighlighter.HighlightType.Attack
            );
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
            if (unit == null || BattleContext?.mapGrid == null)
            {
                return OperationResult.Failure("No unit or BattleContext");
            }

            _validMoveTiles.Clear();
            _validAttackTiles.Clear();
            _aiHelper = BattleContext.AIHelper;
            if (_aiHelper == null)
            {
                TurnrootLogger.Log(
                    "BattleInputControllerBrain: AIHelper is null",
                    TurnrootLogger.LogLevel.Error
                );
                return OperationResult.Failure("AIHelper not available");
            }

            var currentPos = unit.UnitPositionToMapGridPoint(
                unit.MapGridPosition,
                BattleContext.mapGrid
            );
            TurnrootLogger.Log(
                $"BattleInputControllerBrain: Computing valid tiles for unit {unit.CharacterTemplate.DisplayName} at {currentPos}"
            );
            bool canHeal = unit.CurrentClass?.ClassData?.Identity?.CanHeal ?? false;

            bool success;
            if (canHeal)
            {
                var healTilesTemp = new Dictionary<MapGridPoint, float>();
                success = _aiHelper.GetTilesForAIWithHealNonAlloc(
                    currentPos,
                    _validMoveTiles,
                    _validAttackTiles,
                    healTilesTemp
                );
                TurnrootLogger.Log(
                    $"BattleInputControllerBrain: Computed {healTilesTemp.Count} heal tiles for unit {unit.CharacterTemplate.DisplayName}"
                );
            }
            else
            {
                success = _aiHelper.GetTilesForAINonAlloc(
                    currentPos,
                    _validMoveTiles,
                    _validAttackTiles
                );
                TurnrootLogger.Log(
                    $"BattleInputControllerBrain: Computed {_validMoveTiles.Count} move tiles for unit {unit.CharacterTemplate.DisplayName}"
                );
            }

            return !success
                ? OperationResult.Failure(
                    $"Failed to calculate tiles for unit {unit.CharacterTemplate.DisplayName}"
                )
                : OperationResult.Successful();
        }

        #endregion
    }
}
