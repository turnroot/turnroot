using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Neutral/Affect Unit Hit|Avoid")]
    [NodeLabel("Modifies hit/avoid value on the executing unit")]
    public class AffectUnitHitAvoid : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Input]
        public FloatValue changeHit;

        [Input]
        public FloatValue changeAvoid;

        [Tooltip("Test value used in editor mode")]
        public float testChangeHit = 5f;

        [Tooltip("Test value used in editor mode")]
        public float testChangeAvoid = 5f;

        public override void Execute(BattleContext context)
        {
            if (
                !ValidationHelper.ValidateNotNull(
                    context?.UnitInstance,
                    nameof(context.UnitInstance)
                )
            )
            {
                return;
            }

            var changeHitAmount = GetInputFloat("changeHit", testChangeHit);
            ApplyStatChange(
                context.UnitInstance,
                "HitAvoid",
                false,
                changeHitAmount,
                "AffectUnitHitAvoid"
            );

            var changeAvoidAmount = GetInputFloat("changeAvoid", testChangeAvoid);
            ApplyStatChange(
                context.UnitInstance,
                "HitAvoid",
                false,
                changeAvoidAmount,
                "AffectUnitHitAvoid"
            );
        }
    }
}
