using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    /// <summary>
    /// Modifies a stat value (Health, Strength, etc.) on the target enemy by a specified amount.
    /// </summary>
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

        public override void Execute(BattleContext context)
        {
            var changePort = GetInputPort("change");
            if (changePort == null || !changePort.IsConnected)
            {
                "AffectEnemyStatNode: 'change' input not provided".LogWarning();
                return;
            }
            float changeAmount = GetInputFloat("change", 0f);
            bool shouldAffectAll = GetInputValue("affectAllTargets", affectAllTargets).value;

            int affected = ExecuteOnTargets(
                context,
                shouldAffectAll,
                target => ApplyStatChange(target, selectedStat, isBoundedStat, changeAmount),
                "AffectEnemyStat"
            );

            if (shouldAffectAll && affected > 0)
            {
                $"AffectEnemyStat: Affected {affected} enemies".LogInfo();
            }
        }
    }
}
