using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Skills.Nodes;
using Turnroot.Utilities;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Neutral/Gain Gold")]
    [NodeLabel("Grants gold to the player")]
    public class GainGoldNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Input]
        [Tooltip("The amount of gold to gain")]
        public FloatValue goldAmount;

        [Tooltip("Test value for gold in editor mode")]
        public float testGold = 100f;

        public override void Execute(BattleContext context)
        {
            if (context == null)
            {
                Debug.LogWarning("GainGold: No context provided");
                return;
            }

            int gold = (int)GetInputFloat("goldAmount", testGold);

            var brain = GetBrain.Get();
            if (brain != null)
            {
                brain.InvokeGoldGained(gold);
                Debug.Log($"GainGold: Player gained {gold} gold");
            }
            else
            {
                Debug.LogWarning("GainGold: Could not find Brain to invoke event");
            }
        }
    }
}
