using Turnroot.Gameplay.PlayerSettings;
using Turnroot.UI.Components;
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
            // Navigation
            if (_inputActions?.Navigate?.enabled == true)
            {
                var direction = _inputActions.Navigate.ReadValue<Vector2>();
                if (direction.magnitude > 0.1f)
                {
                    _brain.cursorBrain?.NavigateCursor(direction);

                    // If a unit is already selected, preview swap/move to the new cursor position
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

                    return true;
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
