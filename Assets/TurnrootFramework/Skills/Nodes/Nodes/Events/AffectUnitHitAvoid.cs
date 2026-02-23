using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    /// <summary>
    /// Modifies the hit and avoid stat values on the executing unit.
    /// </summary>
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

        public override void Execute(BattleContext context)
        {
            if (
                !ValidationHelper.ValidateNotNull(
                    context?.Unit.UnitInstance,
                    nameof(context.Unit.UnitInstance)
                )
            )
            {
                return;
            }

            var hitPort = GetInputPort("changeHit");
            if (hitPort == null || !hitPort.IsConnected)
            {
                Debug.LogWarning("AffectUnitHitAvoid: 'changeHit' input not provided");
                return;
            }
            var changeHitAmount = GetInputFloat("changeHit", 0f);
            ApplyStatChange(
                context.Unit.UnitInstance,
                "HitAvoid",
                false,
                changeHitAmount,
                "AffectUnitHitAvoid"
            );

            var avoidPort = GetInputPort("changeAvoid");
            if (avoidPort == null || !avoidPort.IsConnected)
            {
                Debug.LogWarning("AffectUnitHitAvoid: 'changeAvoid' input not provided");
                return;
            }
            var changeAvoidAmount = GetInputFloat("changeAvoid", 0f);
            ApplyStatChange(
                context.Unit.UnitInstance,
                "HitAvoid",
                false,
                changeAvoidAmount,
                "AffectUnitHitAvoid"
            );
        }
    }
}
