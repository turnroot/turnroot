using Turnroot.Characters.StatusEffects;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    /// <summary>
    /// Applies a status debuff to one or more target enemies.
    /// </summary>
    [CreateNodeMenu("Events/Offensive/Deal Debuff")]
    [NodeLabel("Applies a debuff to the target")]
    public class DealDebuffNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Output]
        public ExecutionFlow OutFlow;

        [Input]
        [Tooltip("If true, applies debuff to all targeted enemies; if false, only first target")]
        public BoolValue affectAllTargets;

        [Tooltip("The type of debuff to apply")]
        public StatusEffectType debuffType;

        [Tooltip("Duration of the debuff in turns (overrides debuffType default if set)")]
        public int durationOverride = -1;

        [Tooltip("Intensity/strength of the debuff")]
        public float intensity = 1f;

        public override void Execute(BattleContext context)
        {
            if (debuffType == null)
            {
                "DealDebuffNode: No debuff type assigned!".LogWarning();
                return;
            }

            bool shouldAffectAll = GetInputValue("affectAllTargets", affectAllTargets).value;
            int duration = durationOverride >= 0 ? durationOverride : debuffType.DefaultDuration;

            int affected = ExecuteOnTargets(
                context,
                shouldAffectAll,
                target =>
                {
                    // Apply the debuff using the typed StatusEffect system
                    var effect = context.Brain.battleBrain.ApplyStatusEffect(
                        target,
                        debuffType,
                        sourceCharacterId: context.Unit.UnitInstance?.Id,
                        sourceSkillId: context.Skill.CurrentSkill?.name,
                        duration: duration,
                        intensity: intensity
                    );
                },
                "DealDebuff"
            );

            if (affected > 0)
            {
                $"DealDebuff: Applied {debuffType.DisplayName} debuff to {affected} target(s)".LogInfo();
            }
        }
    }
}
