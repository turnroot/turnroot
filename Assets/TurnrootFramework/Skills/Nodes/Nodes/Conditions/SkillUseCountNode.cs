using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    [CreateNodeMenu("Conditions/Counters/Skill Use Count")]
    [NodeLabel("Gets the number of times the skill has been used in this battle")]
    public class SkillUseCountNode : SkillNode
    {
        [Output]
        FloatValue value;

        public override object GetValue(NodePort port)
        {
            if (port.fieldName == "value" && graph is SkillGraph skillGraph)
            {
                var contextFromGraph = GetContextFromGraph(skillGraph);
                if (contextFromGraph == null)
                {
                    Debug.LogError("BattleContext not found in graph!");
                    return null;
                }

                if (contextFromGraph.Skill.CurrentSkill == null)
                {
                    Debug.LogError("CurrentSkill is null in BattleContext!");
                    return null;
                }

                int count = 0;
                if (
                    contextFromGraph.Skill.SkillUseCount != null
                    && contextFromGraph.Skill.SkillUseCount.TryGetValue(
                        contextFromGraph.Skill.CurrentSkill,
                        out count
                    )
                )
                {
                    // Found the count
                }

                FloatValue skillCountValue = new() { value = count };
                return skillCountValue;
            }
            return null;
        }
    }
}
