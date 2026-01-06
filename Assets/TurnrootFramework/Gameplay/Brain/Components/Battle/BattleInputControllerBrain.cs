using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components.Battle;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Locations;
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

            _brain.OnPlayerControlledUnitActivated -= HandlePlayerUnitActivated;
            _brain.OnPlayerTurnStateChanged -= HandlePlayerTurnStateChanged;
        }

        protected override void Awake()
        {
            base.Awake();
            _playerTurnFlow = _brain?.battleBrain?.playerTurnFlow;

            // Initialize Unity Input System actions
            _navigateAction = new InputAction(
                "Navigate",
                InputActionType.Value,
                "<Gamepad>/leftStick"
            );
            _navigateAction.AddBinding("<Keyboard>/wasd");
            _navigateAction.AddBinding("<Keyboard>/upDownLeftRight");
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

            // TODO: Initialize cursor position, subscribe to map changes
        }

        private void Update()
        {
            // Process Unity Input System and publish Brain events
            if (_navigateAction?.WasPressedThisFrame() == true)
            {
                var direction = _navigateAction.ReadValue<Vector2>();
                _brain?.Publish(
                    new BattleContext.BattleInputNavigateEvent { Direction = direction }
                );
            }

            if (_confirmAction?.WasPressedThisFrame() == true)
            {
                _brain?.Publish(new BattleContext.BattleInputConfirmEvent());
            }

            if (_cancelAction?.WasPressedThisFrame() == true)
            {
                _brain?.Publish(new BattleContext.BattleInputCancelEvent());
            }

            if (_menuAction?.WasPressedThisFrame() == true)
            {
                _brain?.Publish(new BattleContext.BattleInputMenuEvent());
            }
        }

        protected override void OnDestroy()
        {
            // Clean up input actions
            _navigateAction?.Disable();
            _confirmAction?.Disable();
            _cancelAction?.Disable();
            _menuAction?.Disable();

            base.OnDestroy();
        }

        // Event handlers for input events from BattleContext
        private void HandleNavigateEvent(BattleContext.BattleInputNavigateEvent navEvent)
        {
            HandleNavigateInput(navEvent.Direction);
        }

        private void HandleConfirmEvent(BattleContext.BattleInputConfirmEvent confirmEvent)
        {
            HandleConfirmInput();
        }

        private void HandleCancelEvent(BattleContext.BattleInputCancelEvent cancelEvent)
        {
            HandleCancelInput();
        }

        private void HandleMenuEvent(BattleContext.BattleInputMenuEvent menuEvent)
        {
            OpenMenu();
        }

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

        private void CalculateValidTiles(CharacterInstance unit)
        {
            if (unit == null || BattleContext?.mapGrid == null)
            {
                return;
            }

            _validMoveTiles.Clear();
            _validAttackTiles.Clear();

            // Get AI helper for pathfinding calculations
            _aiHelper = BattleContext.GetCustomData<BattleContextAIHelper>("AIHelper");

            // TODO: Tile validation using AI helper (movement/attack ranges, terrain costs, skill modifiers, caching)
        }

        // TODO: Implement damage preview system (priorities.md Phase 4.1) - CalculateAttackPreview with hit%/crit%/counters
        // TODO: Implement movement path preview (priorities.md Phase 4.2) - CalculateMovementPath with A* pathfinding

        public bool ValidateTileSelection(MapGridPoint point)
        {
            // Check if the selected point is valid for current action
            var currentState = _playerTurnFlow?.GetCurrentState() ?? PlayerTurnStates.Inactive;

            switch (currentState)
            {
                case PlayerTurnStates.MoveActionChosenChoosingDestination:
                    return _validMoveTiles.ContainsKey(point);
                case PlayerTurnStates.AttackActionChosenChoosingTarget:
                    return _validAttackTiles.ContainsKey(point);
                default:
                    return false;
            }
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

        // Additional detailed damage/movement preview implementations already planned above

        public void MoveCursorToPoint(MapGridPoint point)
        {
            CursorPosition = point;
            // TODO: Cursor UI updates (visuals, sound, previews, constraints)
        }

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

        private CharacterInstance GetUnitAtPosition(MapGridPoint position)
        {
            // TODO: Search battle participants efficiently, check position.CurrentInstance
            return null;
        }

        public void ChangeSelectedUnit(CharacterInstance unit)
        {
            // TODO: Validate player control, update flow, recalculate tiles, update UI
        }

        // TODO: Special battle actions (Wait, Item, Trade, Rescue/Drop, Talk, Steal, Dance/Refresh, Canto movement)
        // TODO: Advanced input validation (range, teams, weapons, action points, error feedback)

        public void OpenActionMenu()
        {
            _playerTurnFlow?.SelectUnit();
        }

        public void RequestUndo()
        {
            _brain?.PublishPlayerUndoAction();
        }

        public void OpenMenu()
        {
            // TODO: Battle pause menu (settings, speed, animation toggles, save/resume, battle info)
        }

        // TODO: Advanced input features (buffering, platform-specific controls, recording/replay, accessibility)

        public void HandleNavigateInput(Vector2 direction)
        {
            if (direction.magnitude < 0.1f)
            {
                return;
            }

            // TODO: Navigation behavior depends on current battle state
            // NoUnitSelected: Move camera/cursor to select units
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
                    // TODO: Handle multiple unit selection if needed
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
    }
}
