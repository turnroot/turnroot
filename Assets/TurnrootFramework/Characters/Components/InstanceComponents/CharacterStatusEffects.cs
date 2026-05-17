using System;
using System.Collections.Generic;
using Turnroot.Characters.StatusEffects;

namespace Turnroot.Characters
{
    /// <summary>
    /// Handles status effect application, removal, and querying.
    /// </summary>
    public partial class CharacterInstance
    {
        #region Status Effects

        /// <summary>
        /// Apply a status effect to this character.
        /// If the effect is already active and stackable, adds a stack.
        /// If the effect is already active and not stackable, refreshes duration.
        /// </summary>
        /// <returns>The applied or updated status effect instance.</returns>
        public StatusEffectInstance ApplyStatusEffect(
            StatusEffectType effectType,
            string sourceCharacterId = null,
            string sourceSkillId = null,
            int? duration = null,
            float intensity = 1f
        )
        {
            if (effectType == null)
            {
                return null;
            }

            // Check for existing effect of same type
            var existing = _activeStatusEffects.Find(e => e.EffectType == effectType);
            if (existing != null)
            {
                if (effectType.IsStackable)
                {
                    existing.AddStack();
                }
                existing.RefreshDuration(duration ?? effectType.DefaultDuration);
                return existing;
            }

            // Apply new effect
            var newEffect = new StatusEffectInstance(
                effectType,
                sourceCharacterId,
                sourceSkillId,
                duration,
                intensity
            );
            _activeStatusEffects.Add(newEffect);
            return newEffect;
        }

        /// <summary>
        /// Remove a specific status effect instance.
        /// </summary>
        public bool RemoveStatusEffect(StatusEffectInstance effect)
        {
            return _activeStatusEffects.Remove(effect);
        }

        /// <summary>
        /// Remove all status effects of a specific type.
        /// </summary>
        public int RemoveStatusEffectsByType(StatusEffectType effectType)
        {
            return _activeStatusEffects.RemoveAll(e => e.EffectType == effectType);
        }

        /// <summary>
        /// Remove all buffs from this character.
        /// </summary>
        public int RemoveAllBuffs()
        {
            return _activeStatusEffects.RemoveAll(e => e.EffectType?.IsBuff == true);
        }

        /// <summary>
        /// Remove all debuffs from this character.
        /// </summary>
        public int RemoveAllDebuffs()
        {
            return _activeStatusEffects.RemoveAll(e => e.EffectType?.IsDebuff == true);
        }

        /// <summary>
        /// Remove all status effects from this character.
        /// </summary>
        public void ClearAllStatusEffects()
        {
            _activeStatusEffects.Clear();
        }

        /// <summary>
        /// Check if this character has a specific status effect type.
        /// </summary>
        public bool HasStatusEffect(StatusEffectType effectType)
        {
            return _activeStatusEffects.Exists(e => e.EffectType == effectType);
        }

        /// <summary>
        /// Check if this character has any buff.
        /// </summary>
        public bool HasAnyBuff()
        {
            return _activeStatusEffects.Exists(e => e.EffectType?.IsBuff == true);
        }

        /// <summary>
        /// Check if this character has any debuff.
        /// </summary>
        public bool HasAnyDebuff()
        {
            return _activeStatusEffects.Exists(e => e.EffectType?.IsDebuff == true);
        }

        /// <summary>
        /// Check if this character has a status effect matching a name (by DisplayName, Id, or asset name).
        /// </summary>
        public bool HasStatusEffectByName(string effectName)
        {
            return !string.IsNullOrEmpty(effectName)
                && _activeStatusEffects.Exists(e =>
                    e.EffectType != null
                    && (
                        e.EffectType.DisplayName?.Equals(
                            effectName,
                            StringComparison.OrdinalIgnoreCase
                        ) == true
                        || e.EffectType.Id?.Equals(effectName, StringComparison.OrdinalIgnoreCase)
                            == true
                        || e.EffectType.name?.Equals(effectName, StringComparison.OrdinalIgnoreCase)
                            == true
                    )
                );
        }

        /// <summary>
        /// Get a status effect by name.
        /// </summary>
        public StatusEffectInstance GetStatusEffectByName(string effectName)
        {
            return string.IsNullOrEmpty(effectName)
                ? null
                : _activeStatusEffects.Find(e =>
                    e.EffectType != null
                    && (
                        e.EffectType.DisplayName?.Equals(
                            effectName,
                            StringComparison.OrdinalIgnoreCase
                        ) == true
                        || e.EffectType.Id?.Equals(effectName, StringComparison.OrdinalIgnoreCase)
                            == true
                        || e.EffectType.name?.Equals(effectName, StringComparison.OrdinalIgnoreCase)
                            == true
                    )
                );
        }

        /// <summary>
        /// Tick all status effect durations. Called at end of turn.
        /// Returns list of expired effects that were removed.
        /// </summary>
        public List<StatusEffectInstance> TickStatusEffects()
        {
            var expired = new List<StatusEffectInstance>();

            for (int i = _activeStatusEffects.Count - 1; i >= 0; i--)
            {
                var effect = _activeStatusEffects[i];
                if (!effect.TickDuration())
                {
                    expired.Add(effect);
                    _activeStatusEffects.RemoveAt(i);
                }
            }

            return expired;
        }

        /// <summary>
        /// Return a copy of active status effects for external consumers.
        /// </summary>
        public List<StatusEffectInstance> GetActiveStatusEffects()
        {
            return new(_activeStatusEffects);
        }

        /// <summary>
        /// Check if movement is prevented by any status effect.
        /// </summary>
        public bool IsMovementPrevented()
        {
            return _activeStatusEffects.Exists(e => e.EffectType?.PreventsMovement == true);
        }

        /// <summary>
        /// Check if attack is prevented by any status effect.
        /// </summary>
        public bool IsAttackPrevented()
        {
            return _activeStatusEffects.Exists(e => e.EffectType?.PreventsAttack == true);
        }

        /// <summary>
        /// Check if item use is prevented by any status effect.
        /// </summary>
        public bool IsItemUsePrevented()
        {
            return _activeStatusEffects.Exists(e => e.EffectType?.PreventsItemUse == true);
        }

        /// <summary>
        /// Get total health change per turn from all active effects.
        /// </summary>
        public int GetTotalHealthChangePerTurn()
        {
            int total = 0;
            foreach (var effect in _activeStatusEffects)
            {
                total += effect.GetEffectiveHealthChangePerTurn();
            }
            return total;
        }

        #endregion
    }
}
