using System.Linq;
using Turnroot.UI.Components.Menu;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Turnroot.UI.Components.GridMenu
{
    public class GridMenu : MenuBase
    {
        [Min(1)]
        public int Columns = 1;

        public InputAction NavigateLeftAction;
        public InputAction NavigateRightAction;
        public InputAction DetailsAction;

        private int _selectedIndex = -1;

        protected override void Awake()
        {
            base.Awake();
            NavigateLeftAction?.Enable();
            NavigateRightAction?.Enable();
            DetailsAction?.Enable();

            // Keep MenuBase informed when pointer hovers items so we can track hover-based selection
            OnNavigate += HandleNavigateTo;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            NavigateLeftAction?.Enable();
            NavigateRightAction?.Enable();
            DetailsAction?.Enable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            NavigateLeftAction?.Disable();
            NavigateRightAction?.Disable();
            DetailsAction?.Disable();

            OnNavigate -= HandleNavigateTo;
        }

        // Grid structure (rows of indices) built from item positions; used for smarter navigation
        private System.Collections.Generic.List<System.Collections.Generic.List<int>> _rows = new();
        private System.Collections.Generic.Dictionary<int, (int row, int col)> _indexToRc = new();

        // Tolerance for grouping items into the same row (in local space units)
        public float RowGroupingTolerance = 2f;

        public override void RefreshMenuItems()
        {
            base.RefreshMenuItems();

            BuildGridRows();

            // Ensure selection is valid after items refresh
            if (menuItems.Count > 0)
            {
                if (_selectedIndex < 0 || _selectedIndex >= menuItems.Count)
                    _selectedIndex = 0;
                UpdateSelectionTo(_selectedIndex);
            }
            else
            {
                _selectedIndex = -1;
            }
        }

        protected override void HandleKeyboardNavigation()
        {
            base.HandleKeyboardNavigation(); // handles up/down via base

            if (NavigateLeftAction != null && NavigateLeftAction.WasPressedThisFrame())
            {
                NavigateLeft();
            }

            if (NavigateRightAction != null && NavigateRightAction.WasPressedThisFrame())
            {
                NavigateRight();
            }

            if (DetailsAction != null && DetailsAction.WasPressedThisFrame())
            {
                // Map details action to select for now
                SelectCurrentItem();
            }
        }

        private void HandleNavigateTo(MenuItemBase item)
        {
            // Mouse hover calls NavigateToItem; update our internal index
            var index = menuItems.IndexOf(item);
            if (index >= 0)
                UpdateSelectionTo(index);
        }

        private void UpdateSelectionTo(int index)
        {
            if (index < 0 || index >= menuItems.Count)
                return;
            if (_selectedIndex == index)
                return;

            // Simulate pointer exit on previous
            if (_selectedIndex >= 0 && _selectedIndex < menuItems.Count)
            {
                var prev = menuItems[_selectedIndex];
                if (
                    prev.TryGetComponent(
                        out Turnroot.UI.Components.SimpleButton.SimpleButton prevButton
                    )
                )
                {
                    var fakeExit = new PointerEventData(EventSystem.current);
                    prevButton.OnPointerExit(fakeExit);
                }
            }

            // Simulate pointer enter on new
            var current = menuItems[index];
            if (
                current.TryGetComponent(
                    out Turnroot.UI.Components.SimpleButton.SimpleButton curButton
                )
            )
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

        private void BuildGridRows()
        {
            _rows.Clear();
            _indexToRc.Clear();

            if (menuItems.Count == 0)
                return;

            var entries = new System.Collections.Generic.List<(int idx, float y, float x)>();
            for (int i = 0; i < menuItems.Count; i++)
            {
                var t = menuItems[i].transform;
                float y,
                    x;
                var rt = t as RectTransform;
                if (rt != null)
                {
                    var ap = rt.anchoredPosition;
                    y = ap.y;
                    x = ap.x;
                }
                else
                {
                    var lp = t.localPosition;
                    y = lp.y;
                    x = lp.x;
                }

                entries.Add((i, y, x));
            }

            // Sort by y descending (top to bottom visually)
            entries.Sort((a, b) => b.y.CompareTo(a.y));

            float tol = Mathf.Abs(RowGroupingTolerance);
            var currentRow = new System.Collections.Generic.List<(int idx, float y, float x)>();
            float? rowY = null;

            foreach (var e in entries)
            {
                if (rowY == null)
                {
                    rowY = e.y;
                    currentRow.Add(e);
                    continue;
                }

                if (Mathf.Abs(e.y - rowY.Value) <= tol)
                {
                    currentRow.Add(e);
                }
                else
                {
                    // finalize
                    currentRow.Sort((p, q) => p.x.CompareTo(q.x));
                    var rowIndices = new System.Collections.Generic.List<int>();
                    foreach (var it in currentRow)
                        rowIndices.Add(it.idx);
                    _rows.Add(rowIndices);

                    currentRow.Clear();
                    rowY = e.y;
                    currentRow.Add(e);
                }
            }

            if (currentRow.Count > 0)
            {
                currentRow.Sort((p, q) => p.x.CompareTo(q.x));
                var rowIndices = new System.Collections.Generic.List<int>();
                foreach (var it in currentRow)
                    rowIndices.Add(it.idx);
                _rows.Add(rowIndices);
            }

            for (int r = 0; r < _rows.Count; r++)
            {
                for (int c = 0; c < _rows[r].Count; c++)
                {
                    _indexToRc[_rows[r][c]] = (r, c);
                }
            }
        }

        protected override void NavigateToNextItem()
        {
            // Move down one visual row keeping the same column where possible
            if (menuItems.Count == 0)
                return;
            if (_rows == null || _rows.Count == 0)
            { /* fallback */
                var newIndex = (_selectedIndex < 0 ? 0 : _selectedIndex) + Columns;
                if (newIndex >= menuItems.Count)
                    newIndex = menuItems.Count - 1;
                UpdateSelectionTo(newIndex);
                return;
            }

            var idx = _selectedIndex < 0 ? 0 : _selectedIndex;
            if (!_indexToRc.TryGetValue(idx, out var rc))
                return;
            var row = rc.row;
            var col = rc.col;
            var targetRow = row + 1;
            if (targetRow >= _rows.Count)
                return;
            var targetCol = Mathf.Min(col, _rows[targetRow].Count - 1);
            UpdateSelectionTo(_rows[targetRow][targetCol]);
        }

        protected override void NavigateToPreviousItem()
        {
            // Move up one visual row keeping the same column where possible
            if (menuItems.Count == 0)
                return;
            if (_rows == null || _rows.Count == 0)
            { /* fallback */
                var newIndex = (_selectedIndex < 0 ? 0 : _selectedIndex) - Columns;
                if (newIndex < 0)
                    newIndex = 0;
                UpdateSelectionTo(newIndex);
                return;
            }

            var idx = _selectedIndex < 0 ? 0 : _selectedIndex;
            if (!_indexToRc.TryGetValue(idx, out var rc))
                return;
            var row = rc.row;
            var col = rc.col;
            var targetRow = row - 1;
            if (targetRow < 0)
                return;
            var targetCol = Mathf.Min(col, _rows[targetRow].Count - 1);
            UpdateSelectionTo(_rows[targetRow][targetCol]);
        }

        private void NavigateLeft()
        {
            if (menuItems.Count == 0)
                return;
            if (_rows == null || _rows.Count == 0)
            { /* fallback to old behavior */
                _selectedIndex = Mathf.Max(0, _selectedIndex - 1);
                UpdateSelectionTo(_selectedIndex);
                return;
            }

            var idx = _selectedIndex < 0 ? 0 : _selectedIndex;
            if (!_indexToRc.TryGetValue(idx, out var rc))
                return;
            var row = rc.row;
            var col = rc.col;

            if (col > 0)
            {
                col--;
            }
            else
            {
                // wrap to end of previous row if exists
                if (row > 0)
                {
                    row--;
                    col = _rows[row].Count - 1;
                }
                else
                {
                    // stay
                    return;
                }
            }

            var newIndex = _rows[row][Mathf.Min(col, _rows[row].Count - 1)];
            UpdateSelectionTo(newIndex);
        }

        private void NavigateRight()
        {
            if (menuItems.Count == 0)
                return;
            if (_rows == null || _rows.Count == 0)
            { /* fallback to old behavior */
                _selectedIndex = Mathf.Min(menuItems.Count - 1, _selectedIndex + 1);
                UpdateSelectionTo(_selectedIndex);
                return;
            }

            var idx = _selectedIndex < 0 ? 0 : _selectedIndex;
            if (!_indexToRc.TryGetValue(idx, out var rc))
                return;
            var row = rc.row;
            var col = rc.col;

            if (col < _rows[row].Count - 1)
            {
                col++;
                UpdateSelectionTo(_rows[row][col]);
                return;
            }

            // move to first element of next row if exists
            if (row + 1 < _rows.Count && _rows[row + 1].Count > 0)
            {
                var targetCol = Mathf.Min(col, _rows[row + 1].Count - 1);
                UpdateSelectionTo(_rows[row + 1][targetCol]);
                return;
            }

            // otherwise stay at last item
        }

        protected override void SelectCurrentItem()
        {
            if (_selectedIndex < 0 && menuItems.Count > 0)
                _selectedIndex = 0;
            if (_selectedIndex >= 0 && _selectedIndex < menuItems.Count)
            {
                SelectItem(menuItems[_selectedIndex]);
            }
        }
    }
}
