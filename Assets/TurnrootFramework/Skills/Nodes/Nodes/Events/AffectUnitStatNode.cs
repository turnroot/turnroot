using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    /// <summary>
    /// Modifies a stat value on the unit executing the skill.
    /// </summary>
    [CreateNodeMenu("Events/Neutral/Affect Unit Stat")]
    [NodeLabel("Modifies a stat value on the executing unit")]
    public class AffectUnitStatNode : SkillNode
    {
        [Tooltip("The stat to modify")]
        public string selectedStat = "Health";
        public bool isBoundedStat = true;

        [Input]
        public ExecutionFlow executionIn;

        [Output]
        public ExecutionFlow OutFlow;

        [Input]
        [Tooltip("The amount to change the stat by (positive or negative)")]
        public FloatValue change;

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

            var changePort = GetInputPort("change");
            if (changePort == null || !changePort.IsConnected)
            {
                "AffectUnitStatNode: 'change' input not provided".LogWarning();
                return;
            }
            float changeAmount = GetInputFloat("change", 0f);
            ApplyStatChange(
                context.Unit.UnitInstance,
                selectedStat,
                isBoundedStat,
                changeAmount,
                "AffectUnitStat"
            );
        }
    }
}
