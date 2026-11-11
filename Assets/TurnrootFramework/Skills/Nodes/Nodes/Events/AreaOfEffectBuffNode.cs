using Turnroot.Characters.StatusEffects;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    /// <summary>
    /// Applies a buff to all allied units within a specified radius.
    /// </summary>
    [CreateNodeMenu("Events/Defensive/Area Of Effect Buff")]
    [NodeLabel("Buff adjacent allies in radius")]
    public class AreaOfEffectBuffNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Output]
        public ExecutionFlow OutFlow;

        [Input]
        [Tooltip("The intensity multiplier for the buff (1.0 = normal strength)")]
        public FloatValue intensity;

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
                "AreaOfEffectBuffNode: No buff type assigned!".LogWarning();
                return;
            }

            if (context.Unit.UnitInstance == null)
            {
                "AreaOfEffectBuff: No caster unit in context".LogWarning();
                return;
            }

            var intensityPort = GetInputPort("intensity");
            float intensityValue =
                intensityPort != null && intensityPort.IsConnected
                    ? GetInputFloat("intensity", 1f)
                    : 1f;
            int duration = durationOverride >= 0 ? durationOverride : buffType.DefaultDuration;

            var casterPos = context.Unit.UnitInstance.MapGridPosition;
            var targets = GetPotentialTargets(
                context,
                ally =>
                {
                    var allyPos = ally.MapGridPosition;
                    int distance =
                        Mathf.Abs(casterPos.x - allyPos.x)
                        + Mathf.Abs(casterPos.y - allyPos.y);
                    return distance <= radius;
                }
            );
            if (targets.Count == 0)
            {
                "AreaOfEffectBuff: No targets in range".LogWarning();
                return;
            }

            int affectedCount = 0;
            foreach (var ally in targets)
            {
                var effect = context.Brain.battleBrain.ApplyStatusEffect(
                    ally,
                    buffType,
                    sourceCharacterId: context.Unit.UnitInstance.Id,
                    sourceSkillId: context.Skill.CurrentSkill?.name,
                    duration: duration,
                    intensity: intensityValue
                );

                if (effect != null)
                {
                    affectedCount++;
                }
            }

            string durationType = duration > 0 ? $"{duration} turns" : "permanent";

            $"AreaOfEffectBuff: Applied {buffType.DisplayName} to {affectedCount} allies within {radius} tiles ({durationType})".LogInfo();
        }
    }
}
