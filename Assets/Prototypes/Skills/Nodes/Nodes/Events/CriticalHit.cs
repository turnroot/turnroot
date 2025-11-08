using Assets.Prototypes.Gameplay.Combat.FundamentalComponents.Battles;
using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Offensive/Critical Hit")]
    [NodeLabel("Triggers a critical hit")]
    public class CriticalHit : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        public override void Execute(BattleContext context)
        {
            if (context == null)
            {
                Debug.LogWarning("CriticalHit: No context provided");
                return;
            }

            // TODO: Integrate with actual combat system to trigger critical hit
            // The combat system will handle the damage multiplier calculation
            context.SetCustomData("IsCriticalHit", true);

            Debug.Log($"CriticalHit: Triggered a critical hit");
        }
    }
}
