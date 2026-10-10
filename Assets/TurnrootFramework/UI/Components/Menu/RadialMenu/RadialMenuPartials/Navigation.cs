using Turnroot.Gameplay.PlayerSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.UI.Components.RadialMenu
{
    /// <summary>
    /// Navigation and selection logic for the radial menu.
    /// Gamepad: point the stick at an item; release to return to center.
    /// Keyboard/D-pad: step through items; press the opposite direction to jump to center.
    /// </summary>
    public partial class RadialMenu
    {
        private Vector2 _heldDirection;
        private float _holdTime;
        private float _repeatTimer;
        private Vector2 _lastStepDirection;
        private float _lastStepTime = -10f;

        private const float ReversalWindow = 1.2f;

        private Vector2 GetCardinalDirection(Vector2 input)
        {
            if (input.magnitude <= joystickDeadzone)
            {
                return Vector2.zero;
            }

            return Mathf.Abs(input.x) >= Mathf.Abs(input.y)
                ? (input.x > 0 ? Vector2.right : Vector2.left)
                : (input.y > 0 ? Vector2.up : Vector2.down);
        }

        private void HandleNavigationInput()
        {
            var navigate = UIInputActionDefaults.Navigate;
            if (!_initialized || navigate == null || (menuItems.Count == 0 && centerItem == null))
            {
                return;
            }

            Vector2 input = navigate.ReadValue<Vector2>();

            var settings = GameplayPlayerSettings.Instance;
            if (
                settings != null
                && settings.PreferredInputControl == GameplayPlayerSettings.InputControlType.Gamepad
            )
            {
                HandleStickNavigation(input);
            }
            else
            {
                HandleStepNavigation(GetCardinalDirection(input));
            }
        }

        private void HandleStickNavigation(Vector2 input)
        {
            if (input.magnitude <= joystickDeadzone)
            {
                if (centerItem != null && !_centerSelected)
                {
                    SelectItemByIndex(0, true);
                }
                return;
            }

            float angle = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg;
            int target = GetItemIndexAtAngle(angle);
            if (target >= 0 && (_centerSelected || target != _selectedIndex))
            {
                SelectItemByIndex(target, false);
            }
        }

        private void HandleStepNavigation(Vector2 direction)
        {
            if (direction == Vector2.zero)
            {
                _heldDirection = Vector2.zero;
                _holdTime = 0f;
                _repeatTimer = 0f;
                return;
            }

            if (direction != _heldDirection)
            {
                _heldDirection = direction;
                _holdTime = 0f;
                _repeatTimer = 0f;
                Step(direction);
                return;
            }

            _holdTime += Time.deltaTime;
            if (_holdTime < navigationInitialDelay)
            {
                return;
            }

            _repeatTimer += Time.deltaTime;
            if (_repeatTimer >= navigationRepeatDelay)
            {
                _repeatTimer = 0f;
                Step(direction);
            }
        }

        private void Step(Vector2 direction)
        {
            bool reversed =
                centerItem != null
                && Time.time - _lastStepTime <= ReversalWindow
                && direction == -_lastStepDirection;

            _lastStepDirection = direction;
            _lastStepTime = Time.time;

            if (reversed)
            {
                SelectItemByIndex(0, true);
                return;
            }

            if (_centerSelected)
            {
                float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
                SelectItemByIndex(GetItemIndexAtAngle(angle), false);
                return;
            }

            if (menuItems.Count == 0)
            {
                return;
            }

            int delta = (direction == Vector2.right || direction == Vector2.down) ? 1 : -1;
            SelectItemByIndex((_selectedIndex + delta + menuItems.Count) % menuItems.Count, false);
        }

        private int GetItemIndexAtAngle(float targetAngle)
        {
            if (menuItems.Count == 0)
            {
                return -1;
            }

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

        private void SelectItemByIndex(int index, bool isCenter)
        {
            GetSelectedItem()?.Deselect();

            _centerSelected = isCenter && centerItem != null;
            if (_centerSelected)
            {
                centerItem.Select();
                OnNavigate?.Invoke(centerItem);
            }
            else if (index >= 0 && index < menuItems.Count)
            {
                _selectedIndex = index;
                menuItems[index].Select();
                OnNavigate?.Invoke(menuItems[index]);
            }
        }

        private void ConfirmSelection()
        {
            var item = GetSelectedItem();
            if (item == null)
            {
                "Select pressed but no item is selected.".LogWarning();
                return;
            }

            $"Confirmed '{item.ItemName}' (center: {_centerSelected}).".LogInfo();
            OnItemSelected?.Invoke(item);
        }

        public MenuItemBase GetSelectedItem()
        {
            if (_centerSelected)
            {
                return centerItem;
            }
            return _selectedIndex >= 0 && _selectedIndex < menuItems.Count
                ? menuItems[_selectedIndex]
                : null;
        }
    }
}
