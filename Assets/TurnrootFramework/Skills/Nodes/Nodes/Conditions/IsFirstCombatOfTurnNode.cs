using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    [CreateNodeMenu("Conditions/Combat/Is First Combat Of Turn")]
    [NodeLabel("Checks if this is the unit's first combat this turn")]
    public class IsFirstCombatOfTurnNode : SkillNode
    {
        [Output]
        public BoolValue IsFirstCombat;

        public override object GetValue(NodePort port)
        {
            if (port.fieldName != "IsFirstCombat")
            {
                return null;
            }

            var skillGraph = graph as SkillGraph;
            if (skillGraph == null || !Application.isPlaying)
            {
                return new BoolValue { value = true }; // Default to first combat in editor
            }

            var context = GetContextFromGraph(skillGraph);
            if (context == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("IsFirstCombatOfTurn: Could not retrieve context from graph");
#endif
                return new BoolValue { value = true };
            }

            // Check if this is the unit's first combat this turn
            var unit = context.Unit.UnitInstance;
            if (unit == null)
            {
                return new BoolValue { value = true };
            }

            // CombatsThisTurn tracks combats completed, so 0 means this is the first combat
            bool isFirstCombat = unit.CombatsThisTurn == 0;
            return new BoolValue { value = isFirstCombat };
        }
    }
}
