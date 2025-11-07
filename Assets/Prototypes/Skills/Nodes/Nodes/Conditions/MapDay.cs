using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Map Day")]
[NodeLabel("Checks if it is daytime on the map")]
public class MapDay : SkillNode
{
    [Output]
    public BoolValue isDay;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "isDay")
        {
            BoolValue dayValue = new();
            // TODO: Implement actual time of day check logic
            // For now, returns true as a placeholder
            dayValue.value = true;
            return dayValue;
        }
        return null;
    }
}
