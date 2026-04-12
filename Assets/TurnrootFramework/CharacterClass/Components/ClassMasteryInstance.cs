using System;
using Newtonsoft.Json;
using Turnroot.GameSettings;
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
        [SerializeField, JsonProperty("_progressPercent")]
        private int _progressPercent = 0; // 0..100

        [SerializeField, JsonProperty("_isMastered")]
        private bool _isMastered = false;

        [SerializeField, JsonProperty("_battlesCompleted")]
        private int _battlesCompleted = 0;

        public int BattlesCompleted => _battlesCompleted;
        public int ProgressPercent => _progressPercent;
        public bool IsMastered => _isMastered;

        public ClassMasteryInstance() { }

        public ClassMasteryInstance(CharacterInstance owner, CharacterClassData classData)
        {
            _battlesCompleted = 0;
            EnsureMasteryProgressInitialized(classData);
        }

        public void OnAfterDeserialize() { }

        public void EnsureMasteryProgressInitialized(CharacterClassData classData)
        {
            // Ensure progress is within bounds and set mastered flag if threshold already met.
            _progressPercent = Math.Clamp(_progressPercent, 0, 100);
            _isMastered = _isMastered || (classData?.Mastery != null && _progressPercent >= 100);
        }

        private void UnlockMasteredSkill(CharacterInstance owner, CharacterClassData classData)
        {
            if (classData?.Mastery == null || classData.Mastery.MasteredSkill == null)
            {
                return;
            }

            var skill = classData.Mastery.MasteredSkill;
            _isMastered = true;

            if (owner != null)
            {
                var exists = owner.SkillInstances?.Find(s => s.SkillTemplate == skill) != null;
                if (!exists)
                {
                    owner.AddSkill(skill);
                    $"{owner.CharacterTemplate?.DisplayName ?? owner.Id}: unlocked mastery skill '{skill.SkillName}' from class '{classData?.GetClassName() ?? "<unknown>"}'".LogInfo();
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
            if (owner != null)
                owner.NeedsPersist = true;
        }

        /// <summary>
        /// Add progress points toward mastery (percent points; 0..100 scale).
        /// </summary>
        public void AddProgress(CharacterInstance owner, CharacterClassData classData, int points)
        {
            if (classData == null || classData.Mastery == null)
            {
                return;
            }

            if (_isMastered)
            {
                return;
            }

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
                100
            );

            if (_progressPercent >= 100)
            {
                UnlockMasteredSkill(owner, classData);
            }
        }
    }
}
