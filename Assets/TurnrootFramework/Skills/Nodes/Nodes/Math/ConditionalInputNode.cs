using XNode;

namespace Turnroot.Skills.Nodes.Math
{
    /// <summary>
    /// Provides constant boolean values as outputs.
    /// The <c>True</c> port always outputs <c>true</c>; the <c>False</c> port always outputs <c>false</c>.
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
            if (port.fieldName == nameof(True))
            {
                return new BoolValue { value = true };
            }
            else if (port.fieldName == nameof(False))
            {
                return new BoolValue { value = false };
            }
            return null;
        }
    }
}
