using Turnroot.Characters.StatusEffects;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Skills.Nodes;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Offensive/Deal Debuff Area Of Effect")]
    [NodeLabel("Applies a debuff to all targeted enemies in an area")]
    public class DealDebuffAreaOfEffectNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Input]
        [Tooltip("The radius of the area of effect")]
        public FloatValue aoeRadius;

        [Tooltip("Test value for AoE radius in editor mode")]
        public float testRadius = 2f;

        [Tooltip("The type of debuff to apply")]
        public StatusEffectType debuffType;

        [Tooltip("Duration of the debuff in turns (overrides debuffType default if set)")]
        public int durationOverride = -1;

        [Tooltip("Intensity/strength of the debuff")]
        public float intensity = 1f;

        public override void Execute(BattleContext context)
        {
            if (!ValidateHasTargets(context))
            {
                return;
            }

            if (debuffType == null)
            {
                Debug.LogWarning("DealDebuffAreaOfEffectNode: No debuff type assigned!");
                return;
            }

            float radius = GetInputFloat("aoeRadius", testRadius);
            int duration = durationOverride > 0 ? durationOverride : debuffType.DefaultDuration;

            // Apply debuff to all targeted enemies in the AoE
            int affectedCount = ExecuteOnAllTargets(
                context,
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
                }
            );

            Debug.Log(
                $"DealDebuffAreaOfEffect: Applied {debuffType.DisplayName} debuff to {affectedCount} enemies in {radius} tile radius"
            );
        }
    }
}
