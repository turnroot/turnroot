using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components.Battle;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Gameplay.Combat;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.PlayerSettings;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Handles player input during battle phases and translates input events
    /// into player turn actions through the Brain event system.
    /// Delegates cursor management to CursorBrain for decoupled state handling.
    /// </summary>
    public partial class BattleInputControllerBrain : BrainComponent
    {
        #region Properties

        public CharacterInstance SelectedUnit =>
            _brain.battleBrain.BattleObject.Context.Unit.UnitInstance;
        public BattleContext BattleContext => _brain.battleBrain.BattleObject.Context;

        // Cursor position now comes from CursorBrain
        public MapGridPoint CursorPosition => _brain.cursorBrain?.CursorPosition;

        #endregion

        #region Fields

        private PlayerTurnFlow _playerTurnFlow;
        private BattleContextAIHelper _aiHelper;

        // Cached data for current player unit
        private Dictionary<MapGridPoint, float> _validMoveTiles = new();
        private Dictionary<MapGridPoint, float> _validAttackTiles = new();

        // Input system
        private InputAction _navigateAction;
        private InputAction _confirmAction;
        private InputAction _cancelAction;
        private InputAction _menuAction;

        // Input cooldown management
        private float _lastInputTime;
        private float _cachedInputCooldown;
        private bool _cachedIsKeyboard = true;
        private const float KEYBOARD_BASE_COOLDOWN = 0.1f;
        private const float GAMEPAD_COOLDOWN = 0.15f;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            _playerTurnFlow = _brain?.battleBrain?.playerTurnFlow;
            // Initialize input timer to a sentinel so early Update logs aren't misleading
            _lastInputTime = -999f;
            UpdateInputCooldown();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                Debug.Log(
                    $"RAW INPUT DETECTED: W/Up pressed. _navigateAction: {(_navigateAction != null ? "exists" : "null")}, enabled: {_navigateAction?.enabled}"
                );
                Debug.Log(
                    $"RAW INPUT. cooldown remaining: {_cachedInputCooldown - (Time.time - _lastInputTime)}"
                );
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
            // Input events
            _brain.Subscribe<BattleContext.BattleInputNavigateEvent>(
                HandleNavigateEvent,
                EventPriority.High
            );
            _brain.Subscribe<BattleContext.BattleInputConfirmEvent>(
                HandleConfirmEvent,
                EventPriority.High
            );
            _brain.Subscribe<BattleContext.BattleInputCancelEvent>(
                HandleCancelEvent,
                EventPriority.High
            );
            _brain.Subscribe<BattleContext.BattleInputMenuEvent>(
                HandleMenuEvent,
                EventPriority.High
            );

            // Battle lifecycle events
            _brain.OnBattleStarted += HandleBattleStarted;
            _brain.OnBattleCompleted += HandleBattleCompleted;

            // Player turn events
            _brain.OnPlayerControlledUnitActivated += HandlePlayerUnitActivated;
            _brain.OnPlayerTurnStateChanged += HandlePlayerTurnStateChanged;
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            _brain.Unsubscribe<BattleContext.BattleInputNavigateEvent>(HandleNavigateEvent);
            _brain.Unsubscribe<BattleContext.BattleInputConfirmEvent>(HandleConfirmEvent);
            _brain.Unsubscribe<BattleContext.BattleInputCancelEvent>(HandleCancelEvent);
            _brain.Unsubscribe<BattleContext.BattleInputMenuEvent>(HandleMenuEvent);

            _brain.OnBattleStarted -= HandleBattleStarted;
            _brain.OnBattleCompleted -= HandleBattleCompleted;
            _brain.OnPlayerControlledUnitActivated -= HandlePlayerUnitActivated;
            _brain.OnPlayerTurnStateChanged -= HandlePlayerTurnStateChanged;
        }

        #endregion

        #region Input Processing

        private bool ProcessInput()
        {
            if (_navigateAction?.enabled == true)
            {
                var direction = _navigateAction.ReadValue<Vector2>();
                if (direction.magnitude > 0.1f)
                {
                    HandleNavigateInput(direction);
                    _brain?.Publish(
                        new BattleContext.BattleInputNavigateEvent { Direction = direction }
                    );
                    return true;
                }
            }

            if (_confirmAction?.WasPressedThisFrame() == true)
            {
                _brain?.Publish(new BattleContext.BattleInputConfirmEvent());
                return true;
            }

            if (_cancelAction?.WasPressedThisFrame() == true)
            {
                _brain?.Publish(new BattleContext.BattleInputCancelEvent());
                return true;
            }

            if (_menuAction?.WasPressedThisFrame() == true)
            {
                _brain?.Publish(new BattleContext.BattleInputMenuEvent());
                return true;
            }

            return false;
        }

        private void UpdateInputCooldown()
        {
            var settings = LoadPlayerSettings();
            if (settings == null)
            {
                SetDefaultInputSettings();
                return;
            }

            _cachedIsKeyboard =
                settings.PreferredInputControl == GameplayPlayerSettings.InputControlType.Keyboard;
            _cachedInputCooldown = _cachedIsKeyboard
                ? GetKeyboardCooldown(settings.SpeedSetting)
                : GAMEPAD_COOLDOWN;
        }

        private float GetKeyboardCooldown(GameplayPlayerSettings.GameSpeed speed)
        {
            return speed switch
            {
                GameplayPlayerSettings.GameSpeed.Fast => 0.09f,
                GameplayPlayerSettings.GameSpeed.VeryFast => 0.08f,
                _ => KEYBOARD_BASE_COOLDOWN,
            };
        }

        private GameplayPlayerSettings LoadPlayerSettings()
        {
            try
            {
                return GameSettingsLoader.LoadFirst<GameplayPlayerSettings>("GameSettings");
            }
            catch (System.Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    $"BattleInputControllerBrain: Error loading player settings: {ex.Message}"
                );
#endif
                return null;
            }
        }

        private void SetDefaultInputSettings()
        {
            _cachedInputCooldown = KEYBOARD_BASE_COOLDOWN;
            _cachedIsKeyboard = true;
        }

        #endregion

        #region Input Setup & Cleanup

        private void SetupInputActions()
        {
            _navigateAction = CreateNavigateAction();
            _confirmAction = CreateConfirmAction();
            _cancelAction = CreateCancelAction();
            _menuAction = CreateMenuAction();

            _navigateAction.Enable();
            _confirmAction.Enable();
            _cancelAction.Enable();
            _menuAction.Enable();
        }

        private InputAction CreateNavigateAction()
        {
            var action = new InputAction("Navigate", InputActionType.Value);

            // WASD composite
            action
                .AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            // Arrow keys composite
            action
                .AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");

            // Gamepad
            action.AddBinding("<Gamepad>/leftStick");
            action.AddBinding("<Gamepad>/dpad");

            return action;
        }

        private InputAction CreateConfirmAction()
        {
            var action = new InputAction(
                "Confirm",
                InputActionType.Button,
                "<Gamepad>/buttonSouth"
            );
            action.AddBinding("<Keyboard>/enter");
            action.AddBinding("<Keyboard>/space");
            return action;
        }

        private InputAction CreateCancelAction()
        {
            var action = new InputAction("Cancel", InputActionType.Button, "<Gamepad>/buttonEast");
            action.AddBinding("<Keyboard>/escape");
            return action;
        }

        private InputAction CreateMenuAction()
        {
            var action = new InputAction("Menu", InputActionType.Button, "<Gamepad>/start");
            action.AddBinding("<Keyboard>/tab");
            return action;
        }

        private void CleanupInputActions()
        {
            _navigateAction?.Disable();
            _confirmAction?.Disable();
            _cancelAction?.Disable();
            _menuAction?.Disable();

            _navigateAction?.Dispose();
            _confirmAction?.Dispose();
            _cancelAction?.Dispose();
            _menuAction?.Dispose();

            _navigateAction = null;
            _confirmAction = null;
            _cancelAction = null;
            _menuAction = null;
        }

        #endregion

        #region Battle Lifecycle Event Handlers

        private void HandleBattleStarted()
        {
#if UNITY_EDITOR
            Debug.Log(
                "BattleInputControllerBrain.HandleBattleStarted called - initializing input timer"
            );
#endif
            // Use current time to avoid misleading negative cooldown log values and provide a clean start point
            _lastInputTime = Time.time;
            StartCoroutine(InitializeWhenReady());
        }

        private IEnumerator InitializeWhenReady()
        {
            while (_brain?.battleBrain?.BattleObject?.Context?.mapGrid == null)
            {
                yield return new WaitForSeconds(0.05f);
            }

            _playerTurnFlow = _brain.battleBrain.playerTurnFlow;
            SetupInputActions();

            // CursorBrain will initialize itself through its own event subscriptions
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
            if (direction.magnitude < 0.1f || _brain?.cursorBrain == null)
            {
                return;
            }

            var currentState = _playerTurnFlow?.GetCurrentState();

            // Only handle navigation in certain states
            if (currentState is PlayerTurnStates.Inactive)
            {
                return;
            }

            // Delegate cursor movement to CursorBrain
            _brain.cursorBrain.NavigateCursor(direction);

            // TODO: Update UI based on cursor position (damage preview, path preview, etc.)
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
            var currentState = _playerTurnFlow?.GetCurrentState() ?? PlayerTurnStates.Inactive;

            switch (currentState)
            {
                case PlayerTurnStates.NoUnitSelected:
                    OpenActionMenu();
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
                    // Clear movement restrictions from cursor
                    _brain.cursorBrain?.ClearAllowedPositions();
                    break;
                case PlayerTurnStates.AttackActionChosenChoosingTarget:
                    _playerTurnFlow?.CancelTargetOrDestinationChoice(
                        PlayerTurnStates.NoActionChosen
                    );
                    // Clear attack range restrictions from cursor
                    _brain.cursorBrain?.ClearAllowedPositions();
                    break;
                case PlayerTurnStates.ConfirmAction:
                    RequestUndo();
                    break;
            }
        }

        #endregion

        #region Player Turn Management

        private void HandlePlayerUnitActivated(CharacterInstance unit)
        {
            ComputeValidTiles(unit);

#if UNITY_EDITOR
            Debug.Log(
                $"BattleInputControllerBrain: Player unit activated - {unit.CharacterTemplate.DisplayName}"
            );
#endif
        }

        private void HandlePlayerTurnStateChanged(PlayerTurnStates newState)
        {
#if UNITY_EDITOR
            Debug.Log($"BattleInputControllerBrain: Player turn state changed to {newState}");
#endif

            // Update cursor restrictions based on state
            switch (newState)
            {
                case PlayerTurnStates.NoUnitSelected:
                    _validMoveTiles.Clear();
                    _validAttackTiles.Clear();
                    _brain.cursorBrain?.ClearAllowedPositions();
                    break;

                case PlayerTurnStates.MoveActionChosenChoosingDestination:
                    // Restrict cursor to valid movement tiles
                    var movePositions = new List<Vector2Int>(
                        _validMoveTiles.Keys.Select(k => k.CoordinatesInt)
                    );
                    _brain.cursorBrain?.SetAllowedPositions(movePositions);
                    break;

                case PlayerTurnStates.AttackActionChosenChoosingTarget:
                    // Restrict cursor to valid attack tiles
                    var attackPositions = new List<Vector2Int>(
                        _validAttackTiles.Keys.Select(k => k.CoordinatesInt)
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
            // TODO: Comprehensive action validation (weapons, skills, rescue/trade requirements, audio/visual feedback)
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
                // TODO: Error feedback for invalid selections
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
                    // Get unit at cursor using CursorBrain's helper
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
            // TODO: Validate player control, update flow, recalculate tiles, update UI
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

            var currentPos = unit.UnitPositionToMapGridPoint(
                unit.MapGridPosition,
                BattleContext.mapGrid
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
            }
            else
            {
                success = _aiHelper.GetTilesForAINonAlloc(
                    currentPos,
                    _validMoveTiles,
                    _validAttackTiles
                );
            }

            if (!success)
            {
#if UNITY_EDITOR
                Debug.LogError(
                    $"BattleInputControllerBrain: Failed to calculate tiles for {unit.CharacterTemplate.DisplayName}"
                );
#endif
                return OperationResult.Failure(
                    $"Failed to calculate tiles for unit {unit.CharacterTemplate.DisplayName}"
                );
            }

            return OperationResult.SuccessResult();
        }

        #endregion
    }
}
