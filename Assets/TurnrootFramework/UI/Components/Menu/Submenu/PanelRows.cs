using UnityEngine;
using UnityEngine.InputSystem;

namespace Turnroot.UI.Components.Menu.Submenu
{
    public enum SubmenuRowInput
    {
        Left,
        Right,
        Select,
    }

    public class PanelRows : MonoBehaviour
    {
        public PanelRow[] panelRows;

        [HideInInspector]
        public int currentRowIndex = 0;

        public void Awake()
        {
            var index = 0;
            foreach (var row in panelRows)
            {
                row.rowIndex = index;
                index++;
            }
        }

        private void OnEnable()
        {
            navigateUpAction?.Enable();
            navigateDownAction?.Enable();
            navigateLeftAction?.Enable();
            navigateRightAction?.Enable();
            selectAction?.Enable();
        }

        private void OnDisable()
        {
            navigateUpAction?.Disable();
            navigateDownAction?.Disable();
            navigateLeftAction?.Disable();
            navigateRightAction?.Disable();
            selectAction?.Disable();
        }

        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            if (panelRows == null || panelRows.Length == 0)
            {
                return;
            }

            if (navigateUpAction?.WasPressedThisFrame() is true)
            {
                NavigateUp();
            }
            else if (navigateDownAction?.WasPressedThisFrame() is true)
            {
                NavigateDown();
            }
            else if (navigateLeftAction?.WasPressedThisFrame() is true)
            {
                HandleRowInput(SubmenuRowInput.Left);
            }
            else if (navigateRightAction?.WasPressedThisFrame() is true)
            {
                HandleRowInput(SubmenuRowInput.Right);
            }
            else if (selectAction?.WasPressedThisFrame() is true)
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

        public InputAction navigateUpAction;
        public InputAction navigateDownAction;
        public InputAction navigateLeftAction;
        public InputAction navigateRightAction;
        public InputAction selectAction;
    }
}
