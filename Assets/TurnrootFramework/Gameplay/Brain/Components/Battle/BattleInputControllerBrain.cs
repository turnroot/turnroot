using System.Collections;
using System.Collections.Generic;
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
    /// </summary>
    // TODO: Add advanced input features (buffering, accessibility, custom mappings, replay)
    public partial class BattleInputControllerBrain : BrainComponent
    {
        #region Properties

        [HideInInspector]
        public MapGridPoint CursorPosition; // TODO: Initialize with constraints

        [HideInInspector]
        public MapGridPoint PotentialCursorPosition; // TODO: Use for preview effects

        public CharacterInstance SelectedUnit =>
            _brain.battleBrain.BattleObject.Context.Unit.UnitInstance;
        public BattleContext BattleContext => _brain.battleBrain.BattleObject.Context;

        #endregion

        #region Fields

        private PlayerTurnFlow _playerTurnFlow;
        private BattleContextAIHelper _aiHelper;

        // Cached data for current player unit
        private Dictionary<MapGridPoint, float> _validMoveTiles = new();
        private Dictionary<MapGridPoint, float> _validAttackTiles = new();

        // TODO: Add caching for all action types, movement costs, and performance optimization

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
            UpdateInputCooldown();
        }

        private void Update()
        {
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

        private void HandleBattleStarted() => StartCoroutine(InitializeWhenReady());

        private IEnumerator InitializeWhenReady()
        {
            while (_brain?.battleBrain?.BattleObject?.Context?.mapGrid == null)
            {
                yield return new WaitForSeconds(0.05f);
            }

            _playerTurnFlow = _brain.battleBrain.playerTurnFlow;
            SetupInputActions();
            InitializeCursor();
        }

        private void HandleBattleCompleted(BattleExitType exitType) => CleanupInputActions();

        private void InitializeCursor()
        {
            if (_brain?.battleBrain?.BattleObject?.Context?.mapGrid == null)
            {
                StartCoroutine(RetryInitializeCursor());
                return;
            }

            var battleContext = _brain.battleBrain.BattleObject.Context;
            var neutralCentralPoint = _brain.cameraBrain.SetBattleGridCameraNeutralCenter();

            CursorPosition = battleContext.mapGrid.GetGridPoint(
                neutralCentralPoint.x,
                neutralCentralPoint.y
            );
            Brain.PublishBattleCursorMoved(CursorPosition.CoordinatesInt);

            // TODO: Set this to the correct unit based on gameplay settings and unit positions
        }

        private IEnumerator RetryInitializeCursor()
        {
            yield return new WaitForSeconds(0.1f);
            InitializeCursor();
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
            if (
                direction.magnitude < 0.1f
                || CursorPosition == null
                || _brain?.battleBrain?.BattleObject?.Context?.mapGrid == null
            )
            {
                return;
            }

            var currentState = _playerTurnFlow?.GetCurrentState();
            if (
                currentState is not PlayerTurnStates.NoUnitSelected
                and not PlayerTurnStates.Inactive
            )
            {
                return;
            }

            // TODO: Navigation behavior depends on current battle state
            // MoveActionChosenChoosingDestination: Navigate valid movement tiles with path preview
            // AttackActionChosenChoosingTarget: Navigate valid attack targets with damage preview
            // MenuOpen: Navigate menu options

            var battleContext = _brain.battleBrain.BattleObject.Context;
            var mapGrid = battleContext.mapGrid;
            var gridMovement = GetGridMovementFromDirection(direction);

            if (gridMovement == Vector2Int.zero)
            {
                return;
            }

            var targetPos = CursorPosition.CoordinatesInt + gridMovement;

            if (IsPositionWithinTraversableArea(targetPos, mapGrid))
            {
                var newCursorPos = mapGrid.GetGridPoint(targetPos.x, targetPos.y);
                if (newCursorPos != null)
                {
                    CursorPosition = newCursorPos;
                    _brain?.PublishBattleCursorMoved(CursorPosition.CoordinatesInt);
                }
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
                    // TODO: Play confirmation sound and start animation
                    break;
                // TODO: Handle all other action confirmation states
            }
        }

        public void HandleCancelInput()
        {
            var currentState = _playerTurnFlow?.GetCurrentState() ?? PlayerTurnStates.Inactive;

            switch (currentState)
            {
                case PlayerTurnStates.NoActionChosen:
                    _playerTurnFlow?.DeselectUnit();
                    // TODO: Play cancel sound
                    break;
                case PlayerTurnStates.MoveActionChosenChoosingDestination:
                    _playerTurnFlow?.CancelTargetOrDestinationChoice(
                        PlayerTurnStates.NoActionChosen
                    );
                    // TODO: Clear movement visualization
                    break;
                case PlayerTurnStates.AttackActionChosenChoosingTarget:
                    _playerTurnFlow?.CancelTargetOrDestinationChoice(
                        PlayerTurnStates.NoActionChosen
                    );
                    // TODO: Clear attack visualization and damage preview
                    break;
                case PlayerTurnStates.ConfirmAction:
                    RequestUndo();
                    // TODO: Clear action preview
                    break;
                // TODO: Handle all other action cancellation states
            }
        }

        #endregion

        #region Player Turn Management

        private void HandlePlayerUnitActivated(CharacterInstance unit)
        {
            CalculateValidTiles(unit);

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

            // TODO: UI updates for each turn phase (cursor, previews, range indicators)
            switch (newState)
            {
                case PlayerTurnStates.NoUnitSelected:
                    _validMoveTiles.Clear();
                    _validAttackTiles.Clear();
                    break;
                case PlayerTurnStates.MoveActionChosenChoosingDestination:
                case PlayerTurnStates.AttackActionChosenChoosingTarget:
                    break;
                case PlayerTurnStates.TurnEnded:
                    CompletePlayerTurn();
                    break;
                // TODO: Complete state handling
            }
        }

        private void CompletePlayerTurn()
        {
            _validMoveTiles.Clear();
            _validAttackTiles.Clear();
            _brain.PublishPlayerTurnEnded();
            _playerTurnFlow?.EndTurn();
        }

        #endregion

        #region Validation

        // TODO: Action confirmation flow (priorities.md 4.3) - BuildCommand, ExecutePreview, Snapshot/Restore, undo tracking

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

        public void MoveCursorToPoint(MapGridPoint point) => CursorPosition = point;

        // TODO: Cursor UI updates (visuals, sound, previews, constraints)

        public void ConfirmTileSelection()
        {
            if (!ValidateTileSelection(CursorPosition))
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
                    // TODO: Movement visualization
                    break;
                case PlayerTurnStates.AttackActionChosenChoosingTarget:
                    var targetUnit = GetUnitAtPosition(CursorPosition);
                    if (ValidateTargetSelection(targetUnit))
                    {
                        _playerTurnFlow.SelectTargetOrDestination(
                            PlayerTurnStates.AttackActionChosenTargetSelected
                        );
                        // TODO: Attack preview UI
                    }
                    break;
                // TODO: Cases for other action confirmations
            }
        }

        public void ChangeSelectedUnit(CharacterInstance unit)
        {
            // TODO: Validate player control, update flow, recalculate tiles, update UI
        }

        // TODO: Special battle actions (Wait, Item, Trade, Rescue/Drop, Talk, Steal, Dance/Refresh, Canto movement)
        // TODO: Advanced input validation (range, teams, weapons, action points, error feedback)

        public void OpenActionMenu() => _playerTurnFlow?.SelectUnit();

        public void RequestUndo() => _brain?.PublishPlayerUndoAction();

        public void OpenMenu()
        {
            // TODO: Battle pause menu (settings, speed, animation toggles, save/resume, battle info)
        }

        // TODO: Advanced input features (buffering, platform-specific controls, recording/replay, accessibility)

        #endregion
    }
}
