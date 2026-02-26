using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    /// <summary>
    /// Condition node that randomly returns true based on a percentage chance.
    /// Useful for stat-based probability checks or simple randomness.
    /// </summary>
    [CreateNodeMenu("Conditions/Misc/Percent Chance")]
    [NodeLabel("Percent Chance")]
    public class PercentChanceNode : SkillNode
    {
        [Input]
        [Tooltip(
            "Percentage chance (0-100) for the condition to be true. Can be driven by a FloatValue node."
        )]
        public FloatValue chance;

        [Output]
        public BoolValue Success;

        public override object GetValue(NodePort port)
        {
            var skillGraph = graph as SkillGraph;
            if (skillGraph == null || !Application.isPlaying)
            {
                return new BoolValue { value = false };
            }

            var chancePort = GetInputPort("chance");
            if (chancePort == null || !chancePort.IsConnected)
            {
                "PercentChanceNode: 'chance' input not provided".LogWarning();
                return new BoolValue { value = false };
            }
            float chanceValue = GetInputFloat("chance", 0f);
            chanceValue = Mathf.Clamp(chanceValue, 0f, 100f);

            float roll = Random.Range(0f, 100f);
            bool result = roll <= chanceValue;

            return new BoolValue { value = result };
        }
    }
}
