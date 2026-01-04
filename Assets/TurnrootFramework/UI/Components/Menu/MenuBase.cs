using System;
using System.Collections.Generic;
using Turnroot.GameSettings;
using Turnroot.UI.Components.ListMenu;
using TurnrootFramework.Gameplay.Brain.Segments;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Turnroot.UI.Components.Menu
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class MenuBase : MonoBehaviour
    {
        [HideInInspector]
        public List<ListMenuItem> menuItems = new();

        public GameObject BackButtonPrefab;

        public InputAction selectAction;

        public InputAction navigateAction;
        protected GamewideUiSettings _uiSettings;

        [HideInInspector]
        public UiBrain uiBrain;

        public event Action<MenuItemBase> OnNavigate;
        public event Action<MenuItemBase> OnItemSelected;

        protected readonly int _currentSelectedIndex = 0;

        protected virtual void Awake()
        {
            // Initialize menu items
            RefreshMenuItems();
        }

        protected virtual void RefreshMenuItems()
        {
            menuItems.Clear();
            var items = GetComponentsInChildren<ListMenuItem>();
            foreach (var item in items)
            {
                item.parentMenu = this;
                menuItems.Add(item);
            }
        }

        public virtual void NavigateToItem(ListMenuItem item)
        {
            OnNavigate?.Invoke(item);
        }

        public virtual void SelectItem(ListMenuItem item)
        {
            OnItemSelected?.Invoke(item);
        }

        // TODO: Implement keyboard/gamepad navigation
        // TODO: Implement visual highlighting
        // TODO: Implement back button functionality
    }
}
