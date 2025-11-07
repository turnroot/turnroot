using Assets.Prototypes.Characters;
using Assets.Prototypes.Characters.Stats;
using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Affect Adjacent Ally Stat")]
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

            // TODO: Get actual adjacent allies from game logic
            // For now, this is a placeholder that logs the action
            Debug.Log(
                $"AffectAdjacentAllyStat: Would change {selectedStat} by {changeAmount} for adjacent allies. "
                    + $"(Requires integration with grid/positioning system to identify adjacent units)"
            );

            // Example of how this would work with actual adjacent allies:
            // var adjacentAllies = GetAdjacentAllies(context.UnitInstance);
            // foreach (var ally in adjacentAllies)
            // {
            //     ApplyStatChange(ally, changeAmount);
            // }
        }

        private void ApplyStatChange(CharacterInstance character, float changeAmount)
        {
            // Apply the stat change
            if (isBoundedStat)
            {
                if (System.Enum.TryParse<BoundedStatType>(selectedStat, out var boundedType))
                {
                    var stat = character.GetBoundedStat(boundedType);
                    if (stat != null)
                    {
                        stat.SetCurrent(stat.Current + changeAmount);
                        Debug.Log(
                            $"AffectAdjacentAllyStat: Changed {selectedStat} by {changeAmount} (new value: {stat.Current})"
                        );
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"AffectAdjacentAllyStat: Bounded stat {selectedStat} not found on ally"
                        );
                    }
                }
                else
                {
                    Debug.LogWarning(
                        $"AffectAdjacentAllyStat: Invalid bounded stat type: {selectedStat}"
                    );
                }
            }
            else
            {
                if (System.Enum.TryParse<UnboundedStatType>(selectedStat, out var unboundedType))
                {
                    var stat = character.GetUnboundedStat(unboundedType);
                    if (stat != null)
                    {
                        stat.SetCurrent(stat.Current + changeAmount);
                        Debug.Log(
                            $"AffectAdjacentAllyStat: Changed {selectedStat} by {changeAmount} (new value: {stat.Current})"
                        );
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"AffectAdjacentAllyStat: Unbounded stat {selectedStat} not found on ally"
                        );
                    }
                }
                else
                {
                    Debug.LogWarning(
                        $"AffectAdjacentAllyStat: Invalid unbounded stat type: {selectedStat}"
                    );
                }
            }
        }
    }
}
