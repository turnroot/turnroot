using Turnroot.Utilities;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    /// <summary>
    /// Check if the unit attacked any targets this turn
    /// </summary>
    [CreateNodeMenu("Conditions/Combat/Targets Attacked This Turn")]
    [NodeLabel("Checks if the unit attacked any targets this turn and how many")]
    public class TargetsAttackedThisTurn : SkillNode
    {
        [Output]
        public BoolValue UnitAttacked;

        [Output]
        public FloatValue AttackCount;

        public override object GetValue(NodePort port)
        {
            var skillGraph = graph as SkillGraph;
            if (skillGraph == null || !Application.isPlaying)
            {
                return port.fieldName switch
                {
                    "UnitAttacked" => new BoolValue { value = false },
                    "AttackCount" => new FloatValue { value = 0f },
                    _ => null,
                };
            }

            var context = GetContextFromGraph(skillGraph);
            var unit = ConditionHelpers.GetCharacterFromContext(
                context,
                ConditionHelpers.CharacterSource.Unit
            );

            if (unit == null)
            {
                "TargetsAttackedThisTurn: Could not retrieve unit from context".LogWarning();
                return port.fieldName switch
                {
                    "UnitAttacked" => new BoolValue { value = false },
                    "AttackCount" => new FloatValue { value = 0f },
                    _ => null,
                };
            }

            return port.fieldName switch
            {
                "UnitAttacked" => new BoolValue { value = unit.HasAttackedTargetThisTurn },
                "AttackCount" => new FloatValue { value = unit.TargetsAttackedThisTurnCount },
                _ => null,
            };
        }
    }
}
