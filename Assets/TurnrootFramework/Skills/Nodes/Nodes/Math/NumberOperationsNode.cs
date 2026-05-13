using Turnroot.Utilities;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Math
{
    /// <summary>
    /// Defines the type of mathematical operation to perform on numeric values.
    /// </summary>
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

    /// <summary>
    /// Performs mathematical operations (Add, Subtract, Multiply, etc.) on two numeric values.
    /// </summary>
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
            FloatValue aValue = GetInputValue("a", a);
            FloatValue bValue = GetInputValue("b", b);
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
                    if (Mathf.Approximately(bValue.value, 0f))
                    {
                        "NumberOperationsNode: Division by zero".LogWarning();
                        resultValue.value = 0f;
                    }
                    else
                    {
                        resultValue.value = aValue.value / bValue.value;
                    }
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
}
