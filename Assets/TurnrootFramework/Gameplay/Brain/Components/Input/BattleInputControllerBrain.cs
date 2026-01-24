using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        private static Vector2 RotateVectorBy90StepsCW(Vector2 v, int steps)
        {
            // Normalize steps to 0..3
            steps = ((steps % 4) + 4) % 4;
            // Apply clockwise 90° rotation steps using integer math to avoid trig imprecision
            switch (steps)
            {
                case 0:
                    return v;
                case 1: // 90° clockwise: (x,y) -> (y, -x)
                    return new Vector2(v.y, -v.x);
                case 2: // 180°: (x,y) -> (-x, -y)
                    return new Vector2(-v.x, -v.y);
                case 3: // 270° clockwise (or 90° ccw): (x,y) -> (-y, x)
                    return new Vector2(-v.y, v.x);
                default:
                    return v;
            }
        }

        private static Vector2 SnapDirectionToFour(Vector2 v)
        {
            if (v.magnitude < 0.0001f)
                return Vector2.zero;
            var angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            // Snap to nearest 45 degrees (8 directions including diagonals)
            var snapped = Mathf.Round(angle / 45f) * 45f;
            var rad = snapped * Mathf.Deg2Rad;
            // Round cosine/sine to avoid floating point imprecision and yield exact integer direction vectors
            return new Vector2(Mathf.Round(Mathf.Cos(rad)), Mathf.Round(Mathf.Sin(rad)));
        }

        public void HandleConfirmInput()
        {
            var currentState = _playerTurnFlow?.GetCurrentState() ?? PlayerTurnStates.Inactive;

            switch (currentState)
            {
                case PlayerTurnStates.NoUnitSelected:
                    // Eventually: OpenActionMenu();
                    var unitAtCursor = _brain.cursorBrain.GetUnitAtCursor();
                    if (unitAtCursor != null && BattleContext.IsPlayerControlledUnit(unitAtCursor))
                    {
                        _playerTurnFlow.SelectUnit();
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

            return !success
                ? OperationResult.Failure(
                    $"Failed to calculate tiles for unit {unit.CharacterTemplate.DisplayName}"
                )
                : OperationResult.Successful();
        }

        #endregion
    }
}
