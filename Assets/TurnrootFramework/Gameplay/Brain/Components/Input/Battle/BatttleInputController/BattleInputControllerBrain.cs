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

        [HideInInspector]
        public TileHighlighter _tileHighlighter;

        #endregion

        #region Fields

        private PlayerTurnFlow _playerTurnFlow;
        private BattleContextAIHelper _aiHelper;

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
            // Don't process input until battle is fully ready
            if (!_inputEnabled)
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
            _cachedInputCooldown = BattleInputSettings.GetInputCooldown();
            _cachedIsKeyboard = BattleInputSettings.IsKeyboardPreferred();
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
            _inputEnabled = false; // Explicitly disable until ready
            StartCoroutine(InitializeWhenReady());
            TurnrootLogger.Log(
                $"Battle started. PlayerTurnFlow state: {_playerTurnFlow?.GetCurrentState()}"
            );
        }

        private IEnumerator InitializeWhenReady()
        {
            // Wait for battle context and map grid
            int waitCount = 0;
            while (_brain?.battleBrain?.BattleObject?.Context?.mapGrid == null)
            {
                waitCount++;
                yield return new WaitForSeconds(0.05f);
            }

            // Wait for cursor brain to be initialized
            waitCount = 0;
            while (_brain?.cursorBrain?.IsInitialized != true)
            {
                waitCount++;
                yield return new WaitForSeconds(0.05f);
            }

            _playerTurnFlow = _brain.battleBrain.playerTurnFlow;
            SetupInputActions();

            // Wait one more frame to ensure everything is settled
            yield return null;

            // Now enable input processing
            _inputEnabled = true;
            _lastInputTime = Time.time; // Reset cooldown timer

            _tileHighlighter = _brain.battleBrain.BattleObject.GetComponent<TileHighlighter>();
        }

        private void HandleBattleCompleted(BattleExitType exitType) => CleanupInputActions();

        #endregion

        #region Input Event Handlers

        private void HandleNavigateEvent(BattleContext.BattleInputNavigateEvent e) =>
            HandleNavigateInput(e.Direction);

        private void HandleConfirmEvent(BattleContext.BattleInputConfirmEvent e) =>
            HandleConfirmInput();

        private void HandleCancelEvent(BattleContext.BattleInputCancelEvent e) =>
            HandleCancelInput();

        private void HandleMenuEvent(BattleContext.BattleInputMenuEvent e) => OpenMenu();

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

            // Round direction to discrete unit steps for cursor navigation (cardinal)
            direction = SnapDirectionToFour(direction);

            var currentState = _playerTurnFlow?.GetCurrentState();

            // Delegate cursor movement to CursorBrain
            _brain.cursorBrain.NavigateCursor(direction);

            // TODO: Update UI based on cursor position
            switch (currentState)
            {
                case PlayerTurnStates.MoveActionChosenChoosingDestination:
                    // Update movement path preview
                    break;
                case PlayerTurnStates.AttackActionChosenChoosingTarget:
                    // Update damage preview
                    break;
            }
        }

        public void HandleConfirmInput()
        {
            TurnrootLogger.Log("BattleInputControllerBrain: Handling Confirm Input");
            var currentState = _playerTurnFlow?.GetCurrentState() ?? PlayerTurnStates.Inactive;
            TurnrootLogger.Log(
                $"BattleInputControllerBrain: Current PlayerTurnState is {currentState}"
            );

            switch (currentState)
            {
                case PlayerTurnStates.NoUnitSelected:
                    // Eventually: OpenActionMenu();
                    var unitAtCursor = _brain.cursorBrain.GetUnitAtCursor();
                    if (unitAtCursor != null && BattleContext.IsPlayerControlledUnit(unitAtCursor))
                    {
                        _playerTurnFlow.SelectUnit();
                        ChangeSelectedUnit(unitAtCursor);
                    }
                    break;
                case PlayerTurnStates.NoActionChosen:
                    // TODO: Open context-sensitive action menu
                    break;
                case PlayerTurnStates.MoveActionChosenChoosingDestination:
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
                case PlayerTurnStates.NoActionChosen:
                    _playerTurnFlow?.DeselectUnit();
                    break;
                case PlayerTurnStates.MoveActionChosenChoosingDestination:
                    _playerTurnFlow?.CancelTargetOrDestinationChoice(
                        PlayerTurnStates.NoActionChosen
                    );
                    _brain.cursorBrain?.ClearAllowedPositions();
                    break;
                case PlayerTurnStates.AttackActionChosenChoosingTarget:
                    _playerTurnFlow?.CancelTargetOrDestinationChoice(
                        PlayerTurnStates.NoActionChosen
                    );
                    _brain.cursorBrain?.ClearAllowedPositions();
                    break;
                case PlayerTurnStates.ConfirmAction:
                    RequestUndo();
                    break;
            }
        }

        #endregion
    }
}
