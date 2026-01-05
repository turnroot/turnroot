using System;
using System.Collections.Generic;
using Turnroot.GameSettings;
using Turnroot.UI.Components.ListMenu;
using TurnrootFramework.Gameplay.Brain.Segments;
using UnityEngine;
using UnityEngine.InputSystem;
using SimpleButtonComponent = Turnroot.UI.Components.SimpleButton.SimpleButton;

namespace Turnroot.UI.Components.Menu
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class MenuBase : MonoBehaviour
    {
        [HideInInspector]
        public List<ListMenuItem> menuItems = new();

        public GameObject BackButtonPrefab;

        public InputAction selectAction;
        public InputAction navigateUpAction;
        public InputAction navigateDownAction;

        protected GamewideUiSettings _uiSettings;

        [HideInInspector]
        public UiBrain uiBrain;

        public event Action<MenuItemBase> OnNavigate;
        public event Action<MenuItemBase> OnItemSelected;

        protected readonly int _currentSelectedIndex = 0;
        private int _actualCurrentSelectedIndex = 0;
        private int _previousSelectedIndex = -1;

        protected virtual void Awake()
        {
            // Initialize menu items
            RefreshMenuItems();
        }

        protected virtual void OnEnable()
        {
            navigateUpAction?.Enable();
            navigateDownAction?.Enable();
            selectAction?.Enable();
        }

        protected virtual void OnDisable()
        {
            navigateUpAction?.Disable();
            navigateDownAction?.Disable();
            selectAction?.Disable();
        }

        protected virtual void Update()
        {
            HandleKeyboardNavigation();
            HandleSelectionInput();
        }

        public virtual void RefreshMenuItems()
        {
            menuItems.Clear();
            var items = GetComponentsInChildren<ListMenuItem>();
            foreach (var item in items)
            {
                item.parentMenu = this;
                menuItems.Add(item);
            }
        }

        public virtual void NavigateToItem(ListMenuItem item) => OnNavigate?.Invoke(item);

        public virtual void SelectItem(ListMenuItem item) => OnItemSelected?.Invoke(item);

        protected virtual void HandleKeyboardNavigation()
        {
            if (menuItems.Count == 0)
            {
                return;
            }

            if (navigateUpAction != null && navigateUpAction.WasPressedThisFrame())
            {
                NavigateToPreviousItem();
            }

            if (navigateDownAction != null && navigateDownAction.WasPressedThisFrame())
            {
                NavigateToNextItem();
            }
        }

        protected virtual void HandleSelectionInput()
        {
            if (selectAction == null || menuItems.Count == 0)
            {
                return;
            }

            if (selectAction.WasPressedThisFrame())
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
                // Clear highlighting from previous item
                if (
                    _previousSelectedIndex >= 0
                    && _previousSelectedIndex < menuItems.Count
                    && menuItems[_previousSelectedIndex]
                        .TryGetComponent<SimpleButtonComponent>(out var prevButton)
                )
                {
                    if (UnityEngine.EventSystems.EventSystem.current != null)
                    {
                        var fakeExitEvent = new UnityEngine.EventSystems.PointerEventData(
                            UnityEngine.EventSystems.EventSystem.current
                        );
                        prevButton.OnPointerExit(fakeExitEvent);
                    }
                }

                // Highlight the current item
                var currentItem = menuItems[_actualCurrentSelectedIndex];
                if (currentItem.TryGetComponent<SimpleButtonComponent>(out var currentButton))
                {
                    if (UnityEngine.EventSystems.EventSystem.current != null)
                    {
                        var fakeHoverEvent = new UnityEngine.EventSystems.PointerEventData(
                            UnityEngine.EventSystems.EventSystem.current
                        );
                        currentButton.OnPointerEnter(fakeHoverEvent);
                    }
                }

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
