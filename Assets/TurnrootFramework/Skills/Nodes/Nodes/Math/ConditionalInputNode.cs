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
            if (port.fieldName == nameof(True))
            {
                return new BoolValue { value = True.value };
            }
            else if (port.fieldName == nameof(False))
            {
                return new BoolValue { value = False.value };
            }
            return null;
        }
    }
}
