using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Math/Number Input")]
[NodeLabel("Outputs a number")]
public class NumberInputNode : SkillNode
{
    [Output]
    public FloatValue Number;

    public float defaultValue = 0f;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "Number")
        {
            FloatValue numberValue = new();
            numberValue.value = defaultValue;
            return numberValue;
        }
        return null;
    }
}
