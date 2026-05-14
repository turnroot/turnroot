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

        [Output]
        public ExecutionFlow OutFlow;

        [Input]
        [Tooltip("Speed threshold modifier for follow-up attacks (positive = easier to double)")]
        public FloatValue speedModifier;

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

            var unit = context.Unit.UnitInstance;
            if (!ValidationHelper.ValidateNotNull(unit, nameof(unit)))
            {
                return;
            }

            // Determine which character the effect applies to
            string targetId;
            if (applyToUnit)
            {
                targetId = unit.Id;
            }
            else
            {
                if (
                    !ValidationHelper.ValidateNotNullOrEmpty(
                        context.Participants.Targets,
                        "Targets"
                    )
                )
                {
                    return;
                }
                targetId = context.Participants.Targets[0].Id;
            }

            float speedMod = 0f;
            if (effectType == OrderEffectType.ModifySpeedThreshold)
            {
                var speedPort = GetInputPort("speedModifier");
                if (speedPort == null || !speedPort.IsConnected)
                {
                    "ChangeBattleOrderNode: 'speedModifier' input not provided for ModifySpeedThreshold".LogWarning();
                    return;
                }
                speedMod = GetInputFloat("speedModifier", 0f);
            }

            // Write to the same CustomData keys that ExecuteCombatExchange / CanFollowUp read
            switch (effectType)
            {
                case OrderEffectType.AttackFirst:
                    // Shared key with FirstStrikeNode — attacker strikes twice before defender responds
                    context.SetCustomData($"FirstStrike_{targetId}", true);
                    break;
                case OrderEffectType.PreventFollowup:
                    // Shared key with DisableEnemyFollowupNode; prevents counter and follow-up
                    context.SetCustomData($"DisableFollowup_{targetId}", true);
                    break;
                case OrderEffectType.GuaranteeFollowup:
                    context.SetCustomData($"GuaranteeFollowup_{targetId}", true);
                    break;
                case OrderEffectType.ModifySpeedThreshold:
                    context.SetCustomData($"SpeedThresholdMod_{targetId}", speedMod);
                    break;
                case OrderEffectType.CounterFirst:
                    // Vantage: the targeted unit counterattacks before the attacker's first strike.
                    // Only meaningful when applied to the defender (applyToUnit = false).
                    context.SetCustomData($"Vantage_{targetId}", true);
                    break;
            }

            string targetLabel = applyToUnit ? "unit" : "target";
            $"ChangeBattleOrder: Applied {effectType} to {targetLabel} ({targetId})".LogInfo();
        }
    }

    /// <summary>
    /// Defines the types of battle order modifications that can be applied during combat.
    /// </summary>
    public enum OrderEffectType
    {
        GuaranteeFollowup, // Unit/target will always perform a follow-up attack
        PreventFollowup, // Prevents counter-attack and follow-up attacks this exchange
        ModifySpeedThreshold, // Adjusts the speed threshold for follow-ups
        AttackFirst, // Always attack first (both strikes) before defender can respond — Desperation
        CounterFirst, // Unit/target counterattacks before the initiator's first strike — Vantage
    }
}
