using Turnroot.Characters;
using Turnroot.Gameplay.Brain;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Conversations.Branching
{
    /// <summary>
    /// Conversation action node that increases support points between the source character
    /// (defaults to the player avatar) and a target character. Publishes brain support events
    /// so UI notifications can react to the change.
    /// </summary>
    [CreateNodeMenu("Conversation/Actions/Change Support Points")]
    public class ChangeSupportPointsNode : ConversationActionNode
    {
        [Tooltip("Character whose support points will increase. Required.")]
        public CharacterData targetCharacter;

        [Tooltip("Character that gains the support points. Leave empty to use the player avatar.")]
        public CharacterData sourceCharacter;

        [Tooltip("Amount of support points to add.")]
        public float amount = 10f;

        public override void Execute(ConversationController controller)
        {
            if (targetCharacter == null)
            {
                "ChangeSupportPointsNode: targetCharacter is not assigned.".LogWarning();
                return;
            }

            var brain = GetAndCacheBrain.GetBrain();
            if (brain == null)
            {
                return;
            }

            var gamewide = brain.GetComponent<GamewideContextBrain>();
            var sourceInstance =
                sourceCharacter != null
                    ? gamewide?.FindInstanceByTemplate(sourceCharacter)
                    : gamewide?.GetOrCreateAvatarInstance();

            if (sourceInstance == null)
            {
                "ChangeSupportPointsNode: could not resolve source character instance.".LogWarning();
                return;
            }

            brain.charactersBrain?.IncreaseSupport(sourceInstance, targetCharacter, amount);
        }
    }
}
