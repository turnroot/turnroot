using Assets.Prototypes.Characters.Stats;
using Assets.Prototypes.Gameplay.Combat.FundamentalComponents.Battles;
using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Neutral/Affect Enemy Stat")]
    [NodeLabel("Modifies a stat value on the target enemy")]
    public class AffectEnemyStatNode : SkillNode
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
            "If true, affects all targeted enemies in Targets list; if false, only affects first target"
        )]
        public BoolValue affectAllTargets;

        [Tooltip("Test value used in editor mode")]
        public float testChange = -10f;

        [Tooltip("Test value for affectAllEnemies in editor mode")]
        public bool testAffectAll = false;

        public override void Execute(BattleContext context)
        {
            if (context?.Targets == null || context.Targets.Count == 0)
            {
                Debug.LogWarning("AffectEnemyStat: No target in context");
                return;
            }

            float changeAmount = GetInputFloat("change", testChange);
            bool shouldAffectAll = GetInputBool("affectAllTargets", testAffectAll);

            // Apply to all targeted enemies or just the first one
            if (shouldAffectAll)
            {
                int affectedCount = 0;
                foreach (var target in context.Targets)
                {
                    if (target != null)
                    {
                        if (
                            ApplyStatChange(
                                target,
                                selectedStat,
                                isBoundedStat,
                                changeAmount,
                                "AffectEnemyStat"
                            )
                        )
                        {
                            affectedCount++;
                        }
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
                ApplyStatChange(
                    target,
                    selectedStat,
                    isBoundedStat,
                    changeAmount,
                    "AffectEnemyStat"
                );
            }
        }
    }
}
