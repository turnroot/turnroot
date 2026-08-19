using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Conversations.Branching
{
    /// <summary>
    /// Conversation action node that unlocks a battle by setting a SceneFlowBrain custom flag.
    /// The same flag key must be used as a condition on the transition leading to the battle
    /// scene. Publishes a brain event so UI notifications can react to the unlock.
    /// </summary>
    [CreateNodeMenu("Conversation/Actions/Unlock Battle")]
    public class UnlockBattleNode : ConversationActionNode
    {
        [Tooltip(
            "Unity scene name of the battle that is being unlocked. Used for the unlock event and to build the default flag key."
        )]
        public string battleSceneName;

        [Tooltip(
            "Custom flag key set on SceneFlowBrain. If empty, defaults to 'battle_unlocked_<battleSceneName>'."
        )]
        public string flagKey;

        public override void Execute(ConversationController controller)
        {
            var brain = GetAndCacheBrain.GetBrain();
            if (brain == null)
            {
                "UnlockBattleNode: could not find Brain.".LogWarning();
                return;
            }

            var sceneFlowBrain = brain.sceneFlowBrain;
            if (sceneFlowBrain == null)
            {
                "UnlockBattleNode: could not find SceneFlowBrain.".LogWarning();
                return;
            }

            if (string.IsNullOrWhiteSpace(battleSceneName))
            {
                "UnlockBattleNode: battleSceneName is not set.".LogWarning();
                return;
            }

            string key = ResolveFlagKey();
            sceneFlowBrain.SetCustomFlag(key, true);
            brain.PublishBattleUnlocked(battleSceneName);
        }

        private string ResolveFlagKey()
        {
            return !string.IsNullOrEmpty(flagKey)
                ? flagKey
                : $"battle_unlocked_{battleSceneName}";
        }
    }
}
