using UnityEngine.EventSystems;

namespace Turnroot.UI.Components.GridMenu
{
    public partial class GridMenu
    {
        private void HandleNavigateTo(MenuItemBase item)
        {
            // Mouse hover calls NavigateToItem; update our internal index
            var index = menuItems.IndexOf(item);
            if (index >= 0)
            {
                UpdateSelectionTo(index);
            }
        }

        private void UpdateSelectionTo(int index)
        {
            if (index < 0 || index >= menuItems.Count)
            {
                return;
            }

            if (_selectedIndex == index)
            {
                return;
            }

            // Simulate pointer exit on previous
            if (_selectedIndex >= 0 && _selectedIndex < menuItems.Count)
            {
                var prev = menuItems[_selectedIndex];
                if (prev.TryGetComponent(out SimpleButton.SimpleButton prevButton))
                {
                    var fakeExit = new PointerEventData(EventSystem.current);
                    prevButton.OnPointerExit(fakeExit);
                }
            }

            // Simulate pointer enter on new
            var current = menuItems[index];
            if (current.TryGetComponent(out SimpleButton.SimpleButton curButton))
            {
                var fakeEnter = new PointerEventData(EventSystem.current);
                curButton.OnPointerEnter(fakeEnter);
            }

            _selectedIndex = index;

            // Ensure grid map is up-to-date and cache row/col
            if (menuItems.Count > 0)
            {
                if (_rows == null || _rows.Count == 0 || !_indexToRc.ContainsKey(_selectedIndex))
                {
                    BuildGridRows();
                }
            }

            // Notify listeners
            NavigateToItem(menuItems[_selectedIndex]);
        }

        protected override void SelectCurrentItem()
        {
            if (_selectedIndex < 0 && menuItems.Count > 0)
            {
                _selectedIndex = 0;
            }

            if (_selectedIndex >= 0 && _selectedIndex < menuItems.Count)
            {
                var currentItem = menuItems[_selectedIndex];
                SelectItem(currentItem);
            }
        }
    }
}
