using Assets.Prototypes.Characters.Stats;
using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Affect Unit Stat")]
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

        public override void Execute(SkillExecutionContext context)
        {
            if (context?.UnitInstance == null)
            {
                Debug.LogWarning("AffectUnitStat: No unit instance in context");
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

            // Apply the stat change
            if (isBoundedStat)
            {
                if (System.Enum.TryParse<BoundedStatType>(selectedStat, out var boundedType))
                {
                    var stat = context.UnitInstance.GetBoundedStat(boundedType);
                    if (stat != null)
                    {
                        stat.SetCurrent(stat.Current + changeAmount);
                        Debug.Log(
                            $"AffectUnitStat: Changed {selectedStat} by {changeAmount} (new value: {stat.Current})"
                        );
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"AffectUnitStat: Bounded stat {selectedStat} not found on unit"
                        );
                    }
                }
                else
                {
                    Debug.LogWarning($"AffectUnitStat: Invalid bounded stat type: {selectedStat}");
                }
            }
            else
            {
                if (System.Enum.TryParse<UnboundedStatType>(selectedStat, out var unboundedType))
                {
                    var stat = context.UnitInstance.GetUnboundedStat(unboundedType);
                    if (stat != null)
                    {
                        stat.SetCurrent(stat.Current + changeAmount);
                        Debug.Log(
                            $"AffectUnitStat: Changed {selectedStat} by {changeAmount} (new value: {stat.Current})"
                        );
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"AffectUnitStat: Unbounded stat {selectedStat} not found on unit"
                        );
                    }
                }
                else
                {
                    Debug.LogWarning(
                        $"AffectUnitStat: Invalid unbounded stat type: {selectedStat}"
                    );
                }
            }
        }
    }
}
