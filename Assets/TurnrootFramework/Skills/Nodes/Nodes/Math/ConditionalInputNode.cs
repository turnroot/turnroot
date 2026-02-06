using XNode;

namespace Turnroot.Skills.Nodes.Math
{
    /// <summary>
    /// Provides constant boolean values (True or False) as outputs.
    /// </summary>
    [CreateNodeMenu("Math/Conditional Input")]
    [NodeLabel("Outputs True or False")]
    public class ConditionalInputNode : SkillNode
    {
        [Output]
        public BoolValue True;

        [Output]
        public BoolValue False;

        public override object GetValue(NodePort port)
        {
            if (port.fieldName == "True")
            {
                BoolValue trueValue = new() { value = true };
                return trueValue;
            }
            else if (port.fieldName == "False")
            {
                BoolValue falseValue = new() { value = false };
                return falseValue;
            }
            return null;
        }
    }
}
