using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Math
{
    /// <summary>
    /// Defines the types of numerical comparison operations available.
    /// </summary>
    public enum NumberComparisonType
    {
        GreaterThan,
        LessThan,
        EqualTo,
        NotEqualTo,
        GreaterThanOrEqualTo,
        LessThanOrEqualTo,
    }

    /// <summary>
    /// Compares two numeric values and returns a boolean result based on the comparison type.
    /// </summary>
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
            FloatValue aValue = GetInputValue("a", a);
            FloatValue bValue = GetInputValue("b", b);
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
}
