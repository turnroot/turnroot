using NaughtyAttributes;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Turnroot.GameSettings
{
    /// <summary>
    /// Singleton ScriptableObject that holds all UI InputActionReferences used by the game.
    /// Assign the references in the inspector and they will be forwarded to UIInputActionDefaults at startup.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GameplayInputSettings",
        menuName = "Turnroot/Game Settings/Gameplay/Input Settings"
    )]
    public class GameplayInputSettings : SingletonScriptableObject<GameplayInputSettings>
    {
        [Header("UI Action References"), HorizontalLine(color: EColor.Blue)]
        public InputActionReference Select;
        public InputActionReference Back;
        public InputActionReference NavigateUp;
        public InputActionReference NavigateDown;
        public InputActionReference NavigateLeft;
        public InputActionReference NavigateRight;
        public InputActionReference ScrollLeft;
        public InputActionReference ScrollRight;
        public InputActionReference Navigate;
        public InputActionReference Confirm;
        public InputActionReference Cancel;
        public InputActionReference Menu;
        public InputActionReference RotateCamera;
        public InputActionReference RightStickMove;
        public InputActionReference Start;
        public InputActionReference ToggleDetails;
        public InputActionReference RightStickClick;
        public InputActionReference Special;

        [Header("Hold Repeat"), HorizontalLine(color: EColor.Green)]
        [Tooltip("Seconds before held navigation begins repeating.")]
        public float InitialRepeatDelay = 0.4f;

        [Tooltip("Seconds between each repeated navigation event while held.")]
        public float RepeatInterval = 0.1f;
    }
}
