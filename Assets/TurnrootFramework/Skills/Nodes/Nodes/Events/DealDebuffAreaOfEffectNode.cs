using Turnroot.Characters.StatusEffects;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    /// <summary>
    /// Applies a status debuff to all enemies within an area of effect radius.
    /// </summary>
    [CreateNodeMenu("Events/Offensive/Deal Debuff Area Of Effect")]
    [NodeLabel("Applies a debuff to all targeted enemies in an area")]
    public class DealDebuffAreaOfEffectNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Output]
        public ExecutionFlow OutFlow;

        [Input]
        [Tooltip("The radius of the area of effect")]
        public FloatValue aoeRadius;

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
                "DealDebuffAreaOfEffectNode: No debuff type assigned!".LogWarning();
                return;
            }

            var radPort = GetInputPort("aoeRadius");
            if (radPort == null || !radPort.IsConnected)
            {
                "DealDebuffAreaOfEffectNode: 'aoeRadius' input not provided".LogWarning();
                return;
            }
            float radius = GetInputFloat("aoeRadius", 0f);
            int duration = durationOverride > 0 ? durationOverride : debuffType.DefaultDuration;

            var casterPos = context.Unit.UnitInstance?.MapGridPosition;
            if (casterPos == null)
            {
                "DealDebuffAreaOfEffectNode: No caster unit in context".LogWarning();
                return;
            }

            var targets = GetPotentialTargets(
                context,
                target =>
                {
                    var targetPos = target.MapGridPosition;
                    int distance =
                        Mathf.Abs(casterPos.Value.x - targetPos.x)
                        + Mathf.Abs(casterPos.Value.y - targetPos.y);
                    return distance <= radius;
                }
            );
            int affectedCount = 0;
            foreach (var target in targets)
            {
                var effect = context.Brain.battleBrain.ApplyStatusEffect(
                    target,
                    debuffType,
                    sourceCharacterId: context.Unit.UnitInstance.Id,
                    sourceSkillId: context.Skill.CurrentSkill?.name,
                    duration: duration,
                    intensity: intensity
                );
                if (effect != null)
                {
                    affectedCount++;
                }
            }

            $"DealDebuffAreaOfEffect: Applied {debuffType.DisplayName} debuff to {affectedCount} targets in {radius} tile radius".LogInfo();
        }
    }
}
