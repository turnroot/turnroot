using XNode;

namespace Turnroot.Skills.Nodes.Flow
{
    /// <summary>
    /// Entry point node that triggers only when the unit that owns this skill has their own
    /// turn start. Use this for self-affecting per-turn effects: regeneration, cooldown
    /// ticking, stance resets, etc.
    /// For a trigger that fires whenever any unit's turn starts (not just the owner's),
    /// use <see cref="AnyTurnStartsNode"/> instead.
    /// </summary>
    [CreateNodeMenu("Flow/Start/Unit Turn Starts")]
    [NodeLabel("Runs only when the skill owner's own turn starts")]
    public class UnitTurnStartsNode : SkillNode
    {
        [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
        public ExecutionFlow flow;

        public override object GetValue(NodePort port) => null;
    }
}
