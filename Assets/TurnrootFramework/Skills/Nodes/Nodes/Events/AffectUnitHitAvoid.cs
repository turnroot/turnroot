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

        [Output]
        public ExecutionFlow OutFlow;

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
                inst?.AddCombatHitBonus(changeHitAmount);
                if (SkillDebug.VerboseExecutionLogs)
                {
                    $"AffectUnitHitAvoid: combat hit bonus now {inst?.CurrentHit}".LogInfo();
                }
            }

            if (avoidPort != null && avoidPort.IsConnected)
            {
                var changeAvoidAmount = GetInputFloat("changeAvoid", 0f);
                var inst = context.Unit.UnitInstance;
                inst?.AddCombatAvoidBonus(changeAvoidAmount);
                if (SkillDebug.VerboseExecutionLogs)
                {
                    $"AffectUnitHitAvoid: affected unit {inst?.Id} avoid by {changeAvoidAmount}, bonus now {inst?.CurrentAvoid}".LogInfo();
                }
            }
        }
    }
}
