using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
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
            float changeAmount = GetInputFloat("change", testChange);
            bool shouldAffectAll = GetInputBool("affectAllTargets", testAffectAll);

            int affected = ExecuteOnTargets(
                context,
                shouldAffectAll,
                target => ApplyStatChange(target, selectedStat, isBoundedStat, changeAmount),
                "AffectEnemyStat"
            );

            if (shouldAffectAll && affected > 0)
            {
#if UNITY_EDITOR
                TurnrootLogger.Log($"AffectEnemyStat: Affected {affected} enemies");
#endif
            }
        }
    }
}
