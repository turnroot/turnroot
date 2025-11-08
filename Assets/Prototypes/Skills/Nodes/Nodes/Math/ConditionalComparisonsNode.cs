using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

public enum BooleanComparisonType
{
    And,
    Or,
    NotAnd,
    NotOr,
    Equal,
    NotEqual,
}

[CreateNodeMenu("Math/Conditional Comparisons")]
[NodeLabel("Compares two True/False values based on the selected operation")]
public class ConditionalComparisonsNode : SkillNode
{
    [Input]
    public BoolValue a;

    [Input]
    public BoolValue b;

    [Output]
    public BoolValue result;

    public BooleanComparisonType operationType;

    public override object GetValue(NodePort port)
    {
        BoolValue aValue = GetInputValue<BoolValue>("a", a);
        BoolValue bValue = GetInputValue<BoolValue>("b", b);
        BoolValue resultValue = new();

        switch (operationType)
        {
            case BooleanComparisonType.And:
                resultValue.value = aValue.value && bValue.value;
                break;
            case BooleanComparisonType.Or:
                resultValue.value = aValue.value || bValue.value;
                break;
            case BooleanComparisonType.NotAnd:
                resultValue.value = !(aValue.value && bValue.value);
                break;
            case BooleanComparisonType.NotOr:
                resultValue.value = !(aValue.value || bValue.value);
                break;
            case BooleanComparisonType.Equal:
                resultValue.value = aValue.value == bValue.value;
                break;
            case BooleanComparisonType.NotEqual:
                resultValue.value = aValue.value != bValue.value;
                break;
        }

        return resultValue;
    }
}
