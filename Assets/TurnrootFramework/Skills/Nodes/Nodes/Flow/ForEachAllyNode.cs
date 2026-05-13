using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using XNode;

namespace Turnroot.Skills.Nodes.Flow
{
    /// <summary>
    /// Loop node that executes the downstream graph once for each battlefield ally.
    /// On each iteration <c>context.Unit.UnitInstance</c> is temporarily swapped to the
    /// current ally AND <c>context.Participants.Allies</c> is narrowed to that single ally,
    /// so all downstream nodes operate on that ally individually.
    ///
    /// This means:
    /// - Effect nodes that target <c>Unit.UnitInstance</c> (e.g. <see cref="Events.BuffUnitNode"/>,
    ///   <see cref="Events.AffectUnitStatNode"/>, <see cref="Events.CureDebuffNode"/>) will apply
    ///   to each ally in turn.
    /// - Condition nodes using <see cref="ConditionHelpers.CharacterSource.Ally"/> (e.g.
    ///   <see cref="Conditions.HasDebuffNode.AllyHasDebuff"/>) will evaluate against the current ally.
    /// - Stat nodes with CharacterTarget = Ally also read the current ally.
    ///
    /// The original <c>Unit.UnitInstance</c> (the skill caster) is fully restored after the loop.
    ///
    /// Use this in non-combat flows (Turn Ends, Turn Starts, Battle Starts) to apply
    /// per-ally conditional effects across the whole team.
    ///
    /// NOTE: If you need to reference the original caster inside the loop (e.g. for a stat
    /// comparison between caster and ally), store the caster identity in a condition node
    /// upstream of this loop before entering it.
    /// </summary>
    [CreateNodeMenu("Flow/For Each Ally")]
    [NodeLabel("Runs the next steps once per battlefield ally")]
    public class ForEachAllyNode : SkillNode
    {
        [Input(ShowBackingValue.Never, ConnectionType.Override)]
        public ExecutionFlow InFlow;

        [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
        public ExecutionFlow OutFlow;

        public override void Execute(BattleContext context)
        {
            var skillGraph = graph as SkillGraph;
            if (skillGraph == null)
            {
                "ForEachAllyNode: Not attached to a SkillGraph".LogWarning();
                return;
            }

            var executor = context.GetCustomData<SkillGraphExecutor>("_executor");
            if (executor == null)
            {
                "ForEachAllyNode: No executor found in context".LogWarning();
                return;
            }

            var outPort = GetOutputPort("OutFlow");
            if (outPort == null || !outPort.IsConnected)
            {
                return;
            }

            if (context.Participants.Allies == null || context.Participants.Allies.Count == 0)
            {
                "ForEachAllyNode: No allies in Participants.Allies".LogWarning();
                return;
            }

            // Snapshot all allies before iterating — both Allies and Unit.UnitInstance will change per-iteration.
            var allAllies = new List<CharacterInstance>(context.Participants.Allies);

            var originalAllies = context.Participants.Allies;
            var originalUnit = context.Unit.UnitInstance;

            // Seed the ancestor set with this node to prevent the subchain from looping back.
            var ancestors = new HashSet<SkillNode> { this };

            foreach (var ally in allAllies)
            {
                if (ally == null)
                {
                    continue;
                }

                // Swap both so all downstream nodes see this ally as "the unit".
                context.Unit.UnitInstance = ally;
                context.Participants.Allies = new List<CharacterInstance> { ally };

                foreach (var connection in outPort.GetConnections())
                {
                    if (connection.node is SkillNode nextNode)
                    {
                        executor.ExecuteSubchain(nextNode, ancestors);
                    }
                }
            }

            // Mark all OutFlow-connected nodes as visited so ContinueFromNode doesn't re-run them.
            foreach (var connection in outPort.GetConnections())
            {
                if (connection.node is SkillNode outNode)
                {
                    executor.MarkVisited(outNode);
                }
            }

            // Restore original state.
            context.Unit.UnitInstance = originalUnit;
            context.Participants.Allies = originalAllies;
        }
    }
}
