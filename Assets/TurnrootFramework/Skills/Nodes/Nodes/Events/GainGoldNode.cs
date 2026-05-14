using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    /// <summary>
    /// Grants a specified amount of gold to the player.
    /// </summary>
    [CreateNodeMenu("Events/Neutral/Gain Gold")]
    [NodeLabel("Grants gold to the player")]
    public class GainGoldNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Input]
        [Tooltip("The amount of gold to gain")]
        public FloatValue goldAmount;

        public override void Execute(BattleContext context)
        {
            if (!ValidateContext(context))
            {
                return;
            }

            var goldPort = GetInputPort("goldAmount");
            int gold =
                goldPort != null && goldPort.IsConnected ? (int)GetInputFloat("goldAmount", 0f) : 0;

            if (gold <= 0)
            {
                "GainGoldNode: goldAmount is 0 or unconnected — no gold awarded".LogInfo();
                return;
            }

            context.Brain.PublishGoldGained(gold);

            $"GainGold: Player gained {gold} gold".LogInfo();
        }
    }
}
