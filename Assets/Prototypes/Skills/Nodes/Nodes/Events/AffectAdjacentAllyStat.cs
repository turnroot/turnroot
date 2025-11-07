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

            // TODO: Implement adjacent ally detection and stat changes
            // This requires integration with grid/positioning system to:
            // 1. Get adjacent allies: var adjacentAllies = GetAdjacentAllies(context.UnitInstance);
            // 2. For each ally, apply the stat change:
            //    - Parse selectedStat to BoundedStatType or UnboundedStatType based on isBoundedStat
            //    - Get the stat from the ally's CharacterInstance using GetBoundedStat() or GetUnboundedStat()
            //    - Update the stat's current value: stat.SetCurrent(stat.Current + changeAmount)
            Debug.Log(
                $"AffectAdjacentAllyStat: Would change {selectedStat} by {changeAmount} for adjacent allies. "
                    + $"(Requires integration with grid/positioning system to identify adjacent units)"
            );
        }
    }
}
