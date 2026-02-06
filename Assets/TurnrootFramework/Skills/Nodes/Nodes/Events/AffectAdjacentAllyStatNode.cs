using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    /// <summary>
    /// Modifies a stat value on all adjacent allied units.
    /// </summary>
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
                    context?.Unit.UnitInstance,
                    nameof(context.Unit.UnitInstance)
                )
            )
            {
                return;
            }

            if (
                !ValidationHelper.ValidateNotNull(
                    context.Participants.AdjacentUnits,
                    nameof(context.Participants.AdjacentUnits)
                )
            )
            {
                return;
            }

            float changeAmount = GetInputFloat("change", testChange);
            var adjacentAllies = ListPool<CharacterInstance>.Get();
            context.Participants.AdjacentUnits.GetAdjacentAlliesNonAlloc(context, adjacentAllies);

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

            ListPool<CharacterInstance>.Return(adjacentAllies);

            if (affectedCount == 0)
            {
                Debug.LogWarning(
                    "AffectAdjacentAllyStat: No adjacent allies found or stat changes failed"
                );
            }
            else
            {
                TurnrootLogger.Log(
                    $"AffectAdjacentAllyStat: Successfully affected {affectedCount} adjacent {(affectedCount == 1 ? "ally" : "allies")}"
                );
            }
        }
    }
}
