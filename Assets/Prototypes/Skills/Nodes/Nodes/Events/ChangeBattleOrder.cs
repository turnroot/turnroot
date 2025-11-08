using Assets.Prototypes.Gameplay.Combat.FundamentalComponents.Battles;
using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Neutral/Change Battle Order")]
    [NodeLabel("Modifies attack order or follow-up attack priority in combat")]
    public class ChangeBattleOrder : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Input]
        [Tooltip("Speed threshold modifier for follow-up attacks (positive = easier to double)")]
        public FloatValue speedModifier;

        [Tooltip("Test value for speed modifier in editor mode")]
        public float testSpeedMod = 5f;

        [Tooltip("Apply to unit or target")]
        public bool applyToUnit = true;

        [Tooltip("Effect type")]
        public OrderEffectType effectType = OrderEffectType.GuaranteeFollowup;

        public override void Execute(BattleContext context)
        {
            if (context == null)
            {
                Debug.LogWarning("ChangeBattleOrder: No context provided");
                return;
            }

            float speedMod = GetInputFloat("speedModifier", testSpeedMod);

            // TODO: Integrate with actual combat system
            context.SetCustomData("AttackOrderSpeedModifier", speedMod);
            context.SetCustomData("AttackOrderApplyToUnit", applyToUnit);
            context.SetCustomData("AttackOrderEffectType", effectType);

            string target = applyToUnit ? "unit" : "target";
            Debug.Log(
                $"ChangeBattleOrder: Applied {effectType} to {target} (speed mod: {speedMod})"
            );
        }
    }

    public enum OrderEffectType
    {
        GuaranteeFollowup, // Unit/target will always perform a follow-up attack
        PreventFollowup, // Prevents follow-up attacks
        ModifySpeedThreshold, // Adjusts the speed threshold for follow-ups
        AttackFirst, // Always attack first regardless of normal turn order
    }
}
