using Turnroot.GameSettings;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    /// <summary>
    /// Unified node for checking character movement types.
    /// Consolidates IsArmoredNode, IsFlyingNode, and IsRidingNode into a single configurable node.
    /// Use this node instead of the individual movement type nodes for new skill graphs.
    /// </summary>
    [CreateNodeMenu("Conditions/Status/Movement Type Check")]
    [NodeLabel("Checks if character has specified movement type")]
    public class MovementTypeCheckNode : SkillNode
    {
        /// <summary>
        /// The movement type to check for.
        /// </summary>
        [Tooltip("The movement type to check for")]
        public MovementType targetMovementType = MovementType.Infantry;

        [Output]
        public BoolValue unit;

        [Output]
        public BoolValue enemy;

        [Output]
        public BoolValue adjacentAlly;

        public override object GetValue(NodePort port)
        {
            var skillGraph = graph as SkillGraph;
            if (skillGraph == null || !Application.isPlaying)
            {
                return new BoolValue { value = false };
            }

            var context = GetContextFromGraph(skillGraph);
            if (context == null)
            {
                return new BoolValue { value = false };
            }

            var characterSource = port.fieldName switch
            {
                "unit" => ConditionHelpers.CharacterSource.Unit,
                "enemy" => ConditionHelpers.CharacterSource.Enemy,
                "adjacentAlly" => ConditionHelpers.CharacterSource.Ally,
                _ => (ConditionHelpers.CharacterSource?)null,
            };

            if (!characterSource.HasValue)
            {
                return new BoolValue { value = false };
            }

            var character = ConditionHelpers.GetCharacterFromContext(
                context,
                characterSource.Value
            );
            if (character == null)
            {
                return new BoolValue { value = false };
            }

            var movementType =
                character.CurrentClass?.ClassData.Identity.MovementType ?? MovementType.Infantry;
            bool matches = movementType == targetMovementType;

            return new BoolValue { value = matches };
        }
    }
}
