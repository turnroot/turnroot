using UnityEngine;

namespace Turnroot.UI.Components.Menu.Submenu
{
    /// <summary>
    /// Input types for navigating submenu rows.
    /// </summary>
    public enum SubmenuRowInput
    {
        Left,
        Right,
        Select,
    }

    /// <summary>
    /// Manages a collection of PanelRow components with keyboard/gamepad navigation support.
    /// </summary>
    public class PanelRows : MonoBehaviour
    {
        public PanelRow[] panelRows;

        [HideInInspector]
        public int currentRowIndex = 0;

        // Input actions are sourced from UIInputActionDefaults (configured via UIInputActionBootstrap)

        public void Awake()
        {
            var index = 0;
            foreach (var row in panelRows)
            {
                row.rowIndex = index;
                index++;
            }

            Initialize();

            UIInputActionDefaults.WhenInitialized(EnableInputActions);
            EnableInputActions();
        }

        public void Initialize()
        {
            currentRowIndex = 0;
            UpdateRowFocus();
        }

        private void OnEnable() => EnableInputActions();

        private void OnDisable()
        {
            // Shared UI actions remain enabled globally; do not disable them here.
        }

        private void OnDestroy() => UIInputActionDefaults.RemoveInitializedHandler(EnableInputActions);

        private void EnableInputActions()
        {
            UIInputActionDefaults.NavigateUp?.Enable();
            UIInputActionDefaults.NavigateDown?.Enable();
            UIInputActionDefaults.Select?.Enable();
        }

        private void Update() => HandleInput();

        private void HandleInput()
        {
            if (panelRows == null || panelRows.Length == 0)
            {
                return;
            }

            if (
                UIInputActionDefaults.NavigateUp == null
                || UIInputActionDefaults.NavigateDown == null
                || UIInputActionDefaults.Select == null
            )
            {
                return;
            }

            if (UIInputActionDefaults.NavigateUp.WasPressedThisFrame())
            {
                NavigateUp();
            }
            else if (UIInputActionDefaults.NavigateDown.WasPressedThisFrame())
            {
                NavigateDown();
            }
            else if (UIInputActionDefaults.NavigateLeft?.WasPressedThisFrame() is true)
            {
                HandleRowInput(SubmenuRowInput.Left);
            }
            else if (UIInputActionDefaults.NavigateRight?.WasPressedThisFrame() is true)
            {
                HandleRowInput(SubmenuRowInput.Right);
            }
            else if (UIInputActionDefaults.Select.WasPressedThisFrame())
            {
                HandleRowInput(SubmenuRowInput.Select);
            }
        }

        private void NavigateUp()
        {
            currentRowIndex = (currentRowIndex - 1 + panelRows.Length) % panelRows.Length;
            UpdateRowFocus();
        }

        private void NavigateDown()
        {
            currentRowIndex = (currentRowIndex + 1) % panelRows.Length;
            UpdateRowFocus();
        }

        private void HandleRowInput(SubmenuRowInput inputType)
        {
            if (currentRowIndex >= 0 && currentRowIndex < panelRows.Length)
            {
                panelRows[currentRowIndex].HandleInput(inputType);
            }
        }

        private void UpdateRowFocus()
        {
            for (int i = 0; i < panelRows.Length; i++)
            {
                panelRows[i].SetFocused(i == currentRowIndex);
            }
        }
    }
}
