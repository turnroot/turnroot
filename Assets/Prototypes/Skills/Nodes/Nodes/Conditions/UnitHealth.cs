using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Unit Health")]
[NodeLabel("Gets the health value of a unit")]
public class UnitHealth : SkillNode
{
    [Output]
    public FloatValue health;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "health")
        {
            FloatValue healthValue = new();
            // TODO: Implement actual unit health retrieval logic
            // For now, returns 100 as a placeholder
            healthValue.value = 100f;
            return healthValue;
        }
        return null;
    }
}
