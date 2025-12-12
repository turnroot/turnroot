using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Neutral/Adjust Advantage Percents")]
    [NodeLabel("Adjust Advantage Percents")]
    public class AdjustAdvantagePercentsNode : SkillNode
    {
        [Input]
        public ExecutionFlow In;

        [Tooltip("The percent to increase advantage by")]
        [Range(0, 100)]
        public float AddAdvantagePercent;

        public override void Execute(BattleContext context)
        {
            if (context == null)
            {
                Debug.LogWarning("AdjustAdvantagePercents: No context provided");
                return;
            }

            // Store in CustomData for combat system to use during advantage calculation
            context.SetCustomData("AdvantagePercentModifier", AddAdvantagePercent);
            Debug.Log(
                $"AdjustAdvantagePercents: Adjusted advantage percents by {AddAdvantagePercent}%"
            );
        }
    }
}
