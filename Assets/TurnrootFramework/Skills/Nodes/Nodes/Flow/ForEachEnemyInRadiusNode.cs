using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Flow
{
    /// <summary>
    /// Loop node that executes the downstream graph once for each battlefield enemy within a
    /// given Manhattan-distance radius of the skill caster.
    ///
    /// On each iteration <c>context.Participants.Targets</c> is temporarily set to a
    /// single enemy, so all downstream condition nodes and event nodes operate on that
    /// one enemy individually — identical to <see cref="ForEachEnemyNode"/> but pre-filtered
    /// by proximity.
    ///
    /// Connect a <c>Number Input</c> node to the <c>radius</c> port, or leave it
    /// disconnected to default to 1 tile.
    ///
    /// Use this in non-combat flows (Turn Ends, Unit Moves, Battle Starts) to apply
    /// per-enemy effects only to nearby enemies.
    /// </summary>
    [CreateNodeMenu("Flow/For Each Enemy In Radius")]
    [NodeLabel("Runs the next steps once per nearby battlefield enemy")]
    public class ForEachEnemyInRadiusNode : SkillNode
    {
        [Input(ShowBackingValue.Never, ConnectionType.Override)]
        public ExecutionFlow InFlow;

        [Input(ShowBackingValue.Always, ConnectionType.Override)]
        [Tooltip("Maximum Manhattan distance from the caster. Defaults to 1 if unconnected.")]
        public FloatValue radius;

        [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
        public ExecutionFlow OutFlow;

        public override void Execute(BattleContext context)
        {
            var skillGraph = graph as SkillGraph;
            if (skillGraph == null)
            {
                "ForEachEnemyInRadiusNode: Not attached to a SkillGraph".LogWarning();
                return;
            }

            var executor = context.GetCustomData<SkillGraphExecutor>("_executor");
            if (executor == null)
            {
                "ForEachEnemyInRadiusNode: No executor found in context".LogWarning();
                return;
            }

            var outPort = GetOutputPort("OutFlow");
            if (outPort == null || !outPort.IsConnected)
            {
                return;
            }

            if (context.Unit.UnitInstance == null)
            {
                "ForEachEnemyInRadiusNode: No caster unit in context".LogWarning();
                return;
            }

            float maxRadius = GetInputFloat("radius", 1f);
            var casterPos = context.Unit.UnitInstance.MapGridPosition;

            // Snapshot all enemies and filter by radius.
            var allEnemies = new List<CharacterInstance>(context.Participants.Targets);
            if (allEnemies.Count == 0)
            {
                "ForEachEnemyInRadiusNode: No enemies in Participants.Targets".LogWarning();
                return;
            }

            var enemiesInRadius = new List<CharacterInstance>();
            foreach (var enemy in allEnemies)
            {
                if (enemy == null)
                {
                    continue;
                }

                var enemyPos = enemy.MapGridPosition;
                int distance =
                    Mathf.Abs(casterPos.x - enemyPos.x) + Mathf.Abs(casterPos.y - enemyPos.y);
                if (distance <= maxRadius)
                {
                    enemiesInRadius.Add(enemy);
                }
            }

            if (enemiesInRadius.Count == 0)
            {
                $"ForEachEnemyInRadiusNode: No enemies within radius {maxRadius}".LogInfo();
                return;
            }

            var originalTargets = context.Participants.Targets;

            // Seed the ancestor set with this node to prevent the subchain from looping back.
            var ancestors = new HashSet<SkillNode> { this };

            foreach (var enemy in enemiesInRadius)
            {
                context.Participants.Targets = new List<CharacterInstance> { enemy };

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

            context.Participants.Targets = originalTargets;
        }
    }
}
