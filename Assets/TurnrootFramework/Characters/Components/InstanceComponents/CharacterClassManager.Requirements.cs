using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters.CharacterClass;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Characters
{
    public partial class CharacterInstance
    {
        #region Requirement Checks

        public bool MeetsClassRequirements(CharacterClassData classData)
        {
            if (classData == null)
            {
                return false;
            }

            // Check level requirement
            if (_currentLevel < classData.Requirements.MinimumLevelRequirement)
            {
                return false;
            }

            // Check class tier progression
            if (!ValidateClassTierProgression(classData))
            {
                return false;
            }

            // Check species restrictions
            if (!IsSpeciesAllowed(classData))
            {
                return false;
            }

            // Certification item requirement (designer can attach an Item template on the class)
            if (!HasRequiredCertification(classData))
            {
                return false;
            }

            // Minimum bounded stat checks (strict)
            if (
                classData.Requirements.MinimumStats != null
                && classData.Requirements.MinimumStats.Count > 0
            )
            {
                foreach (var reqStat in classData.Requirements.MinimumStats.Where(s => s != null))
                {
                    var cur = GetBoundedStat(reqStat.StatType)?.GetCurrent() ?? 0;
                    var need = reqStat.GetCurrent();
                    if (cur < need)
                    {
                        return false;
                    }
                }
            }

            // Experience/weapon-rank requirements (strict)
            if (
                classData.Requirements.ExperienceRequirements != null
                && classData.Requirements.ExperienceRequirements.Count > 0
            )
            {
                foreach (var req in classData.Requirements.ExperienceRequirements)
                {
                    if (string.IsNullOrEmpty(req.experienceTypeId))
                    {
                        continue;
                    }

                    if (!MeetsExperienceRequirement(req.experienceTypeId, req.minimumRank.Value))
                    {
                        return false;
                    }
                }
            }

            // Check pronoun restrictions
            if (classData.allowedPronounKeys != null && classData.allowedPronounKeys.Count > 0)
            {
                string currentPronounKey = _characterTemplate.CharacterPronouns.GetPronounKey();
                if (!classData.allowedPronounKeys.Contains(currentPronounKey))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ValidateClassTierProgression(CharacterClassData targetClass)
        {
            // If no current class, any tier is allowed (starting class)
            if (_currentClass == null || _currentClass.ClassData == null)
            {
                return true;
            }

            var currentTier = _currentClass.ClassData.Identity.ClassTier;
            var targetTier = targetClass.Identity.ClassTier;

            var selectionMode =
                GameplayGeneralSettings.Instance?.GetClassSelectionMode()
                ?? GameplayGeneralSettings.ClassSelectionMode.PromotionBased;

            if (selectionMode == GameplayGeneralSettings.ClassSelectionMode.PromotionBased)
            {
                if (targetTier > currentTier + 1)
                {
                    $"Cannot change from {currentTier} class to {targetTier} class - must progress one tier at a time (PromotionBased).".LogWarning();
                    return false;
                }
                return true;
            }

            if (targetTier < currentTier)
            {
                $"Cannot change from {currentTier} class to lower-tier {targetTier} class while ClassSelection=RequirementBased.".LogWarning();
                return false;
            }

            return true;
        }

        private float CalculateRequirementPassChance(CharacterClassData classData)
        {
            if (classData == null || classData.Requirements == null)
            {
                return 0f;
            }

            // Immediate fails for absolute mismatches
            if (!IsSpeciesAllowed(classData))
            {
                return 0f;
            }

            if (!HasRequiredCertification(classData))
            {
                return 0f;
            }

            var levelContribs = GetLevelContributions(classData);
            var statContribs = GetStatContributions(classData);
            var expContribs = GetExperienceContributions(classData);

            if (levelContribs.Count + statContribs.Count + expContribs.Count == 0)
            {
                return 1f;
            }

            var settings = GameplayGeneralSettings.Instance;
            if (settings == null)
            {
                return Mathf.Clamp01(AverageContributions(levelContribs, statContribs, expContribs));
            }

            return CalculateWeightedPassChance(
                settings,
                levelContribs,
                statContribs,
                expContribs
            );
        }

        private List<float> GetLevelContributions(CharacterClassData classData)
        {
            var contributions = new List<float>();
            int minimumLevel = classData.Requirements.MinimumLevelRequirement;
            if (minimumLevel > 1)
            {
                float ratio = minimumLevel <= 0
                    ? 1f
                    : Mathf.Clamp01((float)CurrentLevel / minimumLevel);
                contributions.Add(ratio);
            }

            return contributions;
        }

        private List<float> GetStatContributions(CharacterClassData classData)
        {
            var contributions = new List<float>();
            var requirements = classData.Requirements.MinimumStats;
            if (requirements == null || requirements.Count == 0)
            {
                return contributions;
            }

            foreach (var requirement in requirements.Where(stat => stat != null))
            {
                int needed = requirement.GetCurrent();
                if (needed <= 0)
                {
                    contributions.Add(1f);
                    continue;
                }

                int current = GetBoundedStat(requirement.StatType)?.GetCurrent() ?? 0;
                contributions.Add(Mathf.Clamp01(current / (float)needed));
            }

            return contributions;
        }

        private List<float> GetExperienceContributions(CharacterClassData classData)
        {
            var contributions = new List<float>();
            var requirements = classData.Requirements.ExperienceRequirements;
            if (requirements == null || requirements.Count == 0)
            {
                return contributions;
            }

            foreach (
                var requirement in requirements.Where(item =>
                    !string.IsNullOrEmpty(item.experienceTypeId)
                )
            )
            {
                int requiredRank = MapRank(requirement.minimumRank.Value);
                if (requiredRank <= 0)
                {
                    contributions.Add(1f);
                    continue;
                }

                var instance = GetExperienceRank(requirement.experienceTypeId);
                int currentRank = instance != null ? MapRank(instance.Rank.Value) : 0;
                contributions.Add(Mathf.Clamp01(currentRank / (float)requiredRank));
            }

            return contributions;
        }

        private static int MapRank(string rank)
        {
            return rank switch
            {
                CommonAncestors.LeveledLetteredField.S => 5,
                CommonAncestors.LeveledLetteredField.A => 4,
                CommonAncestors.LeveledLetteredField.B => 3,
                CommonAncestors.LeveledLetteredField.C => 2,
                CommonAncestors.LeveledLetteredField.D => 1,
                CommonAncestors.LeveledLetteredField.E => 0,
                _ => 0,
            };
        }

        private float CalculateWeightedPassChance(
            GameplayGeneralSettings settings,
            List<float> levelContributions,
            List<float> statContributions,
            List<float> experienceContributions
        )
        {
            float levelAverage = AverageOrZero(levelContributions);
            float statAverage = AverageOrZero(statContributions);
            float experienceAverage = AverageOrZero(experienceContributions);
            var tune = settings.RequirementExamSettings;
            float levelWeight = tune.WeightLevel * (levelContributions.Count > 0 ? 1f : 0f);
            float statWeight = tune.WeightStats * (statContributions.Count > 0 ? 1f : 0f);
            float experienceWeight =
                tune.WeightExperience * (experienceContributions.Count > 0 ? 1f : 0f);
            float totalWeight = levelWeight + statWeight + experienceWeight;

            float score = totalWeight <= 0f
                ? AverageContributions(levelContributions, statContributions, experienceContributions)
                : (
                    levelAverage * levelWeight
                    + statAverage * statWeight
                    + experienceAverage * experienceWeight
                ) / totalWeight;
            return Mathf.Clamp01(Mathf.Max(score, tune.ExamFloor));
        }

        private static float AverageContributions(params List<float>[] contributionGroups)
        {
            float sum = 0f;
            int count = 0;
            foreach (var group in contributionGroups)
            {
                sum += group.Sum();
                count += group.Count;
            }

            return sum / count;
        }

        private static float AverageOrZero(List<float> contributions) =>
            contributions.Count > 0 ? contributions.Sum() / contributions.Count : 0f;

        #endregion

        #region Promotions

        public List<CharacterClassData> GetAvailablePromotions()
        {
            var available = new List<CharacterClassData>();

            if (_currentClass == null || _currentClass.ClassData == null)
            {
                return available;
            }

            var promotionPaths = _currentClass.ClassData.Requirements.PromotionPaths;
            if (promotionPaths == null || promotionPaths.Count == 0)
            {
                return available;
            }

            foreach (var promotionClass in promotionPaths)
            {
                if (promotionClass != null && MeetsClassRequirements(promotionClass))
                {
                    available.Add(promotionClass);
                }
            }

            return available;
        }

        public bool HasEquippedClass(CharacterClassData classData) =>
            classData != null && _equippedClassHistory.Contains(classData);

        #endregion

        #region Helpers

        private bool IsSpeciesAllowed(CharacterClassData classData)
        {
            var allowed = classData?.Requirements?.AllowedSpecies;
            return allowed == null
                || allowed.Count == 0
                || allowed.Contains(_characterTemplate.Species);
        }

        private bool HasRequiredCertification(CharacterClassData classData)
        {
            var cert = classData?.Requirements?.CertificationItem;
            if (cert == null)
            {
                return true;
            }

            var inv = InventoryInstance;
            return inv != null && inv.Items().Any(i => i?.Template == cert);
        }

        #endregion
    }
}
