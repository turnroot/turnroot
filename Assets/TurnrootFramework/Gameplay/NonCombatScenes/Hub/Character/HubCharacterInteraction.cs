using NaughtyAttributes;
using Turnroot.Characters;
using Turnroot.UI;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Character
{
    /// <summary>
    /// Skeleton for the hub character interaction overlay.
    /// Manages the actions menu shown after a character's welcome dialogue completes.
    /// Extend this class (or add partials) to implement individual interaction options.
    /// </summary>
    public class HubCharacterInteraction : MonoBehaviour
    {
        [Tooltip("UIFade for the character actions menu shown after the welcome dialogue.")]
        public UIFade ActionsMenuFade;

        /// <summary>The character currently being interacted with.</summary>
        public CharacterInstance ActiveCharacter { get; private set; }

        /// <summary>Bind this interaction to a specific character at runtime.</summary>
        public void Initialize(CharacterInstance character)
        {
            ActiveCharacter = character;
        }

        /// <summary>Show the actions menu.</summary>
        public void ShowActionsMenu() => ActionsMenuFade?.Show();

        /// <summary>Hide the actions menu.</summary>
        public void HideActionsMenu() => ActionsMenuFade?.Hide();
    }
}
