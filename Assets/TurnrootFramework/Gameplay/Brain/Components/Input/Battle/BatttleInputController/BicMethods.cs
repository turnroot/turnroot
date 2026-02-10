using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components.Battle;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.Maps;
using Turnroot.UI.Components.ListMenu;
using Turnroot.UI.Components.Menu;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Partial class containing helper methods for player turn management and state handling.
    /// </summary>
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
            var movePositions = GetValidMoveCoordinates();
            var attackPositions = GetValidAttackCoordinates();

            _tileHighlighter.HighlightTiles(movePositions, TileHighlighter.HighlightType.Move);
            _tileHighlighter.HighlightTiles(attackPositions, TileHighlighter.HighlightType.Attack);
            Brain.cursorBrain.SetAllowedPositions(movePositions);
        }

        private void HandleChoosingDestinationState()
        {
            var movePositions = GetValidMoveCoordinates();
            _tileHighlighter.HighlightTiles(movePositions, TileHighlighter.HighlightType.Move);
            Brain.cursorBrain.SetAllowedPositions(movePositions);
        }

        private void HandleAttackActionChoosingTargetState()
        {
            var attackPositions = GetValidAttackCoordinates();
            _tileHighlighter.HighlightTiles(attackPositions, TileHighlighter.HighlightType.Attack);
            Brain.cursorBrain.SetAllowedPositions(attackPositions);
        }

        private void HandleChoosingActionState()
        {
            // Check if turn has already ended (shouldn't show menu if so)
            if (_playerTurnFlow.GetCurrentState() == PlayerTurnStates.TurnEnded)
            {
                TurnrootLogger.Log(
                    "Turn already ended, skipping action menu",
                    TurnrootLogger.LogLevel.Warning
                );
                return;
            }

            var result = ShowActionMenu();
            if (!result.Success)
            {
                TurnrootLogger.Log(result.ErrorMessage, TurnrootLogger.LogLevel.Error);
            }
        }

        private void HandleDestinationSelectedState()
        {
            var validation = OperationResultGuards.RequireNotNull(
                _pendingDestination,
                nameof(_pendingDestination)
            );
            if (!validation.Success)
            {
                _playerTurnFlow.CancelTargetOrDestinationChoice(PlayerTurnStates.UnitSelected);
                return;
            }

            // Check if staying in place - skip move and go straight to action menu
            if (IsDestinationSameAsUnitPosition(_pendingDestination))
            {
                _pendingDestination = null;
                _playerTurnFlow.ActionChosen(PlayerTurnStates.ChoosingAction);
                return;
            }

            var unit = BattleContext.Unit.UnitInstance;
            _playerTurnFlow.StartMove();
            Brain.PublishCharacterMoveStarted(unit, _pendingDestination);
            var moveRes = BattleContext.MoveUnitToPoint(unit, _pendingDestination);

            if (!moveRes.Success)
            {
                TurnrootLogger.Log(moveRes.ErrorMessage, TurnrootLogger.LogLevel.Warning);
                Brain.battleBrain.IsInputEnabled = true;
                _playerTurnFlow.CancelTargetOrDestinationChoice(PlayerTurnStates.UnitSelected);
            }

            _pendingDestination = null;
        }

        private void HandleTurnEndedState() => CompletePlayerTurn();

        private void CompletePlayerTurn()
        {
            // This is cleanup code that runs when TurnEnded state is reached
            // DO NOT call EndTurn() here - that would try to transition to TurnEnded when we're already there
            _validMoveTiles.Clear();
            _validAttackTiles.Clear();
            Brain.cursorBrain.ClearAllowedPositions();

            // Note: PlayerTurnEnded is published by TurnRotisserie, not here
            // This used to publish it but caused duplicate events
        }

        #endregion

        #region Helper Methods

        private List<Vector2Int> GetValidMoveCoordinates() =>
            new(
                _validMoveTiles?.Keys.Select(k => k.CoordinatesInt)
                    ?? System.Array.Empty<Vector2Int>()
            );

        private List<Vector2Int> GetValidAttackCoordinates() =>
            new(
                _validAttackTiles?.Keys.Select(k => k.CoordinatesInt)
                    ?? System.Array.Empty<Vector2Int>()
            );

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
            return target != null
                && _playerTurnFlow.GetCurrentState() switch
                {
                    PlayerTurnStates.AttackActionChosenChoosingTarget => BattleContext.IsTarget(
                        target
                    ),
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

            switch (_playerTurnFlow.GetCurrentState())
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
                Brain.battleBrain.BattleObject.Context.MapGrid
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
            if (!BattleContext.IsPlayerControlledUnit(unit))
            {
                TurnrootLogger.Log(
                    "unit is not player-controlled",
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
            ComputeValidTiles(unit);

            // Update adjacency and targets in range for the newly selected unit
            BattleContext.UpdateAdjacentUnits();
            BattleContext.UpdateTargetsInRange();

            HighlightValidTilesForSelectedUnit();
        }

        private void HighlightValidTilesForSelectedUnit()
        {
            var movePositions = GetValidMoveCoordinates();
            var attackPositions = GetValidAttackCoordinates();

            _tileHighlighter.ClearAll();
            _tileHighlighter.HighlightTiles(movePositions, TileHighlighter.HighlightType.Move);
            _tileHighlighter.HighlightTiles(attackPositions, TileHighlighter.HighlightType.Attack);

            Brain.cursorBrain.ClearAllowedPositions();
            Brain.cursorBrain.SetAllowedPositions(movePositions);
        }

        public void RequestUndo() => Brain.PublishPlayerUndoAction();

        #endregion

        #region Action Menu Management

        private OperationResult ShowActionMenu()
        {
            Brain.battleBrain.IsInputEnabled = false;

            var menuLocation = Brain.uiBrain.battleActionSelectMenuLocation;
            var validation = OperationResultGuards.RequireNotNull(
                menuLocation?.prefab,
                "BattleActionSelectMenu prefab"
            );
            if (!validation.Success)
            {
                return validation;
            }

            CloseActionMenu();

            _currentActionMenu = Instantiate(menuLocation.prefab);
            var battleSelectAction =
                _currentActionMenu.GetComponent<UI.Components.BattleSelectAction>();

            validation = OperationResultGuards.RequireNotNull(
                battleSelectAction,
                "BattleSelectAction component"
            );
            if (!validation.Success)
            {
                return validation;
            }

            // TODO: Add more actions (Attack, Item, Trade, etc.)
            var populateResult = battleSelectAction.PopulateList(PopulateActionMenu());

            // Wire up button click handlers
            if (battleSelectAction.ListMenuContainer.TryGetComponent<MenuBase>(out var menuBase))
            {
                menuBase.OnItemSelected += (item) =>
                {
                    if (item is ListMenuItem listMenuItem)
                    {
                        HandleActionSelected(listMenuItem.ItemName);
                    }
                };

                // Disable menu input for one frame to prevent same-frame input processing
                StartCoroutine(EnableMenuInputNextFrame(menuBase));
            }

            return populateResult;
        }

        private System.Collections.IEnumerator EnableMenuInputNextFrame(MenuBase menu)
        {
            if (menu != null)
            {
                menu.enabled = false;

                // Wait until the confirm button is released
                while (_inputActions?.Confirm?.IsPressed() == true)
                {
                    yield return null;
                }

                // Wait one additional frame after release
                yield return null;

                if (menu != null)
                {
                    menu.enabled = true;
                }
            }
        }

        private void CloseActionMenu()
        {
            if (_currentActionMenu != null)
            {
                Destroy(_currentActionMenu);
                _currentActionMenu = null;
            }
        }

        internal string[] PopulateActionMenu()
        {
            var actions = new List<string> { "Wait" };
            // Trade with adjacent allies
            if (BattleContext.Participants.AdjacentUnits.GetAdjacentAllyCount(BattleContext) > 0)
            {
                actions.Add("Trade");
                // TODO: Check talk/support
            }
            // mount/dismount
            if (BattleContext.Unit.UnitInstance.CurrentClass.ClassData.Identity.IsMountedClass())
            {
                if (BattleContext.Unit.UnitInstance.IsMounted)
                {
                    actions.Add("Dismount");
                }
                else
                {
                    actions.Add("Mount");
                }
            }
            // check if any enemies are in range (already updated after movement)
            if (BattleContext.Participants.TargetsInRange.Count > 0)
            {
                actions.Add("Attack");
            }
            return actions.ToArray();
        }

        public void HandleActionSelected(string actionName)
        {
            switch (actionName.ToLower())
            {
                case "wait":
                    HandleWaitAction();
                    break;
                default:
                    TurnrootLogger.Log(
                        $"Unknown action: {actionName}",
                        TurnrootLogger.LogLevel.Warning
                    );
                    break;
            }
        }

        private void HandleWaitAction()
        {
            CloseActionMenu();

            var validation = OperationResultGuards.RequireNotNull(SelectedUnit, "SelectedUnit");
            if (!validation.Success)
            {
                TurnrootLogger.Log(validation.ErrorMessage, TurnrootLogger.LogLevel.Warning);
                return;
            }

            _playerTurnFlow.WaitAndEndTurn();
            // Note: Turn progression is handled by TurnRotisserie via PlayerTurnEnded event
            // DO NOT call Progress() here or turns will advance twice
            Brain.battleBrain.IsInputEnabled = true;
        }

        public void HandleActionMenuBack()
        {
            CloseActionMenu();
            RequestUndo();
        }

        public void OpenActionMenu() =>
            _playerTurnFlow.ActionChosen(PlayerTurnStates.ChoosingAction);

        private OperationResult ComputeValidTiles(CharacterInstance unit)
        {
            var validation = OperationResultGuards.RequireNotNull(unit, nameof(unit));
            if (!validation.Success)
            {
                TurnrootLogger.Log(validation.ErrorMessage, TurnrootLogger.LogLevel.Warning);
                return validation;
            }

            var context = Brain.battleBrain.BattleObject.Context;
            if (!context.TryGetValidTilesForUnit(unit, out var moveTiles, out var attackTiles))
            {
                return OperationResult.Failure(
                    $"Failed to get valid tiles for unit {unit.CharacterTemplate.DisplayName}"
                );
            }

            _validMoveTiles = moveTiles;
            _validAttackTiles = attackTiles;
            Brain.PublishValidTilesComputed(moveTiles, attackTiles);

            return OperationResult.Successful();
        }

        #endregion
    }
}
