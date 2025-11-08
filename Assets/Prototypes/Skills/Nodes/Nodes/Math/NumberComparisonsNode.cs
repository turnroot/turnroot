using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

public enum NumberComparisonType
{
    GreaterThan,
    LessThan,
    EqualTo,
    NotEqualTo,
    GreaterThanOrEqualTo,
    LessThanOrEqualTo,
}

[CreateNodeMenu("Math/Number Comparisons")]
[NodeLabel("Compares two numbers, returning True or False")]
public class NumberComparisonsNode : SkillNode
{
    [Input]
    public FloatValue a;

    [Input]
    public FloatValue b;

    [Output]
    public BoolValue result;

    public NumberComparisonType operationType;

    public override object GetValue(NodePort port)
    {
        FloatValue aValue = GetInputValue<FloatValue>("a", a);
        FloatValue bValue = GetInputValue<FloatValue>("b", b);
        BoolValue resultValue = new();

        switch (operationType)
        {
            case NumberComparisonType.GreaterThan:
                resultValue.value = aValue.value > bValue.value;
                break;
            case NumberComparisonType.LessThan:
                resultValue.value = aValue.value < bValue.value;
                break;
            case NumberComparisonType.EqualTo:
                resultValue.value = Mathf.Approximately(aValue.value, bValue.value);
                break;
            case NumberComparisonType.NotEqualTo:
                resultValue.value = !Mathf.Approximately(aValue.value, bValue.value);
                break;
            case NumberComparisonType.GreaterThanOrEqualTo:
                resultValue.value = aValue.value >= bValue.value;
                break;
            case NumberComparisonType.LessThanOrEqualTo:
                resultValue.value = aValue.value <= bValue.value;
                break;
        }

        return resultValue;
    }
}
