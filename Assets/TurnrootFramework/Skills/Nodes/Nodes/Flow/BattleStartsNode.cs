using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using XNode;

namespace Turnroot.Skills.Nodes.Flow
{
    /// <summary>
    /// Entry point that fires exactly once when the battle begins.
    /// Use this for permanent-for-the-battle stat changes or setup effects.
    /// For per-combat bonuses (e.g. terrain-based hit/avoid), use <see cref="CombatStartsNode"/> instead.
    /// No combat target is set — use a <see cref="ForEachEnemyNode"/> downstream
    /// to check per-enemy conditions.
    /// </summary>
    [CreateNodeMenu("Flow/Start/Battle Starts")]
    [NodeLabel("Fires once at the start of the whole battle (not per-turn, not per-combat)")]
    public class BattleStartsNode : SkillNode
    {
        [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
        public ExecutionFlow flow;

        public override object GetValue(NodePort port) => null;

        public override void Execute(BattleContext context) { }
    }
}
