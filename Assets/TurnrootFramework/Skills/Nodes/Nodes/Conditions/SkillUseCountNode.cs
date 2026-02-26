using Turnroot.Utilities;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    /// <summary>
    /// Retrieves the number of times the current skill has been used during the battle.
    /// </summary>
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
                    "BattleContext not found in graph!".LogError();
                    return null;
                }

                if (contextFromGraph.Skill.CurrentSkill == null)
                {
                    "CurrentSkill is null in BattleContext!".LogError();
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
