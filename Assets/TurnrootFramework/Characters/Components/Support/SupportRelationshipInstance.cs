using System;
using Turnroot.Characters.Subclasses;
using Turnroot.GameSettings;
using UnityEngine;

namespace Turnroot.Characters.Components.Support
{
    /// <summary>
    /// Represents an instance of a support relationship between characters with progression tracking.
    /// </summary>
    [Serializable]
    public class SupportRelationshipInstance
    {
        [SerializeField]
        private CharacterData _character;

        [SerializeField]
        private SupportLevels _supportLevels;

        [SerializeField]
        private string _maxLevel;

        [SerializeField]
        private int _supportSpeed = 1;

        [SerializeField]
        private float _supportGainMultiplier = 1f;

        [SerializeField]
        private float _supportPoints = 0f;

        public SupportRelationshipInstance()
        {
            _supportLevels = new SupportLevels { Value = "E" };
            _maxLevel = "A";
            _supportSpeed = 1;
        }

        public SupportRelationshipInstance(SupportRelationship template)
        {
            _character = template.Character;
            _supportLevels = new SupportLevels { Value = template.SupportLevel.Value };
            _maxLevel = template.MaxLevel;
            _supportSpeed = template.SupportSpeed;
        }

        public SupportRelationshipInstance(
            SupportRelationshipTable.SupportPairing pairing,
            CharacterData partner
        )
        {
            _character = partner;
            _supportLevels = new SupportLevels { Value = "E" };
            _maxLevel = pairing.MaxSupportLevel?.Value ?? "A";
            _supportSpeed = 1;
            _supportGainMultiplier =
                pairing.SupportGainMultiplier > 0f ? pairing.SupportGainMultiplier : 1f;
        }

        public CharacterData Character
        {
            get => _character;
            set => _character = value;
        }
        public string MaxLevel
        {
            get => _maxLevel;
            set => _maxLevel = value;
        }
        public int SupportSpeed
        {
            get => _supportSpeed;
            set => _supportSpeed = value;
        }
        public float SupportPoints
        {
            get => _supportPoints;
            set => _supportPoints = value;
        }

        public string CurrentLevel => _supportLevels.Value;

        public void Increase(float points)
        {
            _supportPoints += points * _supportSpeed * _supportGainMultiplier;
            while (_supportPoints >= 100f)
            {
                _supportLevels.Increase();
                _supportPoints -= 100f;
            }
        }

        public void Decrease(float points)
        {
            _supportPoints -= points;

            if (_supportPoints < 0f)
            {
                _supportPoints = 0f;
            }
        }

        /// <summary>
        /// Subtracts 100 points and advances the support level by one rank.
        /// Call only when <see cref="SupportPoints"/> >= 100.
        /// Fires <see cref="Brain.PublishSupportLevelIncreased"/> via the conversational brain.
        /// </summary>
        public void IncreaseSupportLevel(
            CharacterInstance owner,
            Gameplay.Brain.ConversationalBrain conversationalBrain
        )
        {
            _supportPoints -= 100f;
            if (_supportPoints < 0f)
            {
                _supportPoints = 0f;
            }

            _supportLevels.Increase();
            conversationalBrain?.NotifySupportLevelIncreased(owner, this);
        }

        /* ------------------------ Recruitment Overrides ------------------------ */
        [SerializeField]
        private bool _hasIsRecruitableOverride;

        [SerializeField]
        private bool _isRecruitableOverride;

        [SerializeField]
        private bool _hasRecruitmentChanceOverride;

        [SerializeField]
        private float _recruitmentChanceOverride;

        [SerializeField]
        private bool _hasRecruitmentIncreaseOverride;

        [SerializeField]
        private float _recruitmentChanceIncreasePerConversationOverride;

        [SerializeField]
        private bool _hasRequiresMinSupportLevelOverride;

        [SerializeField]
        private bool _requiresMinSupportLevelOverride;

        public bool GetIsRecruitable() =>
            _hasIsRecruitableOverride
                ? _isRecruitableOverride
                : (_character?.IsRecruitable ?? false);

        public void SetIsRecruitableOverride(bool value)
        {
            _hasIsRecruitableOverride = true;
            _isRecruitableOverride = value;
        }

        public void ClearIsRecruitableOverride() => _hasIsRecruitableOverride = false;

        public float GetRecruitmentChance() =>
            _hasRecruitmentChanceOverride
                ? _recruitmentChanceOverride
                : (_character?.RecruitmentChance ?? 0f);

        public void SetRecruitmentChanceOverride(float value)
        {
            _hasRecruitmentChanceOverride = true;
            _recruitmentChanceOverride = value;
        }

        public void ClearRecruitmentChanceOverride() => _hasRecruitmentChanceOverride = false;

        public float GetRecruitmentChanceIncreasePerConversation() =>
            _hasRecruitmentIncreaseOverride
                ? _recruitmentChanceIncreasePerConversationOverride
                : (_character?.RecruitmentChanceIncreasePerConversation ?? 0f);

        public void SetRecruitmentChanceIncreasePerConversationOverride(float value)
        {
            _hasRecruitmentIncreaseOverride = true;
            _recruitmentChanceIncreasePerConversationOverride = value;
        }

        public void ClearRecruitmentChanceIncreasePerConversationOverride() =>
            _hasRecruitmentIncreaseOverride = false;

        public bool GetRequiresMinSupportLevel() =>
            _hasRequiresMinSupportLevelOverride
                ? _requiresMinSupportLevelOverride
                : (_character?.RequiresMinSupportLevel ?? false);

        public void SetRequiresMinSupportLevelOverride(bool value)
        {
            _hasRequiresMinSupportLevelOverride = true;
            _requiresMinSupportLevelOverride = value;
        }

        public void ClearRequiresMinSupportLevelOverride() =>
            _hasRequiresMinSupportLevelOverride = false;
    }
}
