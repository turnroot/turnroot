using Turnroot.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Math
{
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
                FloatValue numberValue = new()
                {
                    value = defaultValue
                };
                return numberValue;
            }
            return null;
        }
    }
}
