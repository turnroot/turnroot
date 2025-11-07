using Assets.Prototypes.Characters;
using Assets.Prototypes.Characters.Stats;
using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Defensive/Affect Adjacent Ally Stat")]
    [NodeLabel("Modifies a stat value on adjacent allied units")]
    public class AffectAdjacentAllyStat : SkillNode
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

        public override void Execute(SkillExecutionContext context)
        {
            if (context?.UnitInstance == null)
            {
                Debug.LogWarning("AffectAdjacentAllyStat: No unit instance in context");
                return;
            }

            // Get the change value
            float changeAmount = testChange;
            var changePort = GetInputPort("change");
            if (changePort != null && changePort.IsConnected)
            {
                var inputValue = changePort.GetInputValue();
                if (inputValue is FloatValue floatValue)
                {
                    changeAmount = floatValue.value;
                }
            }

            // Get adjacent allies from context
            if (context.AdjacentUnits == null || context.AdjacentUnits.Count == 0)
            {
                Debug.LogWarning("AffectAdjacentAllyStat: No adjacent units available in context");
                return;
            }

            int affectedCount = 0;

            // Iterate through adjacent units and affect allies
            foreach (var kvp in context.AdjacentUnits)
            {
                var adjacentUnit = kvp.Value;
                if (adjacentUnit == null)
                    continue;

                // Skip if not an ally (compare with Allies list)
                if (
                    context.Allies == null
                    || !context.Allies.Exists(ally => ally.Id == adjacentUnit.Id)
                )
                {
                    continue;
                }

                // Apply the stat change
                bool success = false;
                if (isBoundedStat)
                {
                    if (System.Enum.TryParse<BoundedStatType>(selectedStat, out var boundedType))
                    {
                        var stat = adjacentUnit.GetBoundedStat(boundedType);
                        if (stat != null)
                        {
                            stat.SetCurrent(stat.Current + changeAmount);
                            success = true;
                            Debug.Log(
                                $"AffectAdjacentAllyStat: Changed {selectedStat} by {changeAmount} for ally at {kvp.Key} (new value: {stat.Current})"
                            );
                        }
                    }
                }
                else
                {
                    if (
                        System.Enum.TryParse<UnboundedStatType>(selectedStat, out var unboundedType)
                    )
                    {
                        var stat = adjacentUnit.GetUnboundedStat(unboundedType);
                        if (stat != null)
                        {
                            stat.SetCurrent(stat.Current + changeAmount);
                            success = true;
                            Debug.Log(
                                $"AffectAdjacentAllyStat: Changed {selectedStat} by {changeAmount} for ally at {kvp.Key} (new value: {stat.Current})"
                            );
                        }
                    }
                }

                if (success)
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
