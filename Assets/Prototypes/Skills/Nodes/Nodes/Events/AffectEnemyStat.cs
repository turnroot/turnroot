using Assets.Prototypes.Characters.Stats;
using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Affect Enemy Stat")]
    [NodeLabel("Modifies a stat value on the target enemy")]
    public class AffectEnemyStat : SkillNode
    {
        [Tooltip("The stat to modify")]
        public string selectedStat = "Health";
        public bool isBoundedStat = true;

        [Input]
        public ExecutionFlow executionIn;

        [Input]
        [Tooltip("The amount to change the stat by (positive or negative)")]
        public FloatValue change;

        [Input]
        [Tooltip(
            "If true, affects all enemies in Targets list; if false, only affects first target"
        )]
        public BoolValue affectAllEnemies;

        [Tooltip("Test value used in editor mode")]
        public float testChange = -10f;

        [Tooltip("Test value for affectAllEnemies in editor mode")]
        public bool testAffectAll = false;

        public override void Execute(SkillExecutionContext context)
        {
            if (context?.Targets == null || context.Targets.Count == 0)
            {
                Debug.LogWarning("AffectEnemyStat: No target in context");
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

            // Get the affectAllEnemies value
            bool shouldAffectAll = testAffectAll;
            var affectAllPort = GetInputPort("affectAllEnemies");
            if (affectAllPort != null && affectAllPort.IsConnected)
            {
                var inputValue = affectAllPort.GetInputValue();
                if (inputValue is BoolValue boolValue)
                {
                    shouldAffectAll = boolValue.value;
                }
            }

            // Apply to all targets or just the first one
            if (shouldAffectAll)
            {
                int affectedCount = 0;
                foreach (var target in context.Targets)
                {
                    if (target != null)
                    {
                        ApplyStatChange(target, changeAmount);
                        affectedCount++;
                    }
                }
                Debug.Log($"AffectEnemyStat: Affected {affectedCount} enemies");
            }
            else
            {
                var target = context.Targets[0];
                if (target == null)
                {
                    Debug.LogWarning("AffectEnemyStat: Target is null");
                    return;
                }
                ApplyStatChange(target, changeAmount);
            }
        }

        private void ApplyStatChange(
            Assets.Prototypes.Characters.CharacterInstance target,
            float changeAmount
        )
        {
            // Apply the stat change
            if (isBoundedStat)
            {
                if (System.Enum.TryParse<BoundedStatType>(selectedStat, out var boundedType))
                {
                    var stat = target.GetBoundedStat(boundedType);
                    if (stat != null)
                    {
                        stat.SetCurrent(stat.Current + changeAmount);
                        Debug.Log(
                            $"AffectEnemyStat: Changed {selectedStat} by {changeAmount} (new value: {stat.Current})"
                        );
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"AffectEnemyStat: Bounded stat {selectedStat} not found on enemy"
                        );
                    }
                }
                else
                {
                    Debug.LogWarning($"AffectEnemyStat: Invalid bounded stat type: {selectedStat}");
                }
            }
            else
            {
                if (System.Enum.TryParse<UnboundedStatType>(selectedStat, out var unboundedType))
                {
                    var stat = target.GetUnboundedStat(unboundedType);
                    if (stat != null)
                    {
                        stat.SetCurrent(stat.Current + changeAmount);
                        Debug.Log(
                            $"AffectEnemyStat: Changed {selectedStat} by {changeAmount} (new value: {stat.Current})"
                        );
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"AffectEnemyStat: Unbounded stat {selectedStat} not found on enemy"
                        );
                    }
                }
                else
                {
                    Debug.LogWarning(
                        $"AffectEnemyStat: Invalid unbounded stat type: {selectedStat}"
                    );
                }
            }
        }
    }
}
