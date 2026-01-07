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
    public class BattleInputControllerBrain : BrainComponent
    {
        #region Fields and Properties

        [HideInInspector]
        public MapGridPoint CursorPosition; // TODO: Initialize with constraints

        [HideInInspector]
        public MapGridPoint PotentialCursorPosition; // TODO: Use for preview effects

        [HideInInspector]
        public CharacterInstance SelectedUnit =>
            _brain.battleBrain.BattleObject.Context.Unit.UnitInstance;

        [HideInInspector]
        public BattleContext BattleContext => _brain.battleBrain.BattleObject.Context;

        private PlayerTurnFlow _playerTurnFlow;
        private BattleContextAIHelper _aiHelper;

        // Cached data for current player unit
        private Dictionary<MapGridPoint, float> _validMoveTiles = new();
        private Dictionary<MapGridPoint, float> _validAttackTiles = new();

        // TODO: Add caching for all action types, movement costs, and performance optimization

        // Unity Input System actions
        private InputAction _navigateAction;
        private InputAction _confirmAction;
        private InputAction _cancelAction;
        private InputAction _menuAction;

        private float _lastInputTime;
        private const float KEYBOARD_INPUT_COOLDOWN = 0.025f; // Faster for keyboard
        private const float GAMEPAD_INPUT_COOLDOWN = 0.1f; // Slower for gamepad

        // Cached cooldown to avoid recalculating every frame
        private float _cachedInputCooldown = KEYBOARD_INPUT_COOLDOWN;
        #endregion

        #region Brain Event Management

        protected override EventPriority GetSubscriptionPriority() => EventPriority.High;

        protected override void SubscribeToBrainEvents()
        {
            // Subscribe to battle input events from BattleContext
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

            // Subscribe to battle lifecycle events
            _brain.OnBattleStarted += HandleBattleStarted;
            _brain.OnBattleCompleted += HandleBattleCompleted;

            // Subscribe to player turn events
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

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            _playerTurnFlow = _brain?.battleBrain?.playerTurnFlow;

            // Initialize cached input cooldown
            UpdateCachedInputCooldown();
        }

        private void Update()
        {
            // Use cached cooldown instead of calculating every frame
            if (Time.time - _lastInputTime < _cachedInputCooldown)
            {
                return;
            }

            bool inputProcessed = false;

            // Process Unity Input System and publish Brain events
            if (_navigateAction?.WasPressedThisFrame() == true)
            {
                var direction = _navigateAction.ReadValue<Vector2>();
                HandleNavigateInput(direction);
                _brain?.Publish(
                    new BattleContext.BattleInputNavigateEvent { Direction = direction }
                );
                inputProcessed = true;
            }

            if (_confirmAction?.WasPressedThisFrame() == true)
            {
                _brain?.Publish(new BattleContext.BattleInputConfirmEvent());
                inputProcessed = true;
            }

            if (_cancelAction?.WasPressedThisFrame() == true)
            {
                _brain?.Publish(new BattleContext.BattleInputCancelEvent());
                inputProcessed = true;
            }

            if (_menuAction?.WasPressedThisFrame() == true)
            {
                _brain?.Publish(new BattleContext.BattleInputMenuEvent());
                inputProcessed = true;
            }

            if (inputProcessed)
            {
                _lastInputTime = Time.time;
            }
        }

        protected override void OnDestroy()
        {
            // Clean up input actions
            CleanupInputActions();

            base.OnDestroy();
        }

        #endregion

        #region Input Source Detection

        /// <summary>
        /// Updates the cached input cooldown based on player settings
        /// </summary>
        private void UpdateCachedInputCooldown()
        {
            try
            {
                var playerSettings = GameSettingsLoader.LoadFirst<GameplayPlayerSettings>(
                    "GameSettings"
                );
                if (playerSettings == null)
                {
                    _cachedInputCooldown = KEYBOARD_INPUT_COOLDOWN;
                    return;
                }

                switch (playerSettings.PreferredInputControl)
                {
                    case GameplayPlayerSettings.InputControlType.Keyboard:
                        _cachedInputCooldown = KEYBOARD_INPUT_COOLDOWN;
                        break;
                    case GameplayPlayerSettings.InputControlType.Gamepad:
                        _cachedInputCooldown = GAMEPAD_INPUT_COOLDOWN;
                        break;
                    default:
                        _cachedInputCooldown = KEYBOARD_INPUT_COOLDOWN;
                        break;
                }
            }
            catch (System.Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    $"BattleInputControllerBrain: Error loading player settings: {ex.Message}"
                );
#endif
                _cachedInputCooldown = KEYBOARD_INPUT_COOLDOWN;
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Initialize input actions and cursor when the battle starts - with retry logic for timing
        /// </summary>
        private void HandleBattleStarted()
        {
            // Start a coroutine to wait for battle context to be ready
            StartCoroutine(WaitForBattleContextAndInitialize());
        }

        /// <summary>
        /// Wait for battle context to be fully initialized, then setup input and cursor
        /// </summary>
        private System.Collections.IEnumerator WaitForBattleContextAndInitialize()
        {
            // Wait until the battle context is fully set up
            while (_brain?.battleBrain?.BattleObject?.Context?.mapGrid == null)
            {
                yield return new WaitForSeconds(0.05f); // Check every 50ms
            }

            // Capture references after the context is ready and validate them
            var brain = _brain;
            if (brain == null)
            {
                yield break;
            }

            var battleBrain = brain.battleBrain;
            if (battleBrain == null)
            {
                yield break;
            }

            // Try to update PlayerTurnFlow reference since it should be available now
            _playerTurnFlow = battleBrain.playerTurnFlow;
            // Now that battle context is ready, set up input actions and cursor
            SetupInputActions();
            InitializeCursor();
        }

        /// <summary>
        /// Setup and enable input actions for battle
        /// </summary>
        private void SetupInputActions()
        {
            // Create navigation action with 2D Vector composite for keyboard input
            _navigateAction = new InputAction("Navigate", InputActionType.Value);

            // Add WASD composite
            _navigateAction
                .AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            // Add Arrow Keys composite
            _navigateAction
                .AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");

            // Add gamepad bindings
            _navigateAction.AddBinding("<Gamepad>/leftStick");
            _navigateAction.AddBinding("<Gamepad>/dpad");

            _confirmAction = new InputAction(
                "Confirm",
                InputActionType.Button,
                "<Gamepad>/buttonSouth"
            );
            _confirmAction.AddBinding("<Keyboard>/enter");
            _confirmAction.AddBinding("<Keyboard>/space");

            _cancelAction = new InputAction(
                "Cancel",
                InputActionType.Button,
                "<Gamepad>/buttonEast"
            );
            _cancelAction.AddBinding("<Keyboard>/escape");

            _menuAction = new InputAction("Menu", InputActionType.Button, "<Gamepad>/start");
            _menuAction.AddBinding("<Keyboard>/tab");

            // Enable actions
            _navigateAction.Enable();
            _confirmAction.Enable();
            _cancelAction.Enable();
            _menuAction.Enable();
        }

        /// <summary>
        /// Clean up input actions when battle ends
        /// </summary>
        private void HandleBattleCompleted(BattleExitType exitType)
        {
            CleanupInputActions();
        }

        /// <summary>
        /// Disable and cleanup input actions
        /// </summary>
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

        /// <summary>
        /// Initialize cursor position and publish initial cursor moved event
        /// </summary>
        private void InitializeCursor()
        {
            // Check each part of the BattleContext chain for null
            if (_brain?.battleBrain?.BattleObject?.Context?.mapGrid != null)
            {
                var battleContext = _brain.battleBrain.BattleObject.Context;
                // Use CameraBrain to raycast from battle camera center, find closest map grid point,
                // and set cursor position there. Set this as Camera Center
                // Initialize cursor at origin (0,0) or first valid position
                // After initializing, we can move the cursor to the correct unit, but we must have
                // this camera center
                var neutralCentralPoint = _brain.cameraBrain.SetBattleGridCameraNeutralCenter();

                CursorPosition = battleContext.mapGrid.GetGridPoint(
                    neutralCentralPoint.x,
                    neutralCentralPoint.y
                );

                // TODO: Set this to the correct unit based on gameplay settings and unit positions

                // Now it's safe to publish the cursor moved event
                Brain.PublishBattleCursorMoved(CursorPosition.CoordinatesInt);
            }
            else
            {
                // Retry initialization after a short delay
                StartCoroutine(RetryInitializeCursorAfterDelay());
            }
        }

        /// <summary>
        /// Retry cursor initialization after a short delay to allow BattleContext to be fully set up
        /// </summary>
        private System.Collections.IEnumerator RetryInitializeCursorAfterDelay()
        {
            yield return new WaitForSeconds(0.1f); // Wait 100ms for battle context to be set up
            InitializeCursor(); // Try again
        }

        // Event handlers for input events from BattleContext
        private void HandleNavigateEvent(BattleContext.BattleInputNavigateEvent navEvent) =>
            HandleNavigateInput(navEvent.Direction);

        private void HandleConfirmEvent(BattleContext.BattleInputConfirmEvent confirmEvent) =>
            HandleConfirmInput();

        private void HandleCancelEvent(BattleContext.BattleInputCancelEvent cancelEvent) =>
            HandleCancelInput();

        private void HandleMenuEvent(BattleContext.BattleInputMenuEvent menuEvent) => OpenMenu();

        #endregion

        #region Player Turn Management

        // Player turn event handlers
        private void HandlePlayerUnitActivated(CharacterInstance unit)
        {
            // Calculate valid tiles for this unit
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
            // Update UI/cursor behavior based on state
            switch (newState)
            {
                case PlayerTurnStates.NoUnitSelected:
                    // Clear cached data
                    _validMoveTiles.Clear();
                    _validAttackTiles.Clear();
                    // TODO: UI updates for each turn phase (cursor, previews, range indicators)
                    break;
                case PlayerTurnStates.MoveActionChosenChoosingDestination:
                    break;
                case PlayerTurnStates.AttackActionChosenChoosingTarget:
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

        #region Tile Calculation and Validation

        private OperationResult CalculateValidTiles(CharacterInstance unit)
        {
            if (unit == null || BattleContext?.mapGrid == null)
            {
                return OperationResult.Failure("No unit or BattleContext");
            }

            _validMoveTiles.Clear();
            _validAttackTiles.Clear();

            _aiHelper = BattleContext.AIHelper;

            // Get the unit's current position
            var currentPos = unit.UnitPositionToMapGridPoint(
                unit.MapGridPosition,
                BattleContext.mapGrid
            );

            // Check if unit can heal (affects which method we call)
            bool canHeal = unit.CurrentClass?.ClassData?.Identity?.CanHeal ?? false;

            if (canHeal)
            {
                // For healers, we need movement, attack, AND heal ranges
                var healTilesTemp = new Dictionary<MapGridPoint, float>();

                bool success = _aiHelper.GetTilesForAIWithHealNonAlloc(
                    currentPos,
                    _validMoveTiles,
                    _validAttackTiles,
                    healTilesTemp
                );

                if (!success)
                {
#if UNITY_EDITOR
                    Debug.LogError(
                        $"BattleInputControllerBrain: Failed to calculate tiles for healer {unit.CharacterTemplate.DisplayName}"
                    );
#endif
                }
            }
            else
            {
                // For non-healers, just movement and attack ranges
                bool success = _aiHelper.GetTilesForAINonAlloc(
                    currentPos,
                    _validMoveTiles,
                    _validAttackTiles
                );
                if (!success)
                {
                    return OperationResult.Failure(
                        $"Failed to calculate tiles for unit {unit.CharacterTemplate.DisplayName}"
                    );
                }
            }
            return OperationResult.SuccessResult();
        }

        // TODO: Implement damage preview system (priorities.md Phase 4.1) - CalculateAttackPreview with hit%/crit%/counters
        // TODO: Implement movement path preview (priorities.md Phase 4.2) - CalculateMovementPath with A* pathfinding

        public bool ValidateTileSelection(MapGridPoint point)
        {
            // Check if the selected point is valid for current action
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

        // TODO: Action confirmation flow (priorities.md 4.3) - BuildCommand, ExecutePreview, Snapshot/Restore, undo tracking

        public bool ValidateTargetSelection(CharacterInstance target)
        {
            if (target == null)
            {
                return false;
            }

            var currentState = _playerTurnFlow?.GetCurrentState() ?? PlayerTurnStates.Inactive;

            switch (currentState)
            {
                case PlayerTurnStates.AttackActionChosenChoosingTarget:
                    // Check if target is enemy and in range
                    return BattleContext.IsTarget(target);
                case PlayerTurnStates.HealActionChosenChoosingTarget:
                    // Check if target is ally
                    return BattleContext.IsAlly(target);
                default:
                    return false;
            }
        }

        #endregion

        #region Action Methods

        // Additional detailed damage/movement preview implementations already planned above

        public void MoveCursorToPoint(MapGridPoint point) => CursorPosition = point; // TODO: Cursor UI updates (visuals, sound, previews, constraints)

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
                    // Find unit at cursor position
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

        #region Input Handling

        public void HandleNavigateInput(Vector2 direction)
        {
            if (direction.magnitude < 0.1f)
            {
                return;
            }

            // Safety check: ensure cursor and battle context are initialized
            if (
                CursorPosition == null
                || _brain?.battleBrain?.BattleObject?.Context?.mapGrid == null
            )
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    "BattleInputControllerBrain: Cannot navigate - cursor or battle context not initialized"
                );
                Debug.LogWarning(
                    $"BattleInputControllerBrain: CursorPosition null: {CursorPosition == null}, BattleContext chain incomplete: {_brain?.battleBrain?.BattleObject?.Context?.mapGrid == null}"
                );
#endif
                return;
            }

            var battleContext = _brain.battleBrain.BattleObject.Context;

            // TODO: Navigation behavior depends on current battle state
            // NoUnitSelected or Inactive: Move camera/cursor to select units
            bool isNoUnitSelectedOrInactiveState =
                _playerTurnFlow?.GetCurrentState() == PlayerTurnStates.NoUnitSelected
                || _playerTurnFlow?.GetCurrentState() == PlayerTurnStates.Inactive;

            if (isNoUnitSelectedOrInactiveState)
            {
                // Move the cursor on the grid based on input direction
                // If the cursor goes near the edge of the screen, pan the camera
                // Here, we  publish an event through the brain, PublishBattleCursorMoved
                // so the CameraBrain can worry about that.
                // UI will also take care of itself. Here it is JUST the logic of moving the cursor.
                // TODO: Setup CameraBrain
                // TODO: Setup cursor UI
                var newCursorPos = CursorPosition;
                // Get the MapGridPoint based on direction- BattleContext.mapGrid.GetGridPoint()
                // move one tile in the direction indicated by input
                // Note: Removed flip logic - mesh transformation should handle visual alignment
                var mapGrid = battleContext.mapGrid;

                if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                {
                    // Horizontal movement
                    bool moveRight = direction.x > 0;

                    if (moveRight)
                    {
                        var targetPos = CursorPosition.CoordinatesInt + Vector2Int.right;
                        newCursorPos = battleContext.mapGrid.GetGridPoint(targetPos.x, targetPos.y);
                    }
                    else
                    {
                        var targetPos = CursorPosition.CoordinatesInt + Vector2Int.left;
                        newCursorPos = battleContext.mapGrid.GetGridPoint(targetPos.x, targetPos.y);
                    }
                }
                else
                {
                    // Vertical movement
                    bool moveUp = direction.y > 0;

                    if (moveUp)
                    {
                        var targetPos = CursorPosition.CoordinatesInt + Vector2Int.up;
                        newCursorPos = battleContext.mapGrid.GetGridPoint(targetPos.x, targetPos.y);
                    }
                    else
                    {
                        var targetPos = CursorPosition.CoordinatesInt + Vector2Int.down;
                        newCursorPos = battleContext.mapGrid.GetGridPoint(targetPos.x, targetPos.y);
                    }
                }

                // Update cursor position if the new position is valid
                if (newCursorPos != null)
                {
                    CursorPosition = newCursorPos;
                    _brain?.PublishBattleCursorMoved(CursorPosition.CoordinatesInt);
                }
            }

            // MoveActionChosenChoosingDestination: Navigate valid movement tiles with path preview
            // AttackActionChosenChoosingTarget: Navigate valid attack targets with damage preview
            // MenuOpen: Navigate menu options
            // Convert direction to grid movement, validate bounds, update cursor position
            // Trigger appropriate preview systems based on current state
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
                    // Cancel preview, return to previous state
                    RequestUndo();
                    // TODO: Clear action preview
                    break;
                // TODO: Handle all other action cancellation states
            }
        }

        #endregion

        #region Utility Methods

        private CharacterInstance GetUnitAtPosition(MapGridPoint position)
        {
            var cache = BattleContext.GetCurrentUnitPositions();
            return cache.TryGetValue(position.CoordinatesInt, out var unit) ? unit : null;
        }

        #endregion
    }
}
