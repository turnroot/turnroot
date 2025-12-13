using System;
using UnityEngine;

namespace Turnroot.Characters.StatusEffects
{
    /// <summary>
    /// Runtime instance of an active status effect on a character.
    /// Tracks duration, stacks, and source information.
    /// </summary>
    [Serializable]
    public class StatusEffectInstance
    {
        [SerializeField]
        private StatusEffectType _effectType;

        [SerializeField]
        private int _remainingDuration;

        [SerializeField]
        private int _currentStacks;

        [SerializeField]
        private string _sourceCharacterId;

        [SerializeField]
        private string _sourceSkillId;

        [SerializeField]
        private float _intensity;

        /// <summary>
        /// The status effect type definition.
        /// </summary>
        public StatusEffectType EffectType => _effectType;

        /// <summary>
        /// Remaining turns until the effect expires. 0 = permanent.
        /// </summary>
        public int RemainingDuration => _remainingDuration;

        /// <summary>
        /// Current number of stacks (for stackable effects).
        /// </summary>
        public int CurrentStacks => _currentStacks;

        /// <summary>
        /// ID of the character that applied this effect.
        /// </summary>
        public string SourceCharacterId => _sourceCharacterId;

        /// <summary>
        /// ID of the skill that applied this effect.
        /// </summary>
        public string SourceSkillId => _sourceSkillId;

        /// <summary>
        /// Intensity multiplier for the effect.
        /// </summary>
        public float Intensity => _intensity;

        /// <summary>
        /// Whether this effect has expired.
        /// </summary>
        public bool IsExpired => _remainingDuration < 0;

        /// <summary>
        /// Whether this is a permanent effect (duration = 0 means permanent).
        /// </summary>
        public bool IsPermanent => _effectType?.DefaultDuration == 0 || _remainingDuration == int.MaxValue;

        /// <summary>
        /// Creates a new status effect instance.
        /// </summary>
        public StatusEffectInstance(
            StatusEffectType effectType,
            string sourceCharacterId = null,
            string sourceSkillId = null,
            int? duration = null,
            float intensity = 1f)
        {
            _effectType = effectType;
            _sourceCharacterId = sourceCharacterId;
            _sourceSkillId = sourceSkillId;
            _remainingDuration = duration ?? effectType?.DefaultDuration ?? 3;
            _currentStacks = 1;
            _intensity = intensity;

            // If duration is 0, treat as permanent
            if (_remainingDuration == 0)
            {
                _remainingDuration = int.MaxValue;
            }
        }

        /// <summary>
        /// Decrements the duration by one turn.
        /// </summary>
        /// <returns>True if the effect is still active, false if expired.</returns>
        public bool TickDuration()
        {
            if (IsPermanent)
            {
                return true;
            }

            _remainingDuration--;
            return _remainingDuration >= 0;
        }

        /// <summary>
        /// Adds a stack to this effect (for stackable effects).
        /// </summary>
        /// <returns>True if a stack was added, false if at max stacks.</returns>
        public bool AddStack()
        {
            if (_effectType == null || !_effectType.IsStackable)
            {
                return false;
            }

            if (_currentStacks >= _effectType.MaxStacks)
            {
                return false;
            }

            _currentStacks++;
            return true;
        }

        /// <summary>
        /// Refreshes the duration to the default.
        /// </summary>
        public void RefreshDuration()
        {
            _remainingDuration = _effectType?.DefaultDuration ?? 3;
            if (_remainingDuration == 0)
            {
                _remainingDuration = int.MaxValue;
            }
        }

        /// <summary>
        /// Refreshes the duration to a specific value.
        /// </summary>
        public void RefreshDuration(int newDuration)
        {
            _remainingDuration = newDuration;
            if (_remainingDuration == 0)
            {
                _remainingDuration = int.MaxValue;
            }
        }

        /// <summary>
        /// Gets the effective stat modifier value, accounting for stacks and intensity.
        /// </summary>
        public float GetEffectiveFlatModifier(Stats.UnboundedStatType statType)
        {
            if (_effectType?.FlatModifiers == null)
            {
                return 0f;
            }

            foreach (var mod in _effectType.FlatModifiers)
            {
                if (mod.StatType == statType)
                {
                    return mod.Value * _currentStacks * _intensity;
                }
            }

            return 0f;
        }

        /// <summary>
        /// Gets the effective percent modifier value, accounting for stacks and intensity.
        /// </summary>
        public float GetEffectivePercentModifier(Stats.UnboundedStatType statType)
        {
            if (_effectType?.PercentModifiers == null)
            {
                return 0f;
            }

            foreach (var mod in _effectType.PercentModifiers)
            {
                if (mod.StatType == statType)
                {
                    return mod.Value * _currentStacks * _intensity;
                }
            }

            return 0f;
        }

        /// <summary>
        /// Gets the effective health change per turn, accounting for stacks and intensity.
        /// </summary>
        public int GetEffectiveHealthChangePerTurn() => _effectType == null ? 0 : Mathf.RoundToInt(_effectType.HealthChangePerTurn * _currentStacks * _intensity);
    }
}
