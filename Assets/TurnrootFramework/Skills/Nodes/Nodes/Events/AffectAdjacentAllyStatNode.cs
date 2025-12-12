using System.Linq;
using Turnroot.Characters;
using Turnroot.Characters.Stats;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Skills.Nodes;
using Turnroot.Utilities;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Defensive/Affect Adjacent Ally Stat")]
    [NodeLabel("Modifies a stat value on adjacent allied units")]
    public class AffectAdjacentAllyStatNode : SkillNode
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
                Debug.LogWarning("AffectAdjacentAllyStat: No unit instance in context");
                return;
            }

            if (
                !ValidationHelper.ValidateNotNull(
                    context.AdjacentUnits,
                    nameof(context.AdjacentUnits)
                )
            )
            {
                Debug.LogWarning("AffectAdjacentAllyStat: No adjacent units available in context");
                return;
            }

            float changeAmount = GetInputFloat("change", testChange);
            var adjacentAllies = context.AdjacentUnits.GetAdjacentAllies(context);

            int affectedCount = 0;
            foreach (var adjacentUnit in adjacentAllies)
            {
                if (
                    ApplyStatChange(
                        adjacentUnit,
                        selectedStat,
                        isBoundedStat,
                        changeAmount,
                        "AffectAdjacentAllyStat"
                    )
                )
                {
                    affectedCount++;
                }
            }

            if (affectedCount == 0)
            {
                Debug.LogWarning(
                    "AffectAdjacentAllyStat: No adjacent allies found or stat changes failed"
                );
            }
            else
            {
                Debug.Log(
                    $"AffectAdjacentAllyStat: Successfully affected {affectedCount} adjacent {(affectedCount == 1 ? "ally" : "allies")}"
                );
            }
        }
    }
}
