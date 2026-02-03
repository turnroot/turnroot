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
    public partial class BattleInputControllerBrain : BrainComponent
    {
        #region Properties
        public CharacterInstance SelectedUnit =>
            _brain.battleBrain.BattleObject.Context.Unit.UnitInstance;
        public BattleContext BattleContext => _brain.battleBrain.BattleObject.Context;
        public MapGridPoint CursorPosition => _brain.cursorBrain?.CursorPosition;

        private bool IsBattleInputEnabled => _brain.battleBrain.IsInputEnabled;

        [HideInInspector]
        public TileHighlighter _tileHighlighter;

        #endregion

        #region Fields

        private PlayerTurnFlow _playerTurnFlow;
        private Dictionary<MapGridPoint, float> _validMoveTiles = new();
        private Dictionary<MapGridPoint, float> _validAttackTiles = new();

        private BattleInputActions _inputActions;

        private float _lastInputTime;
        private float _cachedInputCooldown;
        private bool _cachedIsKeyboard = true;

        // Add flag to prevent input processing before ready
        private bool _inputEnabled = false;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            _playerTurnFlow = _brain?.battleBrain?.playerTurnFlow;
            _lastInputTime = -999f;
            UpdateInputCooldown();
        }

        private void Start()
        {
            // Check if we're already in battle state (can happen if component loads late)
            if (_brain?.stateBrain?.CurrentState?.Name == BrainStateNames.Battle)
            {
                // Battle already started before we subscribed, initialize manually
                _lastInputTime = Time.time;
                _inputEnabled = false;
                StartCoroutine(InitializeWhenReady());
            }
        }

        private void Update()
        {
            // Don't process input until battle is fully ready; also respect global input enabled flag
            if (!_inputEnabled || !IsBattleInputEnabled)
            {
                return;
            }

            if (Time.time - _lastInputTime < _cachedInputCooldown)
            {
                return;
            }

            if (ProcessInput())
            {
                _lastInputTime = Time.time;
            }
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
            _brain.OnBattleStarted += HandleBattleStarted;
            _brain.OnBattleCompleted += HandleBattleCompleted;
            _brain.OnPlayerControlledUnitActivated += HandlePlayerUnitActivated;
            _brain.OnPlayerTurnStateChanged += new System.Action<PlayerTurnStates>(
                HandlePlayerTurnStateChanged
            );
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            _brain.OnBattleStarted -= HandleBattleStarted;
            _brain.OnBattleCompleted -= HandleBattleCompleted;
            _brain.OnPlayerControlledUnitActivated -= HandlePlayerUnitActivated;
            _brain.OnPlayerTurnStateChanged -= new System.Action<PlayerTurnStates>(
                HandlePlayerTurnStateChanged
            );
        }

        #endregion

        #region Input Processing

        private bool ProcessInput()
        {
            if (_inputActions?.Navigate?.enabled == true)
            {
                var inputVec = _inputActions.Navigate.ReadValue<Vector2>();
                var camAngle = _brain?.cameraBrain?.CurrentAngle ?? 0;
                // a right rotation (1 step) maps Up -> Right.
                var steps = (((int)camAngle % 360) + 360) % 360 / 90;
                var direction = RotateVectorBy90StepsCW(inputVec, steps);
                // Use magnitude threshold for deadzone handling
                if (direction.magnitude > 0.1f)
                {
                    HandleNavigateInput(direction);
                    _brain?.Publish(
                        new BattleContext.BattleInputNavigateEvent { Direction = direction }
                    );
                    return true;
                }
            }

            if (_inputActions?.Confirm?.WasPressedThisFrame() == true)
            {
                _brain?.Publish(new BattleContext.BattleInputConfirmEvent());
                HandleConfirmInput();
                return true;
            }

            if (_inputActions?.Cancel?.WasPressedThisFrame() == true)
            {
                _brain?.Publish(new BattleContext.BattleInputCancelEvent());
                return true;
            }

            if (_inputActions?.Menu?.WasPressedThisFrame() == true)
            {
                _brain?.Publish(new BattleContext.BattleInputMenuEvent());
                return true;
            }

            if (_inputActions?.RotateMapCamera?.enabled == true)
            {
                var rotateValue = _inputActions.RotateMapCamera.ReadValue<float>();
                if (Mathf.Abs(rotateValue) > 0.1f)
                {
                    _brain.cameraBrain.RotateBattleCamera(rotateValue);
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

        // InputAction creation moved into BattleInputActions helper (see BattleInputActions.cs)
        // This keeps this controller focused on handling intent and flow rather than input wiring.

        private void CleanupInputActions()
        {
            _inputEnabled = false;
            _inputActions?.Disable();
            _inputActions?.Dispose();
            _inputActions = null;
        }

        #endregion

        #region Battle Lifecycle Event Handlers

        private void HandleBattleStarted()
        {
            _lastInputTime = Time.time;
            _inputEnabled = false;
            StartCoroutine(InitializeWhenReady());
        }

        private IEnumerator InitializeWhenReady()
        {
            int waitCount = 0;
            while (_brain?.battleBrain?.BattleObject?.Context?.MapGrid == null)
            {
                waitCount++;
                yield return new WaitForSeconds(0.05f);
            }

            waitCount = 0;
            while (_brain?.cursorBrain?.IsInitialized != true)
            {
                waitCount++;
                yield return new WaitForSeconds(0.05f);
            }

            _playerTurnFlow = _brain.battleBrain.playerTurnFlow;
            SetupInputActions();

            yield return null;

            _inputEnabled = true;
            _lastInputTime = Time.time;

            _tileHighlighter = _brain.battleBrain.BattleObject.GetComponent<TileHighlighter>();
        }

        private void HandleBattleCompleted(BattleExitType exitType) => CleanupInputActions();

        #endregion

        #region Input Event Handlers

        public void HandleNavigateInput(Vector2 direction)
        {
            if (
                direction.magnitude < 0.1f
                || _brain == null
                || _brain.cursorBrain == null
                || !_brain.cursorBrain.IsInitialized
            )
            {
                return;
            }

            direction = SnapDirectionToFour(direction);

            var currentState = _playerTurnFlow?.GetCurrentState();
            _brain.cursorBrain.NavigateCursor(direction);

            // TODO: Update UI based on cursor position
            switch (currentState)
            {
                case PlayerTurnStates.UnitSelected:
                case PlayerTurnStates.ChoosingDestination:
                    var path = HandlePathPreview();

                    if (_tileHighlighter == null)
                    {
                        break;
                    }

                    if (path == null || path.Count == 0)
                    {
                        _tileHighlighter.ClearPathPreview();
                        break;
                    }

                    // Path already includes start tile as first element; pass the full path directly
                    _tileHighlighter.HighlightPath(path);
                    break;
                case PlayerTurnStates.AttackActionChosenChoosingTarget:
                    // Update damage preview
                    break;
            }
        }

        public void HandleConfirmInput()
        {
            var currentState = _playerTurnFlow?.GetCurrentState() ?? PlayerTurnStates.Inactive;
            TurnrootLogger.Log(
                $"BattleInputControllerBrain: Handling Confirm Input. Current PlayerTurnState is {currentState}"
            );

            var unitAtCursor = _brain.cursorBrain.GetUnitAtCursor();

            switch (currentState)
            {
                case PlayerTurnStates.Inactive:
                    break;
                case PlayerTurnStates.NoUnitSelected:
                    if (unitAtCursor != null && BattleContext.IsPlayerControlledUnit(unitAtCursor))
                    {
                        _playerTurnFlow.SelectUnit();
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
                    _playerTurnFlow?.ConfirmAction();
                    break;
            }
        }

        public void HandleCancelInput()
        {
            var currentState = _playerTurnFlow?.GetCurrentState() ?? PlayerTurnStates.Inactive;

            switch (currentState)
            {
                case PlayerTurnStates.UnitSelected:
                    _playerTurnFlow?.DeselectUnit();
                    break;
                case PlayerTurnStates.ChoosingDestination:
                    _playerTurnFlow?.CancelTargetOrDestinationChoice(PlayerTurnStates.UnitSelected);
                    _brain.cursorBrain?.ClearAllowedPositions();
                    break;
                case PlayerTurnStates.DestinationSelected:
                    // Cancel destination selection and return to unit selected state
                    _playerTurnFlow?.CancelTargetOrDestinationChoice(PlayerTurnStates.UnitSelected);
                    _brain.cursorBrain?.ClearAllowedPositions();
                    break;
                case PlayerTurnStates.ChoosingAction:
                    // After a move completed, Back undoes the move (handled by PlayerTurnFlow.HandlePlayerUndoAction)
                    RequestUndo();
                    break;
                case PlayerTurnStates.AttackActionChosenChoosingTarget:
                    _playerTurnFlow?.CancelTargetOrDestinationChoice(PlayerTurnStates.UnitSelected);
                    _brain.cursorBrain?.ClearAllowedPositions();
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
                var current = BattleContext?.Unit?.UnitInstance;
                if (current == null || current != unitAtCursor)
                {
                    _playerTurnFlow.SelectUnit();
                    ChangeSelectedUnit(unitAtCursor);
                }
                else
                {
                    OpenActionMenu();
                }

                return;
            }

            // If cursor is on a valid move tile (and not on a unit), start the move immediately
            var cursorPos = _brain.cursorBrain?.CursorPosition;
            if (
                cursorPos != null
                && _playerTurnFlow != null
                && _playerTurnFlow.GetCurrentState() == PlayerTurnStates.UnitSelected
                && BattleContext != null
                && _brain.cursorBrain != null
                && _brain.cursorBrain.GetUnitAtCursor() == null
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
                    _pendingDestination = cursorPos;
                    _playerTurnFlow.SelectDestination(cursorPos);
                    return;
                }
            }

            // Default: open action menu
            OpenActionMenu();
        }

        #endregion
    }
}
