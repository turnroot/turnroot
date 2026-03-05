using Turnroot.Utilities;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    /// <summary>
    /// Retrieves the total number of enemies defeated by the unit.
    /// </summary>
    [CreateNodeMenu("Conditions/Unit/Kill Count")]
    [NodeLabel("Gets the unit's kill count")]
    public class UnitKillCountNode : SkillNode
    {
        [Output]
        private FloatValue value;

        public override object GetValue(NodePort port)
        {
            if (port.fieldName != "value")
            {
                return null;
            }

            var skillGraph = graph as SkillGraph;
            if (skillGraph == null || !Application.isPlaying)
            {
                return new FloatValue { value = 0f }; // Default to 0 kills in editor
            }

            var context = GetContextFromGraph(skillGraph);
            var character = ConditionHelpers.GetCharacterFromContext(
                context,
                ConditionHelpers.CharacterSource.Unit
            );

            if (character == null)
            {
                "UnitKillCount: Could not retrieve unit from context".LogWarning();
                return new FloatValue { value = 0f };
            }

            // Return actual kill count from character instance
            return new FloatValue { value = character.TotalKills };
        }
    }
}
