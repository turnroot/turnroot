using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Manages player input during unit positioning phase before battle starts.
    /// </summary>
    public class PositioningInputController : BrainComponent
    {
        private BattleInputActions _inputActions;
        private bool _isActive = false;
        private float _lastInputTime;
        private float _cachedInputCooldown;
        private bool _cachedIsKeyboard = true;

        private Vector2 _lastDirection = Vector2.zero;

        protected override void Awake()
        {
            base.Awake();
            UpdateInputCooldown();
        }

        protected override void SubscribeToBrainEvents()
        {
            _brain.OnPositioningModeEntered += HandlePositioningModeEntered;
            _brain.OnPositioningModeExited += HandlePositioningModeExited;
            _brain.OnInputControlTypeChanged += HandleInputControlTypeChanged;
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            _brain.OnPositioningModeEntered -= HandlePositioningModeEntered;
            _brain.OnPositioningModeExited -= HandlePositioningModeExited;
            _brain.OnInputControlTypeChanged -= HandleInputControlTypeChanged;
        }

        private void HandlePositioningModeEntered()
        {
            UpdateInputCooldown();
            _inputActions = new BattleInputActions();
            _inputActions.Enable();
            _isActive = true;
            _lastInputTime = Time.time;
        }

        private void HandlePositioningModeExited()
        {
            _isActive = false;
            _inputActions.Disable();
            _inputActions.Dispose();
            _inputActions = null;
        }

        private void Update()
        {
            if (
                !_isActive
                || _inputActions == null
                || (Time.time - _lastInputTime < _cachedInputCooldown)
            )
            {
                return;
            }

            if (ProcessInput())
            {
                _lastInputTime = Time.time;
            }
        }

        private bool ProcessInput()
        {
            if (_inputActions.Navigate?.enabled == true)
            {
                var direction = _inputActions.Navigate.ReadValue<Vector2>();
                var threshold = GetInputThreshold();

                if (direction.magnitude > threshold)
                {
                    if (direction == _lastDirection)
                    {
                        return false;
                    }

                    _lastDirection = direction;

                    var navigated = TryNavigateDirection(direction);
                    if (navigated)
                    {
                        PreviewSwapIfNeeded();
                    }

                    return navigated;
                }

                _lastDirection = Vector2.zero;
            }

            if (_inputActions.Confirm?.WasPressedThisFrame() == true)
            {
                HandleConfirmInput();
                return true;
            }

            if (_inputActions.Cancel?.WasPressedThisFrame() == true)
            {
                HandleCancelInput();
                return true;
            }

            return false;
        }

        private float GetInputThreshold() =>
            Brain.gamewideContextBrain.PlayerSettings == null ? 0.3f
            : _cachedIsKeyboard ? 0.1f
            : 0.5f;

        private bool TryNavigateDirection(Vector2 direction)
        {
            if (Brain.cursorBrain == null)
            {
                return false;
            }

            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                int dir = direction.x > 0 ? 1 : -1;
                return Brain.cursorBrain.NavigateHorizontal(dir);
            }
            else
            {
                int dir = direction.y > 0 ? 1 : -1;
                return Brain.cursorBrain.NavigateVertical(dir);
            }
        }

        private void PreviewSwapIfNeeded()
        {
            var prepObject = Brain.battleBrain.PreparationObject;
            var cursorPos = Brain.cursorBrain.CursorPosition.CoordinatesInt;
            if (prepObject == null || prepObject.selectedPosition == null || cursorPos == null)
            {
                return;
            }

            _ = prepObject.PreviewPotentialSwap(cursorPos);
        }

        private void UpdateInputCooldown()
        {
            _cachedInputCooldown = InputSettingsHelper.GetInputCooldown();
            _cachedIsKeyboard = InputSettingsHelper.IsKeyboardPreferred();
        }

        private void HandleInputControlTypeChanged(
            PlayerSettings.GameplayPlayerSettings.InputControlType _
        ) => UpdateInputCooldown();

        private void HandleConfirmInput()
        {
            var prepObject = Brain.battleBrain.PreparationObject;
            if (prepObject == null)
            {
                return;
            }

            var cursorPos = Brain.cursorBrain.CursorPosition.CoordinatesInt;
            if (cursorPos == null)
            {
                return;
            }

            // First confirm: Select a unit
            if (prepObject.selectedPosition == null)
            {
                var result = prepObject.SelectPosition(cursorPos);
                // TODO: Add SFX if Result.Success
            }
            // Second confirm: Execute swap/move
            else
            {
                prepObject.potentialSwapPosition = cursorPos;
                var result = prepObject.ExecutePositionAction();
                // TODO: Add SFX if Result.Success
            }
        }

        private void HandleCancelInput()
        {
            var prepObject = Brain.battleBrain.PreparationObject;
            if (prepObject == null)
            {
                return;
            }

            // If unit is selected, deselect it
            if (prepObject.selectedPosition != null)
            {
                prepObject.ClearSelection();
            }
            // Otherwise, return to previous menu
        }

        protected override void OnDestroy()
        {
            if (_inputActions != null)
            {
                _inputActions.Disable();
                _inputActions.Dispose();
                _inputActions = null;
            }
            base.OnDestroy();
        }
    }
}
