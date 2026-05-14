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
                    context.Unit.UnitInstance,
                    nameof(context.Unit.UnitInstance)
                )
            )
            {
                return;
            }

            var hitPort = GetInputPort("changeHit");
            var avoidPort = GetInputPort("changeAvoid");

            if (
                (hitPort == null || !hitPort.IsConnected)
                && (avoidPort == null || !avoidPort.IsConnected)
            )
            {
                "AffectUnitHitAvoid: Neither 'changeHit' nor 'changeAvoid' is connected — no effect applied".LogWarning();
                return;
            }

            if (hitPort != null && hitPort.IsConnected)
            {
                var changeHitAmount = GetInputFloat("changeHit", 0f);
                var inst = context.Unit.UnitInstance;
                inst?.AddHit(changeHitAmount);
                if (SkillDebug.VerboseExecutionLogs)
                {
                    $"AffectUnitHitAvoid: stored hit now {inst?.CurrentHit}".LogInfo();
                }
            }

            var avoidPort2 = GetInputPort("changeAvoid");
            if (avoidPort2 != null && avoidPort2.IsConnected)
            {
                var changeAvoidAmount = GetInputFloat("changeAvoid", 0f);
                if (SkillDebug.VerboseExecutionLogs)
                {
                    $"AffectUnitHitAvoid: affected unit {context.Unit.UnitInstance.Id} avoid by {changeAvoidAmount}".LogInfo();
                }
                var inst2 = context.Unit.UnitInstance;
                inst2?.AddAvoid(changeAvoidAmount);
                if (SkillDebug.VerboseExecutionLogs)
                {
                    $"AffectUnitHitAvoid: stored avoid now {inst2?.CurrentAvoid}".LogInfo();
                }
            }
        }
    }
}
