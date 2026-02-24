using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Skills; // for SkillDebug
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
                "AffectUnitHitAvoid: 'changeHit' input not provided".LogWarning();
                return;
            }
            var changeHitAmount = GetInputFloat("changeHit", 0f);

            // update runtime hit value instead of treating it as a stat
            var inst = context.Unit.UnitInstance;
            inst?.AddHit(changeHitAmount);
            if (SkillDebug.VerboseExecutionLogs)
            {
                $"AffectUnitHitAvoid: stored hit now {inst?.CurrentHit}".LogInfo();
            }

            var avoidPort = GetInputPort("changeAvoid");
            if (avoidPort == null || !avoidPort.IsConnected)
            {
                "AffectUnitHitAvoid: 'changeAvoid' input not provided".LogWarning();
                return;
            }
            var changeAvoidAmount = GetInputFloat("changeAvoid", 0f);
            if (changeAvoidAmount != 0f)
            {
                $"AffectUnitHitAvoid: affected unit {context.Unit.UnitInstance.Id} avoid by {changeAvoidAmount}".LogInfo();
            }
            else
            {
                $"AffectUnitHitAvoid: avoid change evaluated to 0 for unit {context.Unit.UnitInstance.Id}".LogInfo();
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
