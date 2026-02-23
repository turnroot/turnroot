using XNode;

namespace Turnroot.Skills.Nodes.Math
{
    /// <summary>
    /// Provides a constant numeric value as output.
    /// </summary>
    [CreateNodeMenu("Math/Number Input")]
    [NodeLabel("Outputs a number")]
    public class NumberInputNode : SkillNode
    {
        [Output]
        public FloatValue Number;

        public override object GetValue(NodePort port)
        {
            if (port.fieldName == nameof(Number))
            {
                // FloatValue is a struct, so just return its stored value (default is 0)
                return new FloatValue { value = Number.value };
            }
            return null;
        }
    }
}
