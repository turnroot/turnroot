using System;
using Turnroot.Characters.Subclasses;
using UnityEngine;

namespace Turnroot.Characters.Components.Support
{
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
        private int _supportPoints = 0;

        public SupportRelationshipInstance()
        {
            _supportLevels = new SupportLevels { Value = "E" };
            _maxLevel = "A";
            _supportSpeed = 1;
        }

        [SerializeField]
        private GameplayGeneralSettings.SupportBonus _supportBonusOverride;

        [SerializeField]
        private bool _hasSupportBonusOverride;

        public SupportRelationshipInstance(SupportRelationship template)
        {
            _character = template.Character;
            _supportLevels = new SupportLevels { Value = template.SupportLevel.Value };
            _maxLevel = template.MaxLevel;
            _supportSpeed = template.SupportSpeed;

            // copy override values from template when present
            _hasSupportBonusOverride = template.HasSupportBonusOverride;
            _supportBonusOverride = template.GetSupportBonusOverride();
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
        public int SupportPoints
        {
            get => _supportPoints;
            set => _supportPoints = value;
        }

        public string CurrentLevel => _supportLevels.Value;

        public void Increase(int points)
        {
            _supportPoints += points * _supportSpeed;
            if (_supportPoints >= 100)
            {
                _supportLevels.Increase();
            }
        }

        public void Decrease(int points)
        {
            _supportPoints -= points;

            if (_supportPoints < 0)
            {
                _supportPoints = 0;
            }
        }

        /// <summary>
        /// Whether this instance includes a support-bonus override.
        /// </summary>
        public bool HasSupportBonusOverride() => _hasSupportBonusOverride;

        /// <summary>
        /// Returns the override support bonus for this relationship instance.
        /// </summary>
        public GameplayGeneralSettings.SupportBonus GetSupportBonusOverride() =>
            _supportBonusOverride;

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
