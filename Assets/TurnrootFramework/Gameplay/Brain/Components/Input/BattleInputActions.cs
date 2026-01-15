using UnityEngine.InputSystem;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Encapsulates creation and lifecycle of the input actions used by battle input controller.
    /// Keeps wiring in one place and makes the controller easier to test.
    /// </summary>
    internal class BattleInputActions
    {
        public InputAction Navigate { get; private set; }
        public InputAction Confirm { get; private set; }
        public InputAction Cancel { get; private set; }
        public InputAction Menu { get; private set; }

        public BattleInputActions()
        {
            Navigate = CreateNavigateAction();
            Confirm = CreateConfirmAction();
            Cancel = CreateCancelAction();
            Menu = CreateMenuAction();
        }

        public void Enable()
        {
            Navigate?.Enable();
            Confirm?.Enable();
            Cancel?.Enable();
            Menu?.Enable();
        }

        public void Disable()
        {
            Navigate?.Disable();
            Confirm?.Disable();
            Cancel?.Disable();
            Menu?.Disable();
        }

        public void Dispose()
        {
            Navigate?.Dispose();
            Confirm?.Dispose();
            Cancel?.Dispose();
            Menu?.Dispose();

            Navigate = null;
            Confirm = null;
            Cancel = null;
            Menu = null;
        }

        private InputAction CreateNavigateAction()
        {
            var action = new InputAction("Navigate", InputActionType.Value);

            action
                .AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            action
                .AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");

            action.AddBinding("<Gamepad>/leftStick");
            action.AddBinding("<Gamepad>/dpad");

            return action;
        }

        private InputAction CreateConfirmAction()
        {
            var action = new InputAction(
                "Confirm",
                InputActionType.Button,
                "<Gamepad>/buttonSouth"
            );
            action.AddBinding("<Keyboard>/enter");
            action.AddBinding("<Keyboard>/space");
            return action;
        }

        private InputAction CreateCancelAction()
        {
            var action = new InputAction("Cancel", InputActionType.Button, "<Gamepad>/buttonEast");
            action.AddBinding("<Keyboard>/escape");
            return action;
        }

        private InputAction CreateMenuAction()
        {
            var action = new InputAction("Menu", InputActionType.Button, "<Gamepad>/start");
            action.AddBinding("<Keyboard>/tab");
            return action;
        }
    }
}
