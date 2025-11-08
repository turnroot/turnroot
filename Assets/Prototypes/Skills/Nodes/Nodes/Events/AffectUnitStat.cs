using Assets.Prototypes.Characters.Stats;
using Assets.Prototypes.Gameplay.Combat.FundamentalComponents.Battles;
using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Defensive/Affect Unit Stat")]
    [NodeLabel("Modifies a stat value on the executing unit")]
    public class AffectUnitStat : SkillNode
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
            if (context?.UnitInstance == null)
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
