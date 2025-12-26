using Turnroot.Characters.StatusEffects;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Defensive/Area Of Effect Buff")]
    [NodeLabel("Buff adjacent allies in radius")]
    public class AreaOfEffectBuffNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Input]
        [Tooltip("The intensity multiplier for the buff (1.0 = normal strength)")]
        public FloatValue intensity;

        [Tooltip("Test value for intensity in editor mode")]
        public float testIntensity = 1f;

        [Tooltip("Effect radius in tiles")]
        [Range(1, 10)]
        public float radius = 2f;

        [Tooltip("The buff type to apply")]
        public StatusEffectType buffType;

        [Tooltip("Duration override in turns (-1 = use buff default, 0 = permanent)")]
        [Range(-1, 10)]
        public int durationOverride = -1;

        public override void Execute(BattleContext context)
        {
            if (!ValidateContext(context))
            {
                return;
            }

            if (buffType == null)
            {
                Debug.LogWarning("AreaOfEffectBuffNode: No buff type assigned!");
                return;
            }

            if (context.Participants.Allies == null || context.Participants.Allies.Count == 0)
            {
                Debug.LogWarning("AreaOfEffectBuff: No allies in context");
                return;
            }

            float intensityValue = GetInputFloat("intensity", testIntensity);
            int duration = durationOverride >= 0 ? durationOverride : buffType.DefaultDuration;

            // Apply buff to allies within radius
            int affectedCount = 0;
            foreach (var ally in context.Participants.Allies)
            {
                // Check if ally is within radius (using Manhattan distance for grid-based)
                var casterPos = context.Unit.UnitInstance?.MapGridPosition;
                var allyPos = ally?.MapGridPosition;

                if (casterPos == null || allyPos == null)
                {
                    continue;
                }

                int distance =
                    Mathf.Abs(casterPos.Value.x - allyPos.Value.x)
                    + Mathf.Abs(casterPos.Value.y - allyPos.Value.y);
                if (distance <= radius)
                {
                    var effect = ally.ApplyStatusEffect(
                        buffType,
                        sourceCharacterId: context.Unit.UnitInstance?.Id,
                        sourceSkillId: context.Skill.CurrentSkill?.name,
                        duration: duration,
                        intensity: intensityValue
                    );

                    if (effect != null)
                    {
                        context.Brain?.PublishStatusEffectApplied(ally, effect);
                        affectedCount++;
                    }
                }
            }

            string durationType = duration > 0 ? $"{duration} turns" : "permanent";
            Debug.Log(
                $"AreaOfEffectBuff: Applied {buffType.DisplayName} to {affectedCount} allies within {radius} tiles ({durationType})"
            );
        }
    }
}
