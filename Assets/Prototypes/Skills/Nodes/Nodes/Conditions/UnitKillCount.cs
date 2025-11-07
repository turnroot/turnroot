using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Unit/Kill Count")]
[NodeLabel("Gets the unit's kill count")]
public class UnitKillCount : SkillNode
{
    [Output]
    FloatValue value;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "value")
        {
            FloatValue killCountValue = new();
            // TODO: Implement runtime retrieval of kill count
            return killCountValue;
        }
        return null;
    }
}
