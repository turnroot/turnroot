using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

public enum NumberOperationType
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Modulo,
    RoundUp,
    RoundDown,
}

[CreateNodeMenu("Math/Number Operations")]
[NodeLabel("Performs operations on two numbers")]
public class NumberOperationsNode : SkillNode
{
    [Input(ShowBackingValue.Always, ConnectionType.Override)]
    public FloatValue a;

    [Input(ShowBackingValue.Always, ConnectionType.Override)]
    public FloatValue b;

    [Output]
    public FloatValue result;

    public NumberOperationType operationType;

    public override object GetValue(NodePort port)
    {
        FloatValue aValue = GetInputValue<FloatValue>("a", a);
        FloatValue bValue = GetInputValue<FloatValue>("b", b);
        FloatValue resultValue = new();

        switch (operationType)
        {
            case NumberOperationType.Add:
                resultValue.value = aValue.value + bValue.value;
                break;
            case NumberOperationType.Subtract:
                resultValue.value = aValue.value - bValue.value;
                break;
            case NumberOperationType.Multiply:
                resultValue.value = aValue.value * bValue.value;
                break;
            case NumberOperationType.Divide:
                resultValue.value = aValue.value / bValue.value;
                break;
            case NumberOperationType.Modulo:
                resultValue.value = aValue.value % bValue.value;
                break;
            case NumberOperationType.RoundUp:
                resultValue.value = Mathf.Ceil(aValue.value);
                break;
            case NumberOperationType.RoundDown:
                resultValue.value = Mathf.Floor(aValue.value);
                break;
        }

        return resultValue;
    }
}
