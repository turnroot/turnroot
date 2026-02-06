using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    /// <summary>
    /// Condition node that checks if the unit is the one initiating combat (attacking rather than defending).
    /// </summary>
    [CreateNodeMenu("Conditions/Combat/Is Initiating Combat")]
    [NodeLabel("Checks if the unit is initiating combat")]
    public class IsInitiatingCombatNode : SkillNode
    {
        [Output]
        public BoolValue UnitInitiating;

        public override object GetValue(NodePort port)
        {
            if (port.fieldName != "UnitInitiating")
            {
                return null;
            }

            var skillGraph = graph as SkillGraph;
            if (skillGraph == null || !Application.isPlaying)
            {
                return new BoolValue { value = true }; // Default to initiating in editor
            }

            var context = GetContextFromGraph(skillGraph);
            if (context == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("IsInitiatingCombat: Could not retrieve context from graph");
#endif
                return new BoolValue { value = true };
            }

            // Check if unit is initiating combat (stored in CustomData by battle system)
            // Default to true if not set (assume unit is attacking)
            bool isInitiating = context.GetCustomData("IsInitiatingCombat", true);
            return new BoolValue { value = isInitiating };
        }
    }
}
