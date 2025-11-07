using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Counters/Skill Use Count")]
[NodeLabel("Gets the number of times the skill has been used in this battle")]
public class SkillUseCount : SkillNode
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
                Debug.LogError("SkillExecutionContext not found in graph!");
                return null;
            }

            FloatValue turnCountValue = new() { value = contextFromGraph.SkillUseCount };
            return turnCountValue;
        }
        return null;
    }
}
