using System;
using System.Collections.Generic;
using Turnroot.Gameplay.Brain.Segments;
using Turnroot.GameSettings;
using UnityEngine;

namespace Turnroot.UI.Components.Menu
{
    /// <summary>
    /// Abstract base class for menu systems, providing navigation, selection, and input handling functionality.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class MenuBase : MonoBehaviour
    {
        [HideInInspector]
        public List<MenuItemBase> menuItems = new();

        public GameObject BackButtonPrefab;

        // Input actions are sourced from UIInputActionDefaults (configured via UIInputActionBootstrap)

        protected GamewideUiSettings _uiSettings;

        [HideInInspector]
        public UiBrain uiBrain;

        public event Action<MenuItemBase> OnNavigate;
        public event Action<MenuItemBase> OnItemSelected;

        protected readonly int _currentSelectedIndex = -1;
        private int _actualCurrentSelectedIndex = -1;
        private int _previousSelectedIndex = -1;

        protected virtual void Awake()
        {
            // Initialize menu items
            RefreshMenuItems();

            // If input actions are not yet available, ensure they get enabled when ready.
            UIInputActionDefaults.WhenInitialized(EnableMenuInputActions);

            // Set up input actions if they exist right now.
            EnableMenuInputActions();
        }

        protected virtual void OnEnable() =>
            // Use shared UI input actions from UIInputActionDefaults.
            EnableMenuInputActions();

        protected virtual void OnDisable()
        {
            // Do not disable shared input actions; they are always available.
        }

        private void OnDestroy() =>
            UIInputActionDefaults.RemoveInitializedHandler(EnableMenuInputActions);

        private void EnableMenuInputActions()
        {
            UIInputActionDefaults.NavigateUp?.Enable();
            UIInputActionDefaults.NavigateDown?.Enable();
            UIInputActionDefaults.Select?.Enable();
        }

        protected virtual void Update()
        {
            HandleKeyboardNavigation();
            HandleSelectionInput();
        }

        public virtual void RefreshMenuItems()
        {
            menuItems.Clear();
            var items = GetComponentsInChildren<MenuItemBase>();
            foreach (var item in items)
            {
                item.SetParentMenu(this);
                menuItems.Add(item);
            }
        }

        public virtual void ResetSelection()
        {
            _actualCurrentSelectedIndex = -1;
            _previousSelectedIndex = -1;
        }

        public void SetSelection(int index)
        {
            if (index >= 0 && index < menuItems.Count)
            {
                _actualCurrentSelectedIndex = index;
                _previousSelectedIndex = -1;
                HighlightCurrentItem();
            }
        }

        public virtual void NavigateToItem(MenuItemBase item) => OnNavigate?.Invoke(item);

        public virtual void SelectItem(MenuItemBase item) => OnItemSelected?.Invoke(item);

        protected virtual void HandleKeyboardNavigation()
        {
            if (menuItems.Count == 0)
            {
                return;
            }

            if (UIInputActionDefaults.NavigateUp?.WasPressedThisFrame() == true)
            {
                NavigateToPreviousItem();
            }

            if (UIInputActionDefaults.NavigateDown?.WasPressedThisFrame() == true)
            {
                NavigateToNextItem();
            }
        }

        protected virtual void HandleSelectionInput()
        {
            if (UIInputActionDefaults.Select == null || menuItems.Count == 0)
            {
                return;
            }

            if (UIInputActionDefaults.Select.WasPressedThisFrame())
            {
                SelectCurrentItem();
            }
        }

        protected virtual void NavigateToNextItem()
        {
            if (menuItems.Count == 0)
            {
                return;
            }

            _actualCurrentSelectedIndex = (_actualCurrentSelectedIndex + 1) % menuItems.Count;
            HighlightCurrentItem();
        }

        protected virtual void NavigateToPreviousItem()
        {
            if (menuItems.Count == 0)
            {
                return;
            }

            _actualCurrentSelectedIndex =
                (_actualCurrentSelectedIndex - 1 + menuItems.Count) % menuItems.Count;
            HighlightCurrentItem();
        }

        protected virtual void HighlightCurrentItem()
        {
            if (menuItems.Count == 0 || _actualCurrentSelectedIndex >= menuItems.Count)
            {
                return;
            }

            // Only update highlighting if the selection has actually changed
            if (_previousSelectedIndex != _actualCurrentSelectedIndex)
            {
                if (_previousSelectedIndex >= 0 && _previousSelectedIndex < menuItems.Count)
                {
                    menuItems[_previousSelectedIndex].TryGetComponent<UiChoice>(out var prevChoice);
                    prevChoice?.Deselect();
                }

                menuItems[_actualCurrentSelectedIndex].TryGetComponent<UiChoice>(out var curChoice);
                curChoice?.Select();

                _previousSelectedIndex = _actualCurrentSelectedIndex;
            }

            NavigateToItem(menuItems[_actualCurrentSelectedIndex]);
        }

        protected virtual void SelectCurrentItem()
        {
            if (menuItems.Count == 0 || _actualCurrentSelectedIndex >= menuItems.Count)
            {
                return;
            }

            var currentItem = menuItems[_actualCurrentSelectedIndex];
            SelectItem(currentItem);
        }
    }
}
