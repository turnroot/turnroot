using UnityEngine;

namespace Turnroot.UI.Components.GridMenu
{
    public partial class GridMenu
    {
        protected override void NavigateToNextItem()
        {
            // Move down one item within the same column (if possible)
            if (menuItems.Count == 0)
            {
                return;
            }

            if (_cols == null || _cols.Count == 0)
            { /* fallback to row-based behavior */
                var newIndex = (_selectedIndex < 0 ? 0 : _selectedIndex) + Columns;
                if (newIndex >= menuItems.Count)
                {
                    newIndex = menuItems.Count - 1;
                }

                UpdateSelectionTo(newIndex);
                return;
            }

            var idx = _selectedIndex < 0 ? 0 : _selectedIndex;
            if (!_indexToCr.TryGetValue(idx, out var cr))
            {
                return;
            }

            var col = cr.col;
            var row = cr.row;
            var targetRow = row + 1;
            if (targetRow >= _cols[col].Count)
            {
                return;
            }

            UpdateSelectionTo(_cols[col][targetRow]);
        }

        protected override void NavigateToPreviousItem()
        {
            // Move up one item within the same column (if possible)
            if (menuItems.Count == 0)
            {
                return;
            }

            if (_cols == null || _cols.Count == 0)
            { /* fallback to row-based behavior */
                var newIndex = (_selectedIndex < 0 ? 0 : _selectedIndex) - Columns;
                if (newIndex < 0)
                {
                    newIndex = 0;
                }

                UpdateSelectionTo(newIndex);
                return;
            }

            var idx = _selectedIndex < 0 ? 0 : _selectedIndex;
            if (!_indexToCr.TryGetValue(idx, out var cr))
            {
                return;
            }

            var col = cr.col;
            var row = cr.row;
            var targetRow = row - 1;
            if (targetRow < 0)
            {
                return;
            }

            UpdateSelectionTo(_cols[col][targetRow]);
        }

        private void NavigateLeft()
        {
            if (menuItems.Count == 0)
            {
                return;
            }

            if (_cols == null || _cols.Count == 0)
            {
                _selectedIndex = Mathf.Max(0, _selectedIndex - 1);
                UpdateSelectionTo(_selectedIndex);
                return;
            }

            var idx = _selectedIndex < 0 ? 0 : _selectedIndex;
            if (!_indexToCr.TryGetValue(idx, out var cr))
            {
                return;
            }

            var col = cr.col;
            var row = cr.row;

            if (col > 0)
            {
                col--;
            }
            else
            {
                // wrap to rightmost column
                col = _cols.Count - 1;
            }

            var targetRow = Mathf.Min(row, _cols[col].Count - 1);
            var newIndex = _cols[col][targetRow];
            UpdateSelectionTo(newIndex);
        }

        private void NavigateRight()
        {
            if (menuItems.Count == 0)
            {
                return;
            }

            if (_cols == null || _cols.Count == 0)
            {
                _selectedIndex = Mathf.Min(menuItems.Count - 1, _selectedIndex + 1);
                UpdateSelectionTo(_selectedIndex);
                return;
            }

            var idx = _selectedIndex < 0 ? 0 : _selectedIndex;
            if (!_indexToCr.TryGetValue(idx, out var cr))
            {
                return;
            }

            var col = cr.col;
            var row = cr.row;

            if (col < _cols.Count - 1)
            {
                col++;
            }
            else
            {
                // wrap to leftmost column
                col = 0;
            }

            var targetRow = Mathf.Min(row, _cols[col].Count - 1);
            UpdateSelectionTo(_cols[col][targetRow]);
        }
    }
}
