using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    /// <summary>
    /// Adjusts the weapon advantage percentage in combat calculations.
    /// </summary>
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
            if (!ValidateContext(context))
            {
                return;
            }

            // Store in CustomData for combat system to use during advantage calculation
            context.SetCustomData("AdvantagePercentModifier", AddAdvantagePercent);

            $"AdjustAdvantagePercents: Adjusted advantage percents by {AddAdvantagePercent}%"
        .LogInfo();
        }
    }
}

