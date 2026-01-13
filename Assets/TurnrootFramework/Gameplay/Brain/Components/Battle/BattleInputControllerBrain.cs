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
    public partial class BattleInputControllerBrain : BrainComponent
    {
        #region Properties

        public CharacterInstance SelectedUnit =>
            _brain.battleBrain.BattleObject.Context.Unit.UnitInstance;
        public BattleContext BattleContext => _brain.battleBrain.BattleObject.Context;
        public MapGridPoint CursorPosition => _brain.cursorBrain?.CursorPosition;

        #endregion

        #region Fields

        private PlayerTurnFlow _playerTurnFlow;
        private BattleContextAIHelper _aiHelper;

        private Dictionary<MapGridPoint, float> _validMoveTiles = new();
        private Dictionary<MapGridPoint, float> _validAttackTiles = new();

        private InputAction _navigateAction;
        private InputAction _confirmAction;
        private InputAction _cancelAction;
        private InputAction _menuAction;

        private float _lastInputTime;
        private float _cachedInputCooldown;
        private bool _cachedIsKeyboard = true;
        private const float KEYBOARD_BASE_COOLDOWN = 0.1f;
        private const float GAMEPAD_COOLDOWN = 0.15f;

        // Add flag to prevent input processing before ready
        private bool _inputEnabled = false;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
#if UNITY_EDITOR
            Debug.Log("BattleInputController: Awake called");
#endif
            _playerTurnFlow = _brain?.battleBrain?.playerTurnFlow;
            _lastInputTime = -999f;
            UpdateInputCooldown();
        }

        private void Start()
        {
#if UNITY_EDITOR
            Debug.Log(
                $"BattleInputController: Start called. Brain: {(_brain != null ? "exists" : "null")}"
            );
#endif
            // Check if we're already in battle state (can happen if component loads late)
            if (_brain?.stateBrain?.CurrentState?.Name == BrainStateNames.Battle)
            {
#if UNITY_EDITOR
                Debug.Log("BattleInputController: Already in Battle state, initializing now");
#endif
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
#if UNITY_EDITOR
            Debug.Log("BattleInputController: SubscribeToBrainEvents called");
#endif
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

            _brain.OnBattleStarted += HandleBattleStarted;
            _brain.OnBattleCompleted += HandleBattleCompleted;
            _brain.OnPlayerControlledUnitActivated += HandlePlayerUnitActivated;
            _brain.OnPlayerTurnStateChanged += HandlePlayerTurnStateChanged;

#if UNITY_EDITOR
            Debug.Log("BattleInputController: Event subscriptions complete");
#endif
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
#if UNITY_EDITOR
                    Debug.Log($"BattleInputController: Processing navigate input: {direction}");
#endif
                    HandleNavigateInput(direction);
                    _brain?.Publish(
                        new BattleContext.BattleInputNavigateEvent { Direction = direction }
                    );
                    return true;
                }
            }

            if (_confirmAction?.WasPressedThisFrame() == true)
            {
#if UNITY_EDITOR
                Debug.Log("BattleInputController: Confirm pressed");
#endif
                _brain?.Publish(new BattleContext.BattleInputConfirmEvent());
                return true;
            }

            if (_cancelAction?.WasPressedThisFrame() == true)
            {
#if UNITY_EDITOR
                Debug.Log("BattleInputController: Cancel pressed");
#endif
                _brain?.Publish(new BattleContext.BattleInputCancelEvent());
                return true;
            }

            if (_menuAction?.WasPressedThisFrame() == true)
            {
#if UNITY_EDITOR
                Debug.Log("BattleInputController: Menu pressed");
#endif
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

#if UNITY_EDITOR
            Debug.Log("BattleInputController: Input actions created and enabled");
#endif
        }

        private InputAction CreateNavigateAction()
        {
            var action = new InputAction("Navigate", InputActionType.Value);

            action
                .AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            action
                .AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");

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
            _inputEnabled = false;

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

#if UNITY_EDITOR
            Debug.Log("BattleInputController: Input actions cleaned up");
#endif
        }

        #endregion

        #region Battle Lifecycle Event Handlers

        private void HandleBattleStarted()
        {
#if UNITY_EDITOR
            Debug.Log("BattleInputController: HandleBattleStarted called!");
#endif
            _lastInputTime = Time.time;
            _inputEnabled = false; // Explicitly disable until ready
            StartCoroutine(InitializeWhenReady());
        }

        private IEnumerator InitializeWhenReady()
        {
#if UNITY_EDITOR
            Debug.Log("BattleInputController: InitializeWhenReady coroutine started");
#endif

            // Wait for battle context and map grid
            int waitCount = 0;
            while (_brain?.battleBrain?.BattleObject?.Context?.mapGrid == null)
            {
                waitCount++;
#if UNITY_EDITOR
                if (waitCount % 20 == 0) // Log every second
                {
                    Debug.Log(
                        $"BattleInputController: Still waiting for battle context... ({waitCount * 0.05f}s)"
                    );
                }
#endif
                yield return new WaitForSeconds(0.05f);
            }

#if UNITY_EDITOR
            Debug.Log("BattleInputController: Battle context ready");
#endif

            // Wait for cursor brain to be initialized
            waitCount = 0;
            while (_brain?.cursorBrain?.IsInitialized != true)
            {
                waitCount++;
#if UNITY_EDITOR
                // Check every single iteration for first few, then every 20
                if (waitCount <= 5 || waitCount % 20 == 0)
                {
                    Debug.Log(
                        $"BattleInputController: Still waiting for cursor brain... (attempt {waitCount}, {waitCount * 0.05f}s)"
                    );
                    Debug.Log($"  - _brain exists: {_brain != null}");
                    Debug.Log($"  - _brain.cursorBrain exists: {_brain?.cursorBrain != null}");
                    Debug.Log(
                        $"  - _brain.cursorBrain.IsInitialized: {_brain?.cursorBrain?.IsInitialized}"
                    );
                    Debug.Log(
                        $"  - _brain.cursorBrain.CursorPosition: {_brain?.cursorBrain?.CursorPosition?.CoordinatesInt}"
                    );

                    // Also check if we're checking the right reference
                    if (_brain?.cursorBrain != null)
                    {
                        Debug.Log($"  - CursorBrain instance: {_brain.cursorBrain.GetHashCode()}");
                    }
                }
#endif
                yield return new WaitForSeconds(0.05f);
            }

#if UNITY_EDITOR
            Debug.Log("BattleInputController: Cursor brain ready");
#endif

            _playerTurnFlow = _brain.battleBrain.playerTurnFlow;
            SetupInputActions();

            // Wait one more frame to ensure everything is settled
            yield return null;

            // Now enable input processing
            _inputEnabled = true;
            _lastInputTime = Time.time; // Reset cooldown timer
#if UNITY_EDITOR
            Debug.Log("BattleInputController: Initialization complete - INPUT ENABLED!");
#endif
        }

        private void HandleBattleCompleted(BattleExitType exitType)
        {
            CleanupInputActions();
        }

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
            if (direction.magnitude < 0.1f)
            {
                Debug.LogWarning("BattleInputController: Ignoring negligible navigate input");
                return;
            }

            if (_brain?.cursorBrain == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("BattleInputController: CursorBrain is null, cannot navigate");
#endif
                return;
            }

            if (!_brain.cursorBrain.IsInitialized)
            {
#if UNITY_EDITOR
                Debug.LogWarning("BattleInputController: CursorBrain not initialized yet");
#endif
                return;
            }

            var currentState = _playerTurnFlow?.GetCurrentState();
            Debug.Log(
                $"BattleInputController: Navigate input received in state {currentState}, direction: {direction}"
            );

            // Delegate cursor movement to CursorBrain
            bool moved = _brain.cursorBrain.NavigateCursor(direction);

#if UNITY_EDITOR
            Debug.Log($"BattleInputController: Cursor navigation attempted, result: {moved}");
#endif

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
            var currentState = _playerTurnFlow?.GetCurrentState() ?? PlayerTurnStates.Inactive;

            Debug.Log($"BattleInputController: Confirm input in state {currentState}");

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

        #region Player Turn Management

        private void HandlePlayerUnitActivated(CharacterInstance unit)
        {
            ComputeValidTiles(unit);

#if UNITY_EDITOR
            Debug.Log(
                $"BattleInputController: Player unit activated - {unit.CharacterTemplate.DisplayName}"
            );
#endif
        }

        private void HandlePlayerTurnStateChanged(PlayerTurnStates newState)
        {
#if UNITY_EDITOR
            Debug.Log($"BattleInputController: Player turn state changed to {newState}");
#endif

            switch (newState)
            {
                case PlayerTurnStates.NoUnitSelected:
                    _validMoveTiles.Clear();
                    _validAttackTiles.Clear();
                    _brain.cursorBrain?.ClearAllowedPositions();
                    break;

                case PlayerTurnStates.MoveActionChosenChoosingDestination:
                    var movePositions = new List<Vector2Int>(
                        _validMoveTiles.Keys.Select(k => k.CoordinatesInt)
                    );
                    _brain.cursorBrain?.SetAllowedPositions(movePositions);
                    break;

                case PlayerTurnStates.AttackActionChosenChoosingTarget:
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
                // TODO: Error feedback
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
                    $"BattleInputController: Failed to calculate tiles for {unit.CharacterTemplate.DisplayName}"
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
