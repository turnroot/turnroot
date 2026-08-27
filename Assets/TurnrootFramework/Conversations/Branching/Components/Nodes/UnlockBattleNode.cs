using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Conversations.Branching
{
    /// <summary>
    /// Conversation action node that unlocks a battle by setting 'battle_unlocked_<battleSceneName>'.
    /// The same flag key must be used as a condition on the transition leading to the battle
    /// scene. Publishes a brain event so UI notifications can react to the unlock.
    /// </summary>
    [CreateNodeMenu("Conversation/Actions/Unlock Battle")]
    public class UnlockBattleNode : ConversationActionNode
    {
        [Tooltip(
            "SceneFlowGraph scene ID of the battle that is being unlocked"
        )]
        public string battleSceneId;

        public override void Execute(ConversationController controller)
        {
            var brain = GetAndCacheBrain.GetBrain();
            var sceneFlowBrain = brain.sceneFlowBrain;

            if (string.IsNullOrWhiteSpace(battleSceneId))
            {
                "UnlockBattleNode: battleSceneId is not set.".LogError();
                return;
            }

            sceneFlowBrain.SetBattleUnlocked(battleSceneId, true);
            brain.PublishBattleUnlocked(battleSceneId);
        }
    }
}
