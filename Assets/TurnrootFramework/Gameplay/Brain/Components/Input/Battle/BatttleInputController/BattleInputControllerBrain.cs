using System.Collections;
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components.Battle;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Gameplay.Combat;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.Maps;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Handles player input during battle, managing cursor navigation, unit selection, and action confirmations.
    /// </summary>
    public partial class BattleInputControllerBrain : BrainComponent
    {
        #region Properties
        public CharacterInstance SelectedUnit =>
            Brain.battleBrain.BattleObject.Context.Unit.UnitInstance;
        public BattleContext BattleContext => Brain.battleBrain.BattleObject.Context;
        public MapGridPoint CursorPosition => Brain.cursorBrain.CursorPosition;

        private bool IsBattleInputEnabled => Brain.battleBrain.IsInputEnabled;

        public List<Vector2Int> Path { get; private set; }

        [HideInInspector]
        public TileHighlighter _tileHighlighter;

        [HideInInspector]
        public TerrainTypeOverlay _terrainTypeOverlay;

        #endregion

        #region Fields

        private PlayerTurnFlow _playerTurnFlow;
        private Dictionary<MapGridPoint, float> _validMoveTiles = new();
        private Dictionary<MapGridPoint, float> _validAttackTiles = new();

        private BattleInputActions _inputActions;
        private GameObject _currentActionMenu;

        private float _lastInputTime;
        private float _cachedInputCooldown;
        private bool _cachedIsKeyboard = true;
        private bool _inputEnabled = false;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            _lastInputTime = -999f;
            UpdateInputCooldown();
        }

        private void Update()
        {
            if (!_inputEnabled || !IsBattleInputEnabled)
            {
                return;
            }
            ProcessInput();
        }

        protected override void OnDestroy()
        {
            CleanupInputActions();
            base.OnDestroy();
        }

        #endregion

        #region Brain Event Management

        protected override EventPriority GetSubscriptionPriority() => EventPriority.High;

        protected override void SubscribeToBrainEvents()
        {
            Brain.OnBattleStarted += HandleBattleStarted;
            Brain.OnBattleCompleted += HandleBattleCompleted;
            Brain.OnPlayerControlledUnitActivated += HandlePlayerUnitActivated;
            Brain.OnPlayerTurnStateChanged += new System.Action<PlayerTurnStates>(
                HandlePlayerTurnStateChanged
            );

            // Update path previews when the cursor moves (used for keyboard/controller navigation)
            Brain.OnBattleCursorMoved += HandleCursorMoved;
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            Brain.OnBattleStarted -= HandleBattleStarted;
            Brain.OnBattleCompleted -= HandleBattleCompleted;
            Brain.OnPlayerControlledUnitActivated -= HandlePlayerUnitActivated;
            Brain.OnPlayerTurnStateChanged -= new System.Action<PlayerTurnStates>(
                HandlePlayerTurnStateChanged
            );

            Brain.OnBattleCursorMoved -= HandleCursorMoved;
        }

        #endregion

        #region Input Processing

        private bool ProcessInput()
        {
            if (_inputActions.Navigate?.enabled == true)
            {
                var inputVec = _inputActions.Navigate.ReadValue<Vector2>();
                var camAngle = Brain.cameraBrain.CurrentAngle;
                var steps = (((int)camAngle % 360) + 360) % 360 / 90;
                var direction = RotateVectorBy90StepsCW(inputVec, steps);
                if (direction.magnitude > 0.1f)
                {
                    direction = SnapDirectionToFour(direction);
                    var navigated = Brain.cursorBrain.TryNavigateWithCooldown(direction);
                    if (navigated)
                    {
                        Brain.Publish(
                            new BattleContext.BattleInputNavigateEvent { Direction = direction }
                        );
                        return true;
                    }
                }
            }

            if (_inputActions.Confirm?.WasPressedThisFrame() == true)
            {
                Brain.Publish(new BattleContext.BattleInputConfirmEvent());
                HandleConfirmInput();
                return true;
            }

            if (_inputActions.Cancel?.WasPressedThisFrame() == true)
            {
                Brain.Publish(new BattleContext.BattleInputCancelEvent());
                return true;
            }

            if (_inputActions.Menu?.WasPressedThisFrame() == true)
            {
                Brain.Publish(new BattleContext.BattleInputMenuEvent());
                return true;
            }

            if (_inputActions.RotateCamera?.enabled == true)
            {
                // RotateCamera is configured as a Vector2 (e.g. right stick). Use the X axis for left/right rotation.
                var rotateVec = _inputActions.RotateCamera.ReadValue<Vector2>();
                var rotateValue = rotateVec.x;

                // Fall back to floating value if the action is configured as 1D
                if (Mathf.Approximately(rotateValue, 0f))
                {
                    rotateValue = _inputActions.RotateCamera.ReadValue<float>();
                }

                if (Mathf.Abs(rotateValue) > 0.1f)
                {
                    Brain.cameraBrain.RotateBattleCamera(rotateValue);
                    return true;
                }
            }

            return false;
        }

        private void UpdateInputCooldown()
        {
            _cachedInputCooldown = InputSettingsHelper.GetInputCooldown();
            _cachedIsKeyboard = InputSettingsHelper.IsKeyboardPreferred();
        }

        #endregion

        #region Input Setup & Cleanup

        private void SetupInputActions()
        {
            _inputActions = new BattleInputActions();
            _inputActions.Enable();
        }

        private void CleanupInputActions()
        {
            _inputEnabled = false;
            if (_inputActions != null)
            {
                _inputActions.Disable();
                _inputActions.Dispose();
                _inputActions = null;
            }
        }

        #endregion

        #region Battle Lifecycle Event Handlers

        private void HandleBattleStarted()
        {
            _lastInputTime = Time.time;
            _inputEnabled = false;
            _playerTurnFlow = Brain.battleBrain.playerTurnFlow;
            StartCoroutine(InitializeWhenReady());
        }

        private IEnumerator InitializeWhenReady()
        {
            _tileHighlighter = Brain.battleBrain.BattleObject.TileHighlighter;
            _terrainTypeOverlay = Brain.battleBrain.BattleObject.TerrainTypeOverlay;

            int waitCount = 0;
            while (Brain.battleBrain.BattleObject.Context?.MapGrid == null && waitCount < 100)
            {
                waitCount++;
                yield return new WaitForSeconds(0.05f);
            }

            waitCount = 0;
            while (Brain.cursorBrain.IsInitialized != true && waitCount < 100)
            {
                waitCount++;
                yield return new WaitForSeconds(0.05f);
            }

            SetupInputActions();

            yield return null;

            _inputEnabled = true;
            _lastInputTime = Time.time;
        }

        private void HandleBattleCompleted(BattleExitType exitType) => CleanupInputActions();

        #endregion

        #region Input Event Handlers

        public void HandleNavigateInput(Vector2 direction)
        {
            if (
                direction.magnitude < 0.1f
                || Brain.cursorBrain == null
                || !Brain.cursorBrain.IsInitialized
            )
            {
                return;
            }

            direction = SnapDirectionToFour(direction);

            var currentState = _playerTurnFlow?.GetCurrentState();
            Brain.cursorBrain.NavigateCursor(direction);

            switch (currentState)
            {
                case PlayerTurnStates.UnitSelected:
                case PlayerTurnStates.ChoosingDestination:
                    Path = HandlePathPreview();

                    if (Path == null || Path.Count == 0)
                    {
                        _tileHighlighter.ClearPathPreview();
                        break;
                    }

                    _tileHighlighter.HighlightPath(Path);

                    break;
                case PlayerTurnStates.AttackActionChosenChoosingTarget:
                    // TODO: Damage preview
                    break;
            }
            // terrain type overlay
            if (destination != null)
            {
                _terrainTypeOverlay.Display(
                    destination,
                    SelectedUnit.CurrentClass.ClassData.Identity.MovementType
                );
            }
        }

        private void HandleCursorMoved(Vector2Int pos)
        {
            // Defensive checks: handler can be called very early in startup before
            // this component has been fully initialized. Bail out if we don't have
            // the systems required to compute and display previews.
            if (_tileHighlighter == null || _terrainTypeOverlay == null)
            {
                return;
            }

            // Only update previews when in states that can show them
            var currentState = _playerTurnFlow?.GetCurrentState();
            if (
                currentState == PlayerTurnStates.UnitSelected
                || currentState == PlayerTurnStates.ChoosingDestination
                || currentState == PlayerTurnStates.AttackActionChosenChoosingTarget
            )
            {
                Path = HandlePathPreview();

                if (Path == null || Path.Count == 0)
                {
                    _tileHighlighter.ClearPathPreview();
                }
                else
                {
                    _tileHighlighter.HighlightPath(Path);
                }

                if (destination != null && SelectedUnit != null)
                {
                    var movementType = SelectedUnit
                        ?.CurrentClass
                        ?.ClassData
                        ?.Identity
                        ?.MovementType;
                    if (movementType.HasValue)
                    {
                        _terrainTypeOverlay.Display(destination, movementType.Value);
                    }
                }
            }
            else
            {
                // Clear previews/overlays when not relevant
                _tileHighlighter.ClearPathPreview();
                _terrainTypeOverlay.ResetDisplay();
            }
        }

        public void HandleConfirmInput()
        {
            var currentState = _playerTurnFlow?.GetCurrentState() ?? PlayerTurnStates.Inactive;

            $"BattleInputControllerBrain: Handling Confirm Input. Current PlayerTurnState is {currentState}".LogInfo();

            _terrainTypeOverlay.ResetDisplay();

            var unitAtCursor = Brain.cursorBrain.GetUnitAtCursor();

            switch (currentState)
            {
                case PlayerTurnStates.Inactive:
                    break;
                case PlayerTurnStates.NoUnitSelected:
                    if (unitAtCursor != null && BattleContext.IsPlayerControlledUnit(unitAtCursor))
                    {
                        _playerTurnFlow.SelectUnit(unitAtCursor);
                        ChangeSelectedUnit(unitAtCursor);
                    }
                    else
                    {
                        OpenActionMenu();
                    }
                    break;
                case PlayerTurnStates.UnitSelected:
                    HandleConfirmOnUnitSelected(unitAtCursor);
                    break;
                case PlayerTurnStates.ChoosingDestination:
                case PlayerTurnStates.AttackActionChosenChoosingTarget:
                    ConfirmTileSelection();
                    break;
                case PlayerTurnStates.ConfirmAction:
                    _playerTurnFlow.ConfirmAction();
                    break;
            }
        }

        public void HandleCancelInput()
        {
            var currentState = _playerTurnFlow.GetCurrentState();

            switch (currentState)
            {
                case PlayerTurnStates.UnitSelected:
                    _playerTurnFlow.DeselectUnit();
                    break;
                case PlayerTurnStates.ChoosingDestination:
                    _playerTurnFlow.CancelTargetOrDestinationChoice(PlayerTurnStates.UnitSelected);
                    Brain.cursorBrain.ClearAllowedPositions();
                    break;
                case PlayerTurnStates.DestinationSelected:
                    // Cancel destination selection and return to unit selected state
                    _playerTurnFlow.CancelTargetOrDestinationChoice(PlayerTurnStates.UnitSelected);
                    Brain.cursorBrain.ClearAllowedPositions();
                    break;
                case PlayerTurnStates.ChoosingAction:
                    // After a move completed, Back undoes the move
                    HandleActionMenuBack();
                    break;
                case PlayerTurnStates.AttackActionChosenChoosingTarget:
                    _playerTurnFlow.CancelTargetOrDestinationChoice(PlayerTurnStates.UnitSelected);
                    Brain.cursorBrain.ClearAllowedPositions();
                    break;
                case PlayerTurnStates.ConfirmAction:
                    RequestUndo();
                    break;
            }
        }

        private void HandleConfirmOnUnitSelected(CharacterInstance unitAtCursor)
        {
            // If cursor is on a player unit, select or open action menu
            if (unitAtCursor != null && BattleContext.IsPlayerControlledUnit(unitAtCursor))
            {
                var current = BattleContext.Unit.UnitInstance;
                if (current == null || current != unitAtCursor)
                {
                    _playerTurnFlow.SelectUnit(unitAtCursor);
                    ChangeSelectedUnit(unitAtCursor);
                }
                else
                {
                    // Confirming on the same unit - treat as "stay in place" and open action menu
                    var unitPoint = current.UnitPositionToMapGridPoint(
                        current.MapGridPosition,
                        BattleContext.MapGrid
                    );
                    if (unitPoint != null)
                    {
                        HandleDestinationSelection(unitPoint);
                    }
                }

                return;
            }

            // If cursor is on a valid move tile (and not on a unit), start the move immediately
            var cursorPos = Brain.cursorBrain.CursorPosition;
            if (
                cursorPos != null
                && _playerTurnFlow != null
                && _playerTurnFlow.GetCurrentState() == PlayerTurnStates.UnitSelected
                && BattleContext != null
                && Brain.cursorBrain != null
                && Brain.cursorBrain.GetUnitAtCursor() == null
            )
            {
                if (
                    BattleContext.TryGetValidTilesForUnit(
                        BattleContext.Unit.UnitInstance,
                        out var mv,
                        out var atk
                    ) && mv.ContainsKey(cursorPos)
                )
                {
                    // Check if confirming on the same tile as the unit's current position
                    var unit = BattleContext.Unit.UnitInstance;
                    var unitPoint = unit.UnitPositionToMapGridPoint(
                        unit.MapGridPosition,
                        BattleContext.MapGrid
                    );

                    if (unitPoint != null && unitPoint.Equals(cursorPos))
                    {
                        // Stay in place - open action menu directly
                        _playerTurnFlow.ActionChosen(PlayerTurnStates.ChoosingAction);
                    }
                    else
                    {
                        // Move to a different tile
                        _pendingDestination = cursorPos;
                        _playerTurnFlow.SelectDestination(cursorPos);
                    }
                    return;
                }
            }

            // Default: open action menu
            OpenActionMenu();
        }

        #endregion
    }
}
