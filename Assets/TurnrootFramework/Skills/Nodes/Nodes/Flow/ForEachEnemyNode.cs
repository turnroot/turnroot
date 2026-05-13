using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using XNode;

namespace Turnroot.Skills.Nodes.Flow
{
    /// <summary>
    /// Loop node that executes the downstream graph once for each battlefield enemy.
    /// On each iteration <c>context.Participants.Targets</c> is temporarily set to a
    /// single enemy, so all downstream condition nodes (e.g. EnemyDistanceNode) and
    /// event nodes (e.g. DealDebuffNode) operate on that one enemy individually.
    ///
    /// Use this in non-combat flows (Turn Ends, Unit Moves, Battle Starts) to check
    /// per-enemy conditions and apply effects independently to each enemy that qualifies.
    ///
    /// In combat flows (Unit Attacks / Enemy Attacks), <c>Targets</c> is already
    /// set to the specific enemy being engaged — ForEachEnemy is not needed there.
    /// </summary>
    [CreateNodeMenu("Flow/For Each Enemy")]
    [NodeLabel("Runs the next steps once per battlefield enemy")]
    public class ForEachEnemyNode : SkillNode
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
                "ForEachEnemyNode: Not attached to a SkillGraph".LogWarning();
                return;
            }

            var executor = context.GetCustomData<SkillGraphExecutor>("_executor");
            if (executor == null)
            {
                "ForEachEnemyNode: No executor found in context".LogWarning();
                return;
            }

            var outPort = GetOutputPort("OutFlow");
            if (outPort == null || !outPort.IsConnected)
            {
                return;
            }

            // Snapshot all enemies before iterating — Targets will be replaced per-iteration.
            var allEnemies = new List<CharacterInstance>(context.Participants.Targets);
            if (allEnemies.Count == 0)
            {
                "ForEachEnemyNode: No enemies in Participants.Targets".LogWarning();
                return;
            }

            var originalTargets = context.Participants.Targets;

            // Seed the ancestor set with this node to prevent the subchain from looping
            // back into the ForEachEnemy node itself.
            var ancestors = new HashSet<SkillNode> { this };

            foreach (var enemy in allEnemies)
            {
                if (enemy == null)
                {
                    continue;
                }

                context.Participants.Targets = new List<CharacterInstance> { enemy };

                foreach (var connection in outPort.GetConnections())
                {
                    if (connection.node is SkillNode nextNode)
                    {
                        executor.ExecuteSubchain(nextNode, ancestors);
                    }
                }
            }

            // Mark all OutFlow-connected nodes as visited in the outer executor so that
            // ContinueFromNode (called after this Execute() returns) does not re-run them
            // with the full target list. ForEachEnemyNode exclusively owns its OutFlow.
            foreach (var connection in outPort.GetConnections())
            {
                if (connection.node is SkillNode outNode)
                {
                    executor.MarkVisited(outNode);
                }
            }

            context.Participants.Targets = originalTargets;
        }
    }
}
