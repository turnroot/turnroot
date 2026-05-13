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
            if (goldPort == null || !goldPort.IsConnected)
            {
                "GainGoldNode: 'goldAmount' input not provided".LogWarning();
                return;
            }

            int gold = (int)GetInputFloat("goldAmount", 0f);

            context.Brain.PublishGoldGained(gold);

            $"GainGold: Player gained {gold} gold".LogInfo();
        }
    }
}
