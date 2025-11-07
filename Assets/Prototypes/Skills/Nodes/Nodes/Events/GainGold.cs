using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Gain Gold")]
    [NodeLabel("Grants gold to the player")]
    public class GainGold : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Input]
        [Tooltip("The amount of gold to gain")]
        public FloatValue goldAmount;

        [Tooltip("Test value for gold in editor mode")]
        public float testGold = 100f;

        public override void Execute(SkillExecutionContext context)
        {
            if (context == null)
            {
                Debug.LogWarning("GainGold: No context provided");
                return;
            }

            // Get the gold value
            float gold = testGold;
            var goldPort = GetInputPort("goldAmount");
            if (goldPort != null && goldPort.IsConnected)
            {
                var inputValue = goldPort.GetInputValue();
                if (inputValue is FloatValue floatValue)
                {
                    gold = floatValue.value;
                }
            }

            // TODO: Integrate with actual gold/currency system
            context.SetCustomData("GoldGained", gold);
            Debug.Log($"GainGold: Player gained {gold} gold");
        }
    }
}
