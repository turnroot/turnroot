using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using Turnroot.Gameplay.Brain.UI;
using Turnroot.GameSettings;
using Turnroot.UI.Components;
using Turnroot.Utilities;
using TurnrootFramework.Gameplay.Brain.Segments;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Turnroot.UI.Components.ListMenu
{
    [RequireComponent(typeof(CanvasGroup))]
    public class ListMenu : MonoBehaviour
    {
        [HideInInspector]
        public List<ListMenuItem> menuItems = new();

        public GameObject BackButtonPrefab;

        public InputAction selectAction;

        public InputAction navigateAction;
        private GamewideUiSettings _uiSettings;

        [HideInInspector]
        public UiBrain uiBrain;

        public event Action<MenuItemBase> OnNavigate;
        public event Action<MenuItemBase> OnItemSelected;

        private readonly int _currentSelectedIndex = 0;

        private void Awake()
        {
            // Initialize menu items
            RefreshMenuItems();
        }

        private void RefreshMenuItems()
        {
            menuItems.Clear();
            var items = GetComponentsInChildren<ListMenuItem>();
            foreach (var item in items)
            {
                item.parentMenu = this;
                menuItems.Add(item);
            }
        }

        public void NavigateToItem(ListMenuItem item)
        {
            OnNavigate?.Invoke(item);
        }

        public void SelectItem(ListMenuItem item)
        {
            OnItemSelected?.Invoke(item);
        }

        // TODO: Implement keyboard/gamepad navigation
        // TODO: Implement visual highlighting
        // TODO: Implement back button functionality
    }
}
