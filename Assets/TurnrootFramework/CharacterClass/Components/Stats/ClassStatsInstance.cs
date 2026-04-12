using System;
using Newtonsoft.Json;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Characters.CharacterClass
{
    /// <summary>
    /// Runtime stat-related behaviour for a class (moved from CharacterClassDataInstance).
    /// Contains the "is first time equipped" flag and stat application / enforcement wrappers.
    /// </summary>
    [Serializable]
    public class ClassStatsInstance
    {
        [SerializeField, JsonProperty("_isFirstTimeEquipped")]
        private bool _isFirstTimeEquipped = true;

        public bool IsFirstTimeEquipped
        {
            get => _isFirstTimeEquipped;
            set => _isFirstTimeEquipped = value;
        }

        public ClassStatsInstance() { }

        public ClassStatsInstance(bool isFirstTime)
        {
            _isFirstTimeEquipped = isFirstTime;
        }

        public OperationResult ApplyClassBonuses(
            CharacterInstance character,
            CharacterClassData classData
        )
        {
            var validation = StatApplicationHelper.ValidateReferences(
                character,
                classData,
                "ClassStatsInstance.ApplyClassBonuses"
            );
            if (!validation.Success)
            {
                return validation;
            }

            StatApplicationHelper.ApplyBoundedBonuses(classData.Stats.StatBonuses, character);
            StatApplicationHelper.ApplyUnboundedBonuses(
                classData.Stats.UnboundedStatBonuses,
                character
            );

            if (classData?.Mastery?.InnateSkills != null && character.SkillInstances != null)
            {
                foreach (var skill in classData.Mastery.InnateSkills)
                {
                    if (skill == null)
                    {
                        continue;
                    }

                    var exists =
                        character.SkillInstances.Find(s => s.SkillTemplate == skill) != null;
                    if (!exists)
                    {
                        character.AddSkill(skill);
                    }
                }
            }

            var brain = UnityEngine.Object.FindFirstObjectByType<Gameplay.Brain.Brain>();
            brain?.PublishCharacterClassBonusesApplied(character, classData);

            return OperationResult.Successful();
        }

        public OperationResult RemoveClassBonuses(
            CharacterInstance character,
            CharacterClassData classData
        )
        {
            var validation = StatApplicationHelper.ValidateReferences(
                character,
                classData,
                "ClassStatsInstance.RemoveClassBonuses"
            );
            if (!validation.Success)
            {
                return validation;
            }

            StatApplicationHelper.RemoveBoundedBonuses(classData.Stats.StatBonuses, character);
            StatApplicationHelper.RemoveUnboundedBonuses(
                classData.Stats.UnboundedStatBonuses,
                character
            );

            if (classData?.Mastery?.InnateSkills != null && character.SkillInstances != null)
            {
                foreach (var skill in classData.Mastery.InnateSkills)
                {
                    if (!ValidationHelper.ValidateNotNull(skill, nameof(skill)))
                    {
                        continue;
                    }

                    var instance = character.SkillInstances.Find(s => s.SkillTemplate == skill);
                    if (instance != null)
                    {
                        character.RemoveSkill(instance);
                    }
                }
            }

            var brain = UnityEngine.Object.FindFirstObjectByType<Gameplay.Brain.Brain>();
            brain?.PublishCharacterClassBonusesRemoved(character, classData);

            return OperationResult.Successful();
        }

        public OperationResult ApplyClassChangeBonuses(
            CharacterInstance character,
            CharacterClassData classData
        )
        {
            if (!_isFirstTimeEquipped)
            {
                return OperationResult.Successful();
            }

            var validation = StatApplicationHelper.ValidateReferences(
                character,
                classData,
                "ClassStatsInstance.ApplyClassChangeBonuses"
            );
            if (!validation.Success)
            {
                return validation;
            }

            StatApplicationHelper.ApplyBoundedPermanentBonuses(
                classData.Stats.ClassChangeBonuses,
                character,
                logChanges: true
            );
            StatApplicationHelper.ApplyUnboundedPermanentBonuses(
                classData.Stats.UnboundedClassChangeBonuses,
                character,
                logChanges: true
            );

            _isFirstTimeEquipped = false;
            return OperationResult.Successful();
        }

        public void EnforceStatMinimums(CharacterInstance character, CharacterClassData classData)
        {
            var validation = StatApplicationHelper.ValidateReferences(
                character,
                classData,
                "ClassStatsInstance.EnforceStatMinimums"
            );
            if (!validation.Success)
            {
                return;
            }

            StatApplicationHelper.EnforceBoundedMinimums(
                classData.Stats.StatMinimums,
                character,
                logChanges: true
            );
            StatApplicationHelper.EnforceUnboundedMinimums(
                classData.Stats.UnboundedStatMinimums,
                character,
                logChanges: true
            );
        }

        public void ApplyStatCaps(CharacterInstance character, CharacterClassData classData)
        {
            var validation = StatApplicationHelper.ValidateReferences(
                character,
                classData,
                "ClassStatsInstance.ApplyStatCaps"
            );
            if (!validation.Success)
            {
                return;
            }

            StatApplicationHelper.ApplyBoundedCaps(classData.Stats.StatCaps, character);
        }

        public bool IsAboveCaps(CharacterInstance character, CharacterClassData classData)
        {
            var validation = StatApplicationHelper.ValidateReferences(character, classData, "");
            return validation.Success
                && StatApplicationHelper.IsAboveUnboundedCaps(
                    classData.Stats.UnboundedStatCaps,
                    character
                );
        }
    }
}
