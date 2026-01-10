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
        private int _selectedIndex = -1;

        protected override void Awake()
        {
            base.Awake();
            NavigateLeftAction?.Enable();
            NavigateRightAction?.Enable();
            // Keep MenuBase informed when pointer hovers items so we can track hover-based selection
            OnNavigate += HandleNavigateTo;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            NavigateLeftAction?.Enable();
            NavigateRightAction?.Enable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            NavigateLeftAction?.Disable();
            NavigateRightAction?.Disable();

            OnNavigate -= HandleNavigateTo;
        }

        // Grid structure (rows of indices) built from item positions; used for smarter navigation
        private System.Collections.Generic.List<System.Collections.Generic.List<int>> _rows = new();
        private System.Collections.Generic.Dictionary<int, (int row, int col)> _indexToRc = new();

        // Column-oriented structure (columns of indices) used for left/right/up/down navigation
        private System.Collections.Generic.List<System.Collections.Generic.List<int>> _cols = new();
        private System.Collections.Generic.Dictionary<int, (int col, int row)> _indexToCr = new();

        // Tolerance for grouping items into the same row (in world-space units)
        // Increased default so small vertical offsets between column parents still group into the same row.
        public float RowGroupingTolerance = 20f;

        // Tolerance for grouping items into the same column (in world-space units)
        public float ColumnGroupingTolerance = 20f;

        [Header("Debug")]
        [Tooltip("Dump internal grid rows/columns mapping to the console when building rows")]
        public bool DebugDumpGrid = false;

        public override void RefreshMenuItems()
        {
            base.RefreshMenuItems();

            BuildGridRows();

            // Ensure selection is valid after items refresh
            if (menuItems.Count > 0)
            {
                if (_selectedIndex < 0 || _selectedIndex >= menuItems.Count)
                {
                    _selectedIndex = 0;
                }

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
        }

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
            {
                return;
            }

            var entries = new System.Collections.Generic.List<(int idx, float y, float x)>();
            for (int i = 0; i < menuItems.Count; i++)
            {
                var t = menuItems[i].transform;
                float y,
                    x;
                var rt = t as RectTransform;
                if (rt != null)
                {
                    // Use world-space positions so items under different parent columns
                    // are compared in the same coordinate space when grouping rows.
                    var wp = rt.position;
                    y = wp.y;
                    x = wp.x;
                }
                else
                {
                    var wp = t.position;
                    y = wp.y;
                    x = wp.x;
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
                    {
                        rowIndices.Add(it.idx);
                    }

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
                {
                    rowIndices.Add(it.idx);
                }

                _rows.Add(rowIndices);
            }

            for (int r = 0; r < _rows.Count; r++)
            {
                for (int c = 0; c < _rows[r].Count; c++)
                {
                    _indexToRc[_rows[r][c]] = (r, c);
                }
            }

            // Build column groups (group by x position then sort by y descending inside each column)
            _cols.Clear();
            _indexToCr.Clear();

            var entriesByX = new System.Collections.Generic.List<(int idx, float x, float y)>();
            for (int i = 0; i < menuItems.Count; i++)
            {
                var t = menuItems[i].transform;
                float y,
                    x;
                var rt = t as RectTransform;
                if (rt != null)
                {
                    var wp = rt.position;
                    y = wp.y;
                    x = wp.x;
                }
                else
                {
                    var p = t.position;
                    y = p.y;
                    x = p.x;
                }
                entriesByX.Add((i, x, y));
            }

            // sort by x ascending (left to right)
            entriesByX.Sort((a, b) => a.x.CompareTo(b.x));

            float xTol = Mathf.Abs(ColumnGroupingTolerance);
            var currentCol = new System.Collections.Generic.List<(int idx, float x, float y)>();
            float? colX = null;

            foreach (var e in entriesByX)
            {
                if (colX == null)
                {
                    colX = e.x;
                    currentCol.Add(e);
                    continue;
                }

                if (Mathf.Abs(e.x - colX.Value) <= xTol)
                {
                    currentCol.Add(e);
                }
                else
                {
                    // finalize column: sort by y descending (top to bottom)
                    currentCol.Sort((p, q) => q.y.CompareTo(p.y));
                    var colIndices = new System.Collections.Generic.List<int>();
                    foreach (var it in currentCol)
                    {
                        colIndices.Add(it.idx);
                    }

                    _cols.Add(colIndices);

                    currentCol.Clear();
                    colX = e.x;
                    currentCol.Add(e);
                }
            }

            if (currentCol.Count > 0)
            {
                currentCol.Sort((p, q) => q.y.CompareTo(p.y));
                var colIndices = new System.Collections.Generic.List<int>();
                foreach (var it in currentCol)
                {
                    colIndices.Add(it.idx);
                }

                _cols.Add(colIndices);
            }

            for (int c = 0; c < _cols.Count; c++)
            {
                for (int r = 0; r < _cols[c].Count; r++)
                {
                    _indexToCr[_cols[c][r]] = (c, r);
                }
            }

#if UNITY_EDITOR
            if (DebugDumpGrid)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("GridMenu: BuildGridRows dump (rows):");
                for (int r = 0; r < _rows.Count; r++)
                {
                    sb.Append($"Row {r}: ");
                    foreach (var idx in _rows[r])
                    {
                        var t = menuItems[idx].transform;
                        var pos =
                            (t as RectTransform) != null ? ((RectTransform)t).position : t.position;
                        sb.Append($"{idx}({menuItems[idx].ItemName}@y={pos.y:F1},x={pos.x:F1}) ");
                    }
                    sb.AppendLine();
                }

                sb.AppendLine("GridMenu: BuildGridRows dump (cols):");
                for (int c = 0; c < _cols.Count; c++)
                {
                    sb.Append($"Col {c}: ");
                    foreach (var idx in _cols[c])
                    {
                        var t = menuItems[idx].transform;
                        var pos =
                            (t as RectTransform) != null ? ((RectTransform)t).position : t.position;
                        sb.Append($"{idx}({menuItems[idx].ItemName}@y={pos.y:F1},x={pos.x:F1}) ");
                    }
                    sb.AppendLine();
                }
                Debug.Log(sb.ToString());
            }
#endif
        }

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
            { /* fallback to old behavior */
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
            { /* fallback to old behavior */
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

        protected override void SelectCurrentItem()
        {
            if (_selectedIndex < 0 && menuItems.Count > 0)
            {
                _selectedIndex = 0;
            }

            if (_selectedIndex >= 0 && _selectedIndex < menuItems.Count)
            {
                SelectItem(menuItems[_selectedIndex]);
            }
        }
    }
}
