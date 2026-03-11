using System.Collections.Generic;
using UnityEngine;

namespace Turnroot.UI.Components.GridMenu
{
    public partial class GridMenu
    {
        private void BuildGridRows()
        {
            _rows.Clear();
            _indexToRc.Clear();

            if (menuItems.Count == 0)
            {
                return;
            }

            var entries = new List<(int idx, float y, float x)>();
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
            var currentRow = new List<(int idx, float y, float x)>();
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
                    var rowIndices = new List<int>();
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
                var rowIndices = new List<int>();
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

            var entriesByX = new List<(int idx, float x, float y)>();
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
            var currentCol = new List<(int idx, float x, float y)>();
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
                    var colIndices = new List<int>();
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
                var colIndices = new List<int>();
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
        }
    }
}
