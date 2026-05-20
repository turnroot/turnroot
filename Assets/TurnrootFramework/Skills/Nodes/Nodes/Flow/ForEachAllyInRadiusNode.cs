using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Flow
{
    /// <summary>
    /// Loop node that executes the downstream graph once for each battlefield ally within a
    /// given Manhattan-distance radius of the skill caster.
    ///
    /// On each iteration <c>context.Unit.UnitInstance</c> is swapped to the current ally
    /// AND <c>context.Participants.Allies</c> is narrowed to that single ally — identical
    /// to <see cref="ForEachAllyNode"/> but pre-filtered by proximity.
    ///
    /// This means effect nodes (BuffUnitNode, AffectUnitStatNode, CureDebuffNode) and
    /// condition nodes using CharacterSource.Ally all operate on the nearby ally in turn.
    ///
    /// Connect a <c>Number Input</c> node to the <c>radius</c> port, or leave it
    /// disconnected to default to 1 tile.
    ///
    /// The original <c>Unit.UnitInstance</c> (the skill caster) is fully restored after the loop.
    /// </summary>
    [CreateNodeMenu("Flow/For Each Ally In Radius")]
    [NodeLabel("Runs the next steps once per nearby battlefield ally")]
    public class ForEachAllyInRadiusNode : SkillNode
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
                "ForEachAllyInRadiusNode: Not attached to a SkillGraph".LogWarning();
                return;
            }

            var executor = context.GetCustomData<SkillGraphExecutor>("_executor");
            if (executor == null)
            {
                "ForEachAllyInRadiusNode: No executor found in context".LogWarning();
                return;
            }

            var outPort = GetOutputPort("OutFlow");
            if (outPort == null || !outPort.IsConnected)
            {
                return;
            }

            if (context.Unit.UnitInstance == null)
            {
                "ForEachAllyInRadiusNode: No caster unit in context".LogWarning();
                return;
            }

            if (context.Participants.Allies == null || context.Participants.Allies.Count == 0)
            {
                "ForEachAllyInRadiusNode: No allies in Participants.Allies".LogWarning();
                return;
            }

            float maxRadius = GetInputFloat("radius", 1f);
            var casterPos = context.Unit.UnitInstance.MapGridPosition;

            // Snapshot all allies and filter by radius.
            var allAllies = new List<CharacterInstance>(context.Participants.Allies);
            var alliesInRadius = new List<CharacterInstance>();
            foreach (var ally in allAllies)
            {
                if (ally == null)
                {
                    continue;
                }

                var allyPos = ally.MapGridPosition;
                int distance =
                    Mathf.Abs(casterPos.x - allyPos.x) + Mathf.Abs(casterPos.y - allyPos.y);
                if (distance <= maxRadius)
                {
                    alliesInRadius.Add(ally);
                }
            }

            if (alliesInRadius.Count == 0)
            {
                $"ForEachAllyInRadiusNode: No allies within radius {maxRadius}".LogInfo();
                return;
            }

            var originalAllies = context.Participants.Allies;
            var originalUnit = context.Unit.UnitInstance;

            // Seed the ancestor set with this node to prevent the subchain from looping back.
            var ancestors = new HashSet<SkillNode> { this };

            foreach (var ally in alliesInRadius)
            {
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
