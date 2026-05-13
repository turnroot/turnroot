using XNode;

namespace Turnroot.Skills.Nodes.Flow
{
    /// <summary>
    /// Triggers when this unit (a player ally) defeats an enemy. Fired via
    /// <c>UnitDefeatedEvent</c> on the typed Brain event bus. During execution,
    /// <c>context.Participants.Targets[0]</c> is the defeated enemy; the owner unit is
    /// <c>context.Unit.UnitInstance</c>. Does NOT fire for enemy-kills-player events.
    /// </summary>
    [CreateNodeMenu("Flow/Start/Enemy Defeated")]
    [NodeLabel("Runs when this unit defeats an enemy (target = defeated enemy)")]
    public class EnemyDefeatedNode : SkillNode
    {
        [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
        public ExecutionFlow flow;

        public override object GetValue(NodePort port) => null;
    }
}
