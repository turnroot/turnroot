using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Map Raining")]
[NodeLabel("Checks if it is raining on the map")]
public class MapRaining : SkillNode
{
    [Output]
    public BoolValue isRaining;

    [Tooltip("Test value used in editor mode")]
    public bool editorTestValue = false;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "isRaining")
        {
            BoolValue rainingValue = new();

            // In editor mode, return the test value
            if (!Application.isPlaying)
            {
                rainingValue.value = editorTestValue;
            }
            else
            {
                // At runtime, get actual weather state
                if (graph is SkillGraph skillGraph)
                {
                    var contextFromGraph = GetContextFromGraph(skillGraph);
                    if (
                        contextFromGraph != null
                        && contextFromGraph.EnvironmentalConditions != null
                    )
                    {
                        rainingValue.value = contextFromGraph.EnvironmentalConditions.IsRaining;
                    }
                }
            }

            return rainingValue;
        }
        return null;
    }
}
