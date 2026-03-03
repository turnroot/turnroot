using Turnroot.Gameplay.PlayerSettings;
using UnityEngine;

namespace Turnroot.UI.Components.RadialMenu
{
    /// <summary>
    /// Partial class containing navigation logic and input handling for radial menu item selection.
    /// </summary>
    public partial class RadialMenu
    {
        /// <summary>
        /// Represents the current state of directional input navigation through menu items.
        /// </summary>
        private enum NavigationState
        {
            Idle,
            FirstPress,
            Holding,
            Repeating,
        }

        private NavigationState _navState = NavigationState.Idle;
        private float _navHoldTime = 0f;
        private float _navRepeatTimer = 0f;
        private bool _justNavigated = false;
        private float _justNavTimer = 0f;
        private Vector2 _lastNavigateInput;

        private static readonly Vector2 DirectionRight = Vector2.right;
        private static readonly Vector2 DirectionLeft = Vector2.left;
        private static readonly Vector2 DirectionUp = Vector2.up;
        private static readonly Vector2 DirectionDown = Vector2.down;

        private Vector2 GetCardinalDirection(Vector2 input)
        {
            return input.magnitude <= joystickDeadzone ? Vector2.zero
                : Mathf.Abs(input.x) >= Mathf.Abs(input.y)
                    ? (input.x > 0 ? DirectionRight : DirectionLeft)
                : (input.y > 0 ? DirectionUp : DirectionDown);
        }

        private void HandleNavigationInput()
        {
            if (navigateAction == null || (menuItems.Count == 0 && centerItem == null))
            {
                return;
            }

            // Gamepad: point stick to directly select the item at that angle.
            // Keyboard/D-pad: keep the existing sequential step-based navigation.
            var settings = GameplayPlayerSettings.Instance;
            bool isGamepad =
                settings != null
                && settings.PreferredInputControl
                    == GameplayPlayerSettings.InputControlType.Gamepad;

            if (isGamepad)
            {
                HandleGamepadRadialNavigation();
                return;
            }

            Vector2 input = navigateAction.ReadValue<Vector2>();
            bool hasInput = input.magnitude > joystickDeadzone;
            Vector2 direction = GetCardinalDirection(input);

            // If the user reverses direction within the reversal window after a navigation,
            // treat it as a request to select the center item (if present). Require an exact reversal
            // (opposite cardinal direction) to avoid accidental center selection on repeated input.
            if (
                hasInput
                && _justNavigated
                && centerItem != null
                && direction == -_lastNavigateInput
            )
            {
                SelectItemByIndex(0, true);
                TransitionTo(NavigationState.Idle, Vector2.zero);
                return;
            }

            switch (_navState)
            {
                case NavigationState.Idle:
                    if (hasInput)
                    {
                        TransitionTo(NavigationState.FirstPress, input);
                    }
                    break;

                case NavigationState.FirstPress:
                    if (!hasInput)
                    {
                        TransitionTo(NavigationState.Idle, Vector2.zero);
                    }
                    else if (direction != _lastNavigateInput)
                    {
                        // Direction changed - check for reversal (require exact opposite)
                        if (
                            _justNavigated
                            && centerItem != null
                            && direction == -_lastNavigateInput
                        )
                        {
                            // Exact opposite direction during reversal window = select center
                            SelectItemByIndex(0, true);
                            TransitionTo(NavigationState.Idle, Vector2.zero);
                        }
                        else
                        {
                            // Different direction, not an exact reversal - navigate again
                            NavigateInDirection(direction);
                            _lastNavigateInput = direction;
                            _justNavigated = true;
                            _justNavTimer = 0f;
                            // Stay in FirstPress to allow chaining
                        }
                    }
                    else
                    {
                        // Same direction still held - transition to holding
                        TransitionTo(NavigationState.Holding, input);
                    }
                    break;

                case NavigationState.Holding:
                    if (!hasInput)
                    {
                        TransitionTo(NavigationState.Idle, Vector2.zero);
                    }
                    else
                    {
                        _navHoldTime += Time.deltaTime;
                        if (_navHoldTime >= navigationInitialDelay)
                        {
                            TransitionTo(NavigationState.Repeating, input);
                        }
                    }
                    break;

                case NavigationState.Repeating:
                    if (!hasInput)
                    {
                        TransitionTo(NavigationState.Idle, Vector2.zero);
                    }
                    else
                    {
                        _navRepeatTimer += Time.deltaTime;
                        if (_navRepeatTimer >= navigationRepeatDelay)
                        {
                            NavigateSegments(_lastNavigateInput);
                            _navRepeatTimer = 0f;
                        }
                    }
                    break;
            }
        }

        private void HandleGamepadRadialNavigation()
        {
            Vector2 input = navigateAction.ReadValue<Vector2>();
            if (input.magnitude <= joystickDeadzone)
            {
                // Stick released — if a center item exists, move to it; otherwise keep selection.
                if (centerItem != null && !_centerSelected)
                {
                    SelectItemByIndex(0, true);
                }
                return;
            }

            // Convert stick vector to an angle (0° = up, 90° = right, etc.) and select the
            // closest item to that angle directly — standard radial menu gamepad feel.
            float angle = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg;
            if (angle < 0f)
            {
                angle += 360f;
            }

            int targetIndex = GetItemIndexAtAngle(angle);
            if (_centerSelected || targetIndex != _selectedIndex)
            {
                SelectItemByIndex(targetIndex, false);
            }
        }

        private void UpdateReversalWindow()
        {
            if (_justNavigated)
            {
                _justNavTimer += Time.deltaTime;
                if (_justNavTimer > reversalWindow)
                {
                    _justNavigated = false;
                }
            }
        }

        private void TransitionTo(NavigationState newState, Vector2 input)
        {
            var oldState = _navState;
            _navState = newState;

            switch (newState)
            {
                case NavigationState.Idle:
                    // Do not clear _lastNavigateInput here — keep last direction available for
                    // reversal detection across a brief release between inputs.
                    _navHoldTime = 0f;
                    _navRepeatTimer = 0f;
                    break;

                case NavigationState.FirstPress:
                    Vector2 dir = GetCardinalDirection(input);
                    NavigateInDirection(dir);
                    _lastNavigateInput = dir;
                    _justNavigated = true;
                    _justNavTimer = 0f;
                    break;

                case NavigationState.Holding:
                    _navHoldTime = 0f;
                    break;

                case NavigationState.Repeating:
                    _navRepeatTimer = 0f;
                    break;
            }
        }

        private int GetItemIndexAtAngle(float targetAngle)
        {
            if (menuItems.Count == 0)
            {
                return -1;
            }

            // Normalize target angle to 0-360
            targetAngle = (targetAngle + 360f) % 360f;

            int closestIndex = 0;
            float closestDiff = float.MaxValue;

            for (int i = 0; i < menuItems.Count; i++)
            {
                float itemAngle = (i * _rotStep + _rotStep * 0.5f) % 360f;
                float diff = Mathf.Abs(Mathf.DeltaAngle(targetAngle, itemAngle));

                if (diff < closestDiff)
                {
                    closestDiff = diff;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

        private void NavigateInDirection(Vector2 direction)
        {
            if (centerItem == null)
            {
                NavigateSegments(direction);
                return;
            }

            if (_centerSelected)
            {
                // From center, navigate to the item in the direction pressed
                // Convert direction to angle (0° = up/top, 90° = right, etc.)
                float targetAngle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
                if (targetAngle < 0)
                {
                    targetAngle += 360f;
                }

                int targetIndex = GetItemIndexAtAngle(targetAngle);
                SelectItemByIndex(targetIndex, false);
            }
            else
            {
                NavigateSegments(direction);
            }
        }

        private void NavigateSegments(Vector2 direction)
        {
            if (menuItems.Count == 0)
            {
                return;
            }

            int delta = 0;

            if (direction == Vector2.right || direction == Vector2.down)
            {
                delta = 1;
            }
            else if (direction == Vector2.left || direction == Vector2.up)
            {
                delta = -1;
            }

            if (delta != 0)
            {
                int newIndex = (_selectedIndex + delta + menuItems.Count) % menuItems.Count;
                SelectItemByIndex(newIndex, false);
            }
        }

        private void HandleItemHover(int index, bool isCenter) =>
            SelectItemByIndex(index, isCenter);

        private void SelectItemByIndex(int index, bool isCenter)
        {
            if (_centerSelected && centerItem != null)
            {
                centerItem.Deselect();
            }
            else if (!_centerSelected && _selectedIndex >= 0 && _selectedIndex < menuItems.Count)
            {
                menuItems[_selectedIndex].Deselect();
            }

            _centerSelected = isCenter;
            if (isCenter && centerItem != null)
            {
                centerItem.Select();
                OnNavigate?.Invoke(centerItem);
            }
            else if (!isCenter && index >= 0 && index < menuItems.Count)
            {
                _selectedIndex = index;
                menuItems[_selectedIndex].Select();
                OnNavigate?.Invoke(menuItems[_selectedIndex]);
            }
        }

        private void ConfirmSelection()
        {
            if (_centerSelected && centerItem != null)
            {
                OnItemSelected?.Invoke(centerItem);
            }
            else if (_selectedIndex >= 0 && _selectedIndex < menuItems.Count)
            {
                OnItemSelected?.Invoke(menuItems[_selectedIndex]);
            }
        }

        public void SetSelectedIndex(int index, bool selectCenter = false) =>
            SelectItemByIndex(index, selectCenter);

        public int GetSelectedIndex() => _selectedIndex;

        public bool IsCenterSelected() => _centerSelected;

        public MenuItemBase GetSelectedItem()
        {
            if (_centerSelected)
            {
                return centerItem;
            }
            else if (_selectedIndex >= 0 && _selectedIndex < menuItems.Count)
            {
                return menuItems[_selectedIndex];
            }
            return null;
        }
    }
}
