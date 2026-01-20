using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
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
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            _brain.OnPositioningModeEntered -= HandlePositioningModeEntered;
            _brain.OnPositioningModeExited -= HandlePositioningModeExited;
        }

        private void HandlePositioningModeEntered()
        {
            UpdateInputCooldown();
            _inputActions = new BattleInputActions(); // Reuse existing input actions
            _inputActions.Enable();
            _isActive = true;
            _lastInputTime = Time.time;

#if UNITY_EDITOR
            Debug.Log("PositioningInputController: Activated");
#endif
        }

        private void HandlePositioningModeExited()
        {
            _isActive = false;
            _inputActions?.Disable();
            _inputActions?.Dispose();
            _inputActions = null;

#if UNITY_EDITOR
            Debug.Log("PositioningInputController: Deactivated");
#endif
        }

        private void Update()
        {
            if (!_isActive || _inputActions == null)
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

        private bool ProcessInput()
        {
            // Navigation - CHANGED to use wrapping navigation for spawn points
            if (_inputActions?.Navigate?.enabled == true)
            {
                var direction = _inputActions.Navigate.ReadValue<Vector2>();
                float inputThreshold;
                if (Brain?.gamewideContextBrain?.PlayerSettings != null)
                {
                    inputThreshold =
                        Brain.gamewideContextBrain.PlayerSettings.PreferredInputControl
                        == PlayerSettings.GameplayPlayerSettings.InputControlType.Keyboard
                            ? 0.1f
                            : 0.5f;
                }
                else
                {
                    inputThreshold = 0.3f;
                }

                if (direction.magnitude > inputThreshold)
                {
                    // Prevent repeat inputs when stick/key is held
                    if (direction == _lastDirection)
                    {
                        return false;
                    }
                    _lastDirection = direction;

                    // CRITICAL FIX: Use NavigateWithWrapping for spawn points
                    // Determine primary direction (horizontal or vertical)
                    bool navigated = false;
                    if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                    {
                        // Horizontal navigation
                        int dir = direction.x > 0 ? 1 : -1;
                        navigated = _brain.cursorBrain?.NavigateWithWrapping(dir) ?? false;
#if UNITY_EDITOR
                        Debug.Log($"Positioning: Navigate horizontal {dir}, success={navigated}");
#endif
                    }
                    else
                    {
                        // Vertical navigation (treat as forward/backward in list)
                        int dir = direction.y > 0 ? 1 : -1;
                        navigated = _brain.cursorBrain?.NavigateWithWrapping(dir) ?? false;
#if UNITY_EDITOR
                        Debug.Log($"Positioning: Navigate vertical {dir}, success={navigated}");
#endif
                    }

                    // If navigation succeeded, preview swap
                    if (navigated)
                    {
                        var prepObject = _brain?.battleBrain?.PreparationObject;
                        var cursorPos = _brain.cursorBrain?.CursorPosition?.CoordinatesInt;
                        if (
                            prepObject != null
                            && prepObject.selectedPosition != null
                            && cursorPos != null
                        )
                        {
                            _ = prepObject.PreviewPotentialSwap(cursorPos.Value);
                        }
                    }

                    return navigated;
                }
                else
                {
                    // Input below threshold - reset direction tracking
                    _lastDirection = Vector2.zero;
                }
            }

            // Confirm - Select position / Execute swap
            if (_inputActions?.Confirm?.WasPressedThisFrame() == true)
            {
                HandleConfirmInput();
                return true;
            }

            // Cancel - Deselect / Return to menu
            if (_inputActions?.Cancel?.WasPressedThisFrame() == true)
            {
                HandleCancelInput();
                return true;
            }

            return false;
        }

        private void UpdateInputCooldown()
        {
            _cachedInputCooldown = BattleInputSettings.GetInputCooldown();
            _cachedIsKeyboard = BattleInputSettings.IsKeyboardPreferred();
        }

        private void HandleConfirmInput()
        {
            var prepObject = _brain?.battleBrain?.PreparationObject;
            if (prepObject == null)
            {
                return;
            }

            var cursorPos = _brain.cursorBrain?.CursorPosition?.CoordinatesInt;
            if (cursorPos == null)
            {
                return;
            }

            // First confirm: Select a unit
            if (prepObject.selectedPosition == null)
            {
                var result = prepObject.SelectPosition(cursorPos.Value);
                if (result.Success)
                {
#if UNITY_EDITOR
                    Debug.Log($"Selected unit at {cursorPos.Value}");
#endif
                    // TODO: Show visual feedback (highlight selected tile)
                }
                else
                {
#if UNITY_EDITOR
                    Debug.Log($"Cannot select position: {result.ErrorMessage}");
#endif
                    // TODO: Play error sound/show error feedback
                }
            }
            // Second confirm: Execute swap/move
            else
            {
                prepObject.potentialSwapPosition = cursorPos.Value;
                var result = prepObject.ExecutePositionAction();

                if (result.Success)
                {
#if UNITY_EDITOR
                    Debug.Log($"Executed position action (swap/move)");
#endif
                    // TODO: Update visuals, play sound
                }
                else
                {
#if UNITY_EDITOR
                    Debug.Log($"Cannot execute action: {result.ErrorMessage}");
#endif
                }
            }
        }

        private void HandleCancelInput()
        {
            var prepObject = _brain?.battleBrain?.PreparationObject;
            if (prepObject == null)
            {
                return;
            }

            // If unit is selected, deselect it
            if (prepObject.selectedPosition != null)
            {
                prepObject.ClearSelection();
#if UNITY_EDITOR
                Debug.Log("Cleared selection");
#endif
                // TODO: Clear visual feedback
            }
            // Otherwise, return to previous menu
        }

        protected override void OnDestroy()
        {
            _inputActions?.Disable();
            _inputActions?.Dispose();
            base.OnDestroy();
        }
    }
}
