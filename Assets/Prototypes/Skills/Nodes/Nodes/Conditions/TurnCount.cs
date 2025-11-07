using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Turn Count")]
[NodeLabel("Gets battle current turn count")]
public class TurnCount : SkillNode
{
    [Output]
    FloatValue value;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "value")
        {
            FloatValue turnCountValue = new();
            // TODO: Implement runtime retrieval of turn count
            return turnCountValue;
        }
        return null;
    }
}
