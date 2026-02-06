using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Provides helper methods for battle management including status effects, last attacker tracking, and AI cache management.
    /// </summary>
    public partial class BattleBrain : BrainComponent
    {
        #region Status Effect Management

        /// <summary>
        /// Apply a status effect to a character via the BattleBrain so events are published consistently.
        /// </summary>
        public Characters.StatusEffects.StatusEffectInstance ApplyStatusEffect(
            CharacterInstance character,
            Characters.StatusEffects.StatusEffectType effectType,
            string sourceCharacterId = null,
            string sourceSkillId = null,
            int? duration = null,
            float intensity = 1f
        )
        {
            if (character == null || effectType == null)
            {
                return null;
            }

            var previous = character.GetActiveStatusEffects().Find(e => e.EffectType == effectType);
            var prevStacks = previous?.CurrentStacks ?? 0;

            var result = character.ApplyStatusEffect(
                effectType,
                sourceCharacterId,
                sourceSkillId,
                duration,
                intensity
            );
            if (result == null)
            {
                return null;
            }

            if (previous == null)
            {
                Brain.PublishStatusEffectApplied(character, result);
            }
            else if (result.CurrentStacks > prevStacks)
            {
                Brain.PublishStatusEffectStacked(character, result);
            }
            else
            {
                // Refreshed or updated duration without stacking
                Brain.PublishStatusEffectApplied(character, result);
            }

            return result;
        }

        private void HandleTurnEndStatusEffects()
        {
            var allInstances = GetAllActiveInstances();
            foreach (var inst in allInstances)
            {
                if (inst == null)
                {
                    continue;
                }

                var expired = inst.TickStatusEffects();
                foreach (var e in expired)
                {
                    Brain.PublishStatusEffectExpired(inst, e);
                }
            }
        }

        public bool RemoveStatusEffect(
            CharacterInstance character,
            Characters.StatusEffects.StatusEffectInstance effect
        )
        {
            if (character == null || effect == null)
            {
                return false;
            }

            var removed = character.RemoveStatusEffect(effect);
            if (removed)
            {
                Brain.PublishStatusEffectRemoved(character, effect);
            }
            return removed;
        }

        public int RemoveStatusEffectsByType(
            CharacterInstance character,
            Characters.StatusEffects.StatusEffectType effectType
        )
        {
            if (character == null || effectType == null)
            {
                return 0;
            }

            var toRemove = character
                .GetActiveStatusEffects()
                .FindAll(e => e.EffectType == effectType);
            var count = character.RemoveStatusEffectsByType(effectType);
            foreach (var r in toRemove)
            {
                Brain.PublishStatusEffectRemoved(character, r);
            }
            return count;
        }

        public int RemoveAllBuffs(CharacterInstance character)
        {
            if (character == null)
            {
                return 0;
            }

            var toRemove = character
                .GetActiveStatusEffects()
                .FindAll(e => e.EffectType?.IsBuff == true);
            var count = character.RemoveAllBuffs();
            foreach (var r in toRemove)
            {
                Brain.PublishStatusEffectRemoved(character, r);
            }
            return count;
        }

        public int RemoveAllDebuffs(CharacterInstance character)
        {
            if (character == null)
            {
                return 0;
            }

            var toRemove = character
                .GetActiveStatusEffects()
                .FindAll(e => e.EffectType?.IsDebuff == true);
            var count = character.RemoveAllDebuffs();
            foreach (var r in toRemove)
            {
                Brain.PublishStatusEffectRemoved(character, r);
            }
            return count;
        }

        public void ClearAllStatusEffects(CharacterInstance character)
        {
            if (character == null)
            {
                return;
            }

            var toRemove = character.GetActiveStatusEffects();
            character.ClearAllStatusEffects();
            foreach (var r in toRemove)
            {
                Brain.PublishStatusEffectRemoved(character, r);
            }
            Brain.PublishAllStatusEffectsCleared(character);
        }

        #endregion

        #region Last Attacker Management

        public void SetLastAttacker(
            BattleContext context,
            CharacterInstance target,
            CharacterInstance attacker
        )
        {
            if (target == null)
            {
                return;
            }

            target.SetLastAttacker(attacker);
            context?.RegisterLastAttacker(target, attacker);
            if (attacker == null)
            {
                Brain.PublishLastAttackerCleared(target);
            }
            else
            {
                Brain.PublishLastAttackerSet(target, attacker);
            }
        }

        public void ClearLastAttacker(BattleContext context, CharacterInstance target)
        {
            if (target == null)
            {
                return;
            }

            target.ClearLastAttacker();
            context?.RegisterLastAttacker(target, null);
            Brain.PublishLastAttackerCleared(target);
        }

        #endregion

        #region AI Management
        public void ClearAICache() => _aiHelper?.InvalidateAllCaches();

        #endregion
    }
}
