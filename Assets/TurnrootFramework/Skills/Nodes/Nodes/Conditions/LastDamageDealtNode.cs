using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    /// <summary>
    /// Outputs the amount of damage this unit dealt on its most recent attack strike.
    /// Updated every time <c>AttackTarget</c> fires for this unit. Returns 0 on a miss
    /// or before any attack has occurred this combat.
    ///
    /// Primary use-case: Sol — heal HP equal to damage dealt.
    /// Graph: UnitAttacksNode → FlowIf(PercentChanceNode(25)) → AffectUnitStatNode(Health, +LastDamageDealtNode.Value)
    /// </summary>
    [CreateNodeMenu("Conditions/Combat/Last Damage Dealt")]
    [NodeLabel("Damage dealt on the unit's most recent strike (0 on miss)")]
    public class LastDamageDealtNode : SkillNode
    {
        [Output]
        public FloatValue Value;

        public override object GetValue(NodePort port)
        {
            if (port.fieldName != "Value")
            {
                return null;
            }

            var skillGraph = graph as SkillGraph;
            if (skillGraph == null || !Application.isPlaying)
            {
                return new FloatValue { value = 0f };
            }

            var context = GetContextFromGraph(skillGraph);
            if (context == null || context.Unit.UnitInstance == null)
            {
                return new FloatValue { value = 0f };
            }

            float damage = context.GetCustomData(
                $"LastDamageDealt_{context.Unit.UnitInstance.Id}",
                0f
            );
            return new FloatValue { value = damage };
        }
    }
}
