using System.Collections.Generic;
using Turnroot.UI.Components.Menu;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Turnroot.UI.Components.GridMenu
{
    /// <summary>
    /// Partial of GridMenu containing configuration fields and lifecycle hooks.
    /// </summary>
    public partial class GridMenu : MenuBase
    {
        [Min(1)]
        public int Columns = 1;

        public InputActionReference NavigateLeftAction;
        public InputActionReference NavigateRightAction;
        private int _selectedIndex = -1;

        private InputAction GetNavigateLeftAction() =>
            NavigateLeftAction?.action ?? UIInputActionDefaults.NavigateLeft;

        private InputAction GetNavigateRightAction() =>
            NavigateRightAction?.action ?? UIInputActionDefaults.NavigateRight;

        protected override void Awake()
        {
            base.Awake();
            GetNavigateLeftAction()?.Enable();
            GetNavigateRightAction()?.Enable();
            // Keep MenuBase informed when pointer hovers items so we can track hover-based selection
            OnNavigate += HandleNavigateTo;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            GetNavigateLeftAction()?.Enable();
            GetNavigateRightAction()?.Enable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            GetNavigateLeftAction()?.Disable();
            GetNavigateRightAction()?.Disable();

            OnNavigate -= HandleNavigateTo;
        }

        // Grid structure (rows of indices) built from item positions; used for smarter navigation
        private List<List<int>> _rows = new();
        private Dictionary<int, (int row, int col)> _indexToRc = new();

        // Column-oriented structure (columns of indices) used for left/right/up/down navigation
        private List<List<int>> _cols = new();
        private Dictionary<int, (int col, int row)> _indexToCr = new();

        // Tolerance for grouping items into the same row (in world-space units)
        public float RowGroupingTolerance = 20f;

        // Tolerance for grouping items into the same column (in world-space units)
        public float ColumnGroupingTolerance = 20f;
    }
}
