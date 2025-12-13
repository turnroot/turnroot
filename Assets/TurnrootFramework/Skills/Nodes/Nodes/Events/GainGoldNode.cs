using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using UnityEngine;

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
            if (!ValidateContext(context))
            {
                return;
            }

            int gold = (int)GetInputFloat("goldAmount", testGold);

            context.Brain?.PublishGoldGained(gold);
            Debug.Log($"GainGold: Player gained {gold} gold");
        }
    }
}
