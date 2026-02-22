using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    /// <summary>
    /// Modifies attack order or follow-up attack priority in combat.
    /// </summary>
    [CreateNodeMenu("Events/Neutral/Change Battle Order")]
    [NodeLabel("Modifies attack order or follow-up attack priority in combat")]
    public class ChangeBattleOrderNode : SkillNode
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
            if (!ValidateContext(context))
            {
                return;
            }

            float speedMod = GetInputFloat("speedModifier", testSpeedMod);

            // Store in CustomData for combat system to use during attack resolution
            context.SetCustomData("AttackOrderSpeedModifier", speedMod);
            context.SetCustomData("AttackOrderApplyToUnit", applyToUnit);
            context.SetCustomData("AttackOrderEffectType", effectType);

            string target = applyToUnit ? "unit" : "target";

            $"ChangeBattleOrder: Applied {effectType} to {target} (speed mod: {speedMod})".LogInfo();
        }
    }

    /// <summary>
    /// Defines the types of battle order modifications that can be applied during combat.
    /// </summary>
    public enum OrderEffectType
    {
        GuaranteeFollowup, // Unit/target will always perform a follow-up attack
        PreventFollowup, // Prevents follow-up attacks
        ModifySpeedThreshold, // Adjusts the speed threshold for follow-ups
        AttackFirst, // Always attack first regardless of normal turn order
    }
}
