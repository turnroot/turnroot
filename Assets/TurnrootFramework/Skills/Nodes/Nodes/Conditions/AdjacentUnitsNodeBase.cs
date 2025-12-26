using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    /// <summary>
    /// Base class for adjacent units condition nodes (AdjacentAlliesNode, AdjacentEnemiesNode).
    /// Provides shared adjacent unit counting functionality.
    /// </summary>
    public abstract class AdjacentUnitsNodeBase : SkillNode
    {
        [Output]
        public FloatValue count;

        [Output]
        public BoolValue hasAdjacent;

        /// <summary>
        /// Gets the count of adjacent units from the context.
        /// </summary>
        protected abstract int GetAdjacentCount(
            Gameplay.Combat.FundamentalComponents.Battles.BattleContext context
        );

        /// <summary>
        /// Gets the node name for logging purposes.
        /// </summary>
        protected abstract string NodeName { get; }

        public override object GetValue(NodePort port)
        {
            var skillGraph = graph as SkillGraph;
            if (skillGraph == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"{NodeName}: Could not get SkillGraph");
#endif
                return port.fieldName == "count" ? new FloatValue() : (object)new BoolValue();
            }

            var context = GetContextFromGraph(skillGraph);
            if (context?.Participants?.AdjacentUnits == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"{NodeName}: No adjacent units in context");
#endif
                return port.fieldName == "count" ? new FloatValue() : (object)new BoolValue();
            }

            int adjacentCount = GetAdjacentCount(context);

            return port.fieldName switch
            {
                "count" => new FloatValue { value = adjacentCount },
                "hasAdjacent" => new BoolValue { value = adjacentCount > 0 },
                _ => null,
            };
        }
    }
}
