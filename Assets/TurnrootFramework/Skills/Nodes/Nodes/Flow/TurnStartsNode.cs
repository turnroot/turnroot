using XNode;

namespace Turnroot.Skills.Nodes.Flow
{
    /// <summary>
    /// Entry point node that triggers whenever ANY unit's turn starts (player, enemy, or
    /// third-party), for every unit that has this skill — regardless of whose turn it is.
    /// Use this for passive effects that must react to all turn activity, e.g. a poison
    /// that ticks on every unit's turn, or an aura that refreshes each round.
    /// For a trigger that only fires on the skill owner's own turn, use
    /// <see cref="UnitTurnStartsNode"/> instead.
    /// </summary>
    [CreateNodeMenu("Flow/Start/Any Turn Starts")]
    [NodeLabel("Runs for every skill owner when ANY unit's turn starts")]
    public class AnyTurnStartsNode : SkillNode
    {
        [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
        public ExecutionFlow flow;

        public override object GetValue(NodePort port) => null;
    }
}
