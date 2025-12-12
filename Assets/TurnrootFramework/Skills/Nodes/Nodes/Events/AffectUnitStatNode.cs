using Turnroot.Characters.Stats;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Skills.Nodes;
using Turnroot.Utilities;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Neutral/Affect Unit Stat")]
    [NodeLabel("Modifies a stat value on the executing unit")]
    public class AffectUnitStatNode : SkillNode
    {
        [Tooltip("The stat to modify")]
        public string selectedStat = "Health";
        public bool isBoundedStat = true;

        [Input]
        public ExecutionFlow executionIn;

        [Input]
        [Tooltip("The amount to change the stat by (positive or negative)")]
        public FloatValue change;

        [Tooltip("Test value used in editor mode")]
        public float testChange = 5f;

        public override void Execute(BattleContext context)
        {
            if (
                !ValidationHelper.ValidateNotNull(
                    context?.UnitInstance,
                    nameof(context.UnitInstance)
                )
            )
            {
                Debug.LogWarning("AffectUnitStat: No unit instance in context");
                return;
            }

            float changeAmount = GetInputFloat("change", testChange);
            ApplyStatChange(
                context.UnitInstance,
                selectedStat,
                isBoundedStat,
                changeAmount,
                "AffectUnitStat"
            );
        }
    }
}
