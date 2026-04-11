using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Turnroot.Characters.StatusEffects
{
    /// <summary>
    /// Runtime instance of an active status effect on a character.
    /// </summary>
    [Serializable]
    public class StatusEffectInstance
    {
        [SerializeField, JsonProperty("_effectType")]
        private StatusEffectType _effectType;

        [SerializeField, JsonProperty("RemainingDuration")]
        private int _remainingDuration;

        [SerializeField, JsonProperty("CurrentStacks")]
        private int _currentStacks;

        [SerializeField, JsonProperty("_sourceCharacterId")]
        private string _sourceCharacterId;

        [SerializeField, JsonProperty("_sourceSkillId")]
        private string _sourceSkillId;

        [SerializeField, JsonProperty("_intensity")]
        private float _intensity;
        public StatusEffectType EffectType => _effectType;

        [JsonIgnore]
        public int RemainingDuration => _remainingDuration;

        [JsonIgnore]
        public int CurrentStacks => _currentStacks;
        public string SourceCharacterId => _sourceCharacterId;

        public string SourceSkillId => _sourceSkillId;

        public float Intensity => _intensity;

        public bool IsExpired => _remainingDuration < 0;

        public bool IsPermanent =>
            _effectType?.DefaultDuration == 0 || _remainingDuration == int.MaxValue;

        public StatusEffectInstance(
            StatusEffectType effectType,
            string sourceCharacterId = null,
            string sourceSkillId = null,
            int? duration = null,
            float intensity = 1f
        )
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

        public bool TickDuration()
        {
            if (IsPermanent)
            {
                return true;
            }

            _remainingDuration--;
            return _remainingDuration >= 0;
        }

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

        public void RefreshDuration()
        {
            _remainingDuration = _effectType?.DefaultDuration ?? 3;
            if (_remainingDuration == 0)
            {
                _remainingDuration = int.MaxValue;
            }
        }

        public void RefreshDuration(int newDuration)
        {
            _remainingDuration = newDuration;
            if (_remainingDuration == 0)
            {
                _remainingDuration = int.MaxValue;
            }
        }

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

        public int GetEffectiveHealthChangePerTurn() =>
            _effectType == null
                ? 0
                : Mathf.RoundToInt(_effectType.HealthChangePerTurn * _currentStacks * _intensity);
    }
}
