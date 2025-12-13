using Turnroot.Characters.StatusEffects;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Skills.Nodes;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Offensive/Deal Debuff")]
    [NodeLabel("Applies a debuff to the target")]
    public class DealDebuffNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Input]
        [Tooltip("If true, applies debuff to all targeted enemies; if false, only first target")]
        public BoolValue affectAllTargets;

        [Tooltip("Test value for affectAllTargets in editor mode")]
        public bool testAffectAll = false;

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
                Debug.LogWarning("DealDebuffNode: No debuff type assigned!");
                return;
            }

            bool shouldAffectAll = GetInputBool("affectAllTargets", testAffectAll);
            int duration = durationOverride > 0 ? durationOverride : debuffType.DefaultDuration;

            int affected = ExecuteOnTargets(
                context,
                shouldAffectAll,
                target =>
                {
                    // Apply the debuff using the typed StatusEffect system
                    var effect = target.ApplyStatusEffect(
                        debuffType,
                        sourceCharacterId: context.UnitInstance?.Id,
                        sourceSkillId: context.CurrentSkill?.name,
                        duration: duration,
                        intensity: intensity
                    );

                    if (effect != null)
                    {
                        // Publish event through Brain
                        var brain = GetBrain.Get();
                        brain?.PublishStatusEffectApplied(target, effect);
                    }
                },
                "DealDebuff"
            );

            if (affected > 0)
            {
                Debug.Log(
                    $"DealDebuff: Applied {debuffType.DisplayName} debuff to {affected} target(s)"
                );
            }
        }
    }
}
