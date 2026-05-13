using XNode;

namespace Turnroot.Skills.Nodes.Flow
{
    /// <summary>
    /// Entry point that fires when this unit completes an attack.
    /// <c>context.Participants.Targets</c> is already set to the combat target(s) —
    /// no ForEachEnemyNode is needed for single-target checks.
    /// </summary>
    [CreateNodeMenu("Flow/Start/Unit Attacks")]
    [NodeLabel("Runs when this unit attacks (combat; target enemy is already set)")]
    public class UnitAttacksNode : SkillNode
    {
        [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
        public ExecutionFlow execOut;

        public override object GetValue(NodePort port) => null;
    }
}
