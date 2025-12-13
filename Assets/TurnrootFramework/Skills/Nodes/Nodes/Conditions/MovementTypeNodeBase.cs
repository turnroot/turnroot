using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    /// <summary>
    /// Base class for movement type condition nodes (IsArmoredNode, IsFlyingNode, IsRidingNode).
    /// Provides shared character movement type checking functionality.
    /// </summary>
    public abstract class MovementTypeNodeBase : SkillNode
    {
        [Output]
        public BoolValue unit;

        [Output]
        public BoolValue enemy;

        [Output]
        public BoolValue adjacentAlly;

        /// <summary>
        /// The movement type to check for.
        /// </summary>
        protected abstract MovementType TargetMovementType { get; }

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

            bool hasMovementType =
                character.CurrentClass.ClassData.Identity.MovementType == TargetMovementType;
            return new BoolValue { value = hasMovementType };
        }
    }
}
