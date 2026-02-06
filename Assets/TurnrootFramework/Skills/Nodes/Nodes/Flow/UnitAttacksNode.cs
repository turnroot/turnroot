using XNode;

namespace Turnroot.Skills.Nodes.Flow
{
    /// <summary>
    /// Entry point node that triggers when a unit performs an attack.
    /// </summary>
    [CreateNodeMenu("Flow/Start/Unit Attacks")]
    [NodeLabel("Runs when this unit attacks")]
    public class UnitAttacksNode : SkillNode
    {
        [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
        public ExecutionFlow execOut;

        public override object GetValue(NodePort port) => null;
    }
}
