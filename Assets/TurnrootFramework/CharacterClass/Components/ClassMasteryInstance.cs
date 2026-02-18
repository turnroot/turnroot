using System;
using System.Collections.Generic;
using Turnroot.GameSettings;
using Turnroot.Skills;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Characters.CharacterClass
{
    /// <summary>
    /// Runtime/per-instance bookkeeping and logic for class mastery (moved out of CharacterClassDataInstance).
    /// Keeps progress, unlocked skills and battle/level tracking here so the class instance stays thin.
    /// </summary>
    [Serializable]
    public class ClassMasteryInstance : Serialization.IPostDeserialize
    {
        [SerializeField]
        private int _progressPercent = 0; // 0..100

        [SerializeField]
        private bool _isMastered = false;

        [SerializeField]
        private int _battlesCompleted = 0;

        [SerializeField]
        private int _levelWhenEquipped = 1;

        public int BattlesCompleted => _battlesCompleted;
        public int LevelWhenEquipped => _levelWhenEquipped;

        public int ProgressPercent => _progressPercent;
        public bool IsMastered => _isMastered;

        public ClassMasteryInstance() { }

        public ClassMasteryInstance(
            CharacterInstance owner,
            CharacterClassData classData,
            int levelWhenEquipped
        )
        {
            _levelWhenEquipped = levelWhenEquipped;
            _battlesCompleted = 0;
            EnsureMasteryProgressInitialized(classData);
        }

        public void OnAfterDeserialize()
        {
            // No owner/class reference available here; caller should ensure initialization with classData when possible.
        }

        public void EnsureMasteryProgressInitialized(CharacterClassData classData)
        {
            // Ensure progress is within bounds and set mastered flag if threshold already met.
            _progressPercent = Math.Clamp(_progressPercent, 0, 100);
            _isMastered =
                _isMastered
                || (
                    classData?.Mastery != null
                    && _progressPercent >= Math.Clamp(classData.Mastery.MasteryThreshold, 1, 100)
                );
        }

        // Backwards-compatible signature: treat any targetIndex as the single mastery slot.
        public bool IsMasteryTargetUnlocked(CharacterClassData classData, int targetIndex)
        {
            return classData?.Mastery != null && _isMastered;
        }

        // Kept name for backward compatibility; unlocks the class's single mastered skill.
        private void UnlockMasteryTarget(
            CharacterInstance owner,
            CharacterClassData classData,
            int targetIndex
        )
        {
            UnlockMasteredSkill(owner, classData);
        }

        private void UnlockMasteredSkill(CharacterInstance owner, CharacterClassData classData)
        {
            if (classData?.Mastery == null || classData.Mastery.MasteredSkill == null)
                return;

            var skill = classData.Mastery.MasteredSkill;
            _isMastered = true;

            if (owner != null)
            {
                var exists = owner.SkillInstances?.Find(s => s.SkillTemplate == skill) != null;
                if (!exists)
                {
                    owner.AddSkill(skill);
                    TurnrootLogger.Log(
                        $"{owner.CharacterTemplate?.DisplayName ?? owner.Id}: unlocked mastery skill '{skill.SkillName}' from class '{classData?.GetClassName() ?? "<unknown>"}'",
                        TurnrootLogger.LogLevel.Info
                    );
                }
            }

            var brain = UnityEngine.Object.FindFirstObjectByType<Gameplay.Brain.Brain>();
            brain?.PublishCharacterClassMasteryTargetUnlocked(owner, classData, 0, skill);
        }

        /// <summary>
        /// Increment battle-based usage for this class instance.
        /// </summary>
        public void IncrementBattleCount(
            CharacterInstance owner,
            CharacterClassData classData,
            int points = 1
        )
        {
            var settings = GameplayGeneralSettings.Instance;
            int effectivePoints = Math.Max(0, points);
            if (settings != null)
            {
                var mt = settings.MasterySettings;
                float multiplier = mt.BattlePointMultiplier;
                if (points > 1)
                {
                    multiplier *= mt.BattleSuccessMultiplier;
                }
                effectivePoints = Mathf.CeilToInt(points * multiplier);
                effectivePoints = Math.Max(0, effectivePoints);
            }

            _battlesCompleted += effectivePoints;
            AddProgress(owner, classData, effectivePoints);
        }

        /// <summary>
        /// Add progress points toward mastery (percent points; 0..100 scale).
        /// </summary>
        public void AddProgress(CharacterInstance owner, CharacterClassData classData, int points)
        {
            if (classData == null || classData.Mastery == null || classData.Mastery == null)
                return;
            if (_isMastered)
                return;

            var settings = GameplayGeneralSettings.Instance;
            int effectivePoints = Math.Max(0, points);
            if (settings != null)
            {
                var mt = settings.MasterySettings;
                effectivePoints = Mathf.CeilToInt(effectivePoints * mt.BattlePointMultiplier);
            }

            _progressPercent = Math.Clamp(_progressPercent + effectivePoints, 0, 100);

            var brain = UnityEngine.Object.FindFirstObjectByType<Gameplay.Brain.Brain>();
            brain?.PublishCharacterClassMasteryProgressChanged(
                owner,
                classData,
                0,
                _progressPercent,
                classData.Mastery.MasteryThreshold
            );

            if (_progressPercent >= Math.Clamp(classData.Mastery.MasteryThreshold, 1, 100))
            {
                UnlockMasteredSkill(owner, classData);
            }
        }

        // Expose setter for level when equipped so caller can initialize from owner
        public void SetLevelWhenEquipped(int level)
        {
            _levelWhenEquipped = level;
        }
    }
}
