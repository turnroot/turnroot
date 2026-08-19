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
        [Tooltip("Character toward whom support points are increased. Required.")]
        public CharacterData towardCharacter;

        [Tooltip("Character whose support relationship is updated. Leave empty to use the player avatar.")]
        public CharacterData supporterCharacter;

        [Tooltip("Amount of support points to add.")]
        public float amount = 10f;

        public override void Execute(ConversationController controller)
        {
            if (towardCharacter == null)
            {
                "ChangeSupportPointsNode: towardCharacter is not assigned.".LogWarning();
                return;
            }

            var brain = GetAndCacheBrain.GetBrain();
            if (brain == null)
            {
                "ChangeSupportPointsNode: could not find Brain.".LogWarning();
                return;
            }

            var gamewide = brain.gamewideContextBrain;
            var supporterInstance =
                supporterCharacter != null
                    ? gamewide?.FindInstanceByTemplate(supporterCharacter)
                    : gamewide?.GetOrCreateAvatarInstance();

            if (supporterInstance == null)
            {
                "ChangeSupportPointsNode: could not resolve supporter character instance.".LogWarning();
                return;
            }

            brain.charactersBrain?.IncreaseSupport(supporterInstance, towardCharacter, amount);
        }
    }
}
