using Turnroot.Characters.StatusEffects;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    /// <summary>
    /// Applies a buff to the caster's own unit.
    /// </summary>
    [CreateNodeMenu("Events/Defensive/Buff Unit")]
    [NodeLabel("Buff Unit")]
    public class BuffUnitNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

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
                "BuffUnitNode: No buff type assigned!".LogWarning();
                return;
            }

            var self = context.Unit.UnitInstance;
            if (self == null)
            {
                "BuffUnitNode: caster has no CharacterInstance".LogWarning();
                return;
            }

            int duration = durationOverride >= 0 ? durationOverride : buffType.DefaultDuration;

            var effect = context.Brain.battleBrain.ApplyStatusEffect(
                self,
                buffType,
                sourceCharacterId: self.Id,
                sourceSkillId: context.Skill.CurrentSkill?.name,
                duration: duration,
                intensity: 1f
            );

            if (effect != null)
            {
                string durStr = duration > 0 ? $"{duration} turns" : "permanent";
                $"BuffUnit: Applied {buffType.DisplayName} to self ({durStr})".LogInfo();
            }
        }
    }
}
