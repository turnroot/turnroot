using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Map Raining")]
[NodeLabel("Checks if it is raining on the map")]
public class MapRaining : SkillNode
{
    [Output]
    public BoolValue isRaining;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "isRaining")
        {
            BoolValue rainingValue = new();
            // TODO: Implement actual weather check logic
            // For now, returns false as a placeholder
            rainingValue.value = false;
            return rainingValue;
        }
        return null;
    }
}
