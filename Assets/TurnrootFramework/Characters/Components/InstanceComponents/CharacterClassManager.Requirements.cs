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

            var levelContribs = new List<float>();
            var statContribs = new List<float>();
            var expContribs = new List<float>();

            if (classData.Requirements.MinimumLevelRequirement > 1)
            {
                var denom = classData.Requirements.MinimumLevelRequirement;
                var ratio = denom <= 0 ? 1f : Mathf.Clamp01((float)CurrentLevel / denom);
                levelContribs.Add(ratio);
            }

            if (
                classData.Requirements.MinimumStats != null
                && classData.Requirements.MinimumStats.Count > 0
            )
            {
                foreach (var req in classData.Requirements.MinimumStats.Where(s => s != null))
                {
                    var need = req.GetCurrent();
                    if (need <= 0)
                    {
                        statContribs.Add(1f);
                        continue;
                    }

                    var curStat = GetBoundedStat(req.StatType)?.GetCurrent() ?? 0;
                    var ratio = Mathf.Clamp01(curStat / (float)need);
                    statContribs.Add(ratio);
                }
            }

            if (
                classData.Requirements.ExperienceRequirements != null
                && classData.Requirements.ExperienceRequirements.Count > 0
            )
            {
                foreach (
                    var req in classData.Requirements.ExperienceRequirements.Where(r =>
                        !string.IsNullOrEmpty(r.experienceTypeId)
                    )
                )
                {
                    int MapRank(string v)
                    {
                        return v switch
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

                    var requiredNumeric = MapRank(req.minimumRank.Value);
                    if (requiredNumeric <= 0)
                    {
                        expContribs.Add(1f);
                        continue;
                    }

                    var inst = GetExperienceRank(req.experienceTypeId);
                    var currentNumeric = inst != null ? MapRank(inst.Rank.Value) : 0;
                    var ratio = Mathf.Clamp01(currentNumeric / (float)requiredNumeric);
                    expContribs.Add(ratio);
                }
            }

            if (levelContribs.Count + statContribs.Count + expContribs.Count == 0)
            {
                return 1f;
            }

            var settings = GameplayGeneralSettings.Instance;
            if (settings == null)
            {
                var all = new List<float>();
                all.AddRange(levelContribs);
                all.AddRange(statContribs);
                all.AddRange(expContribs);
                var avg = all.Sum() / all.Count;
                return Mathf.Clamp01(avg);
            }

            float levelAvg =
                levelContribs.Count > 0 ? levelContribs.Sum() / levelContribs.Count : 0f;
            float statAvg = statContribs.Count > 0 ? statContribs.Sum() / statContribs.Count : 0f;
            float expAvg = expContribs.Count > 0 ? expContribs.Sum() / expContribs.Count : 0f;

            var tune = settings.RequirementExamSettings;
            float wLevel = tune.WeightLevel * (levelContribs.Count > 0 ? 1f : 0f);
            float wStats = tune.WeightStats * (statContribs.Count > 0 ? 1f : 0f);
            float wExp = tune.WeightExperience * (expContribs.Count > 0 ? 1f : 0f);
            float totalWeight = wLevel + wStats + wExp;

            if (totalWeight <= 0f)
            {
                var all = new List<float>();
                all.AddRange(levelContribs);
                all.AddRange(statContribs);
                all.AddRange(expContribs);
                var avg = all.Sum() / all.Count;
                return Mathf.Clamp01(Mathf.Max(avg, tune.ExamFloor));
            }

            var weighted = (levelAvg * wLevel + statAvg * wStats + expAvg * wExp) / totalWeight;
            var chance = Mathf.Clamp01(Mathf.Max(weighted, tune.ExamFloor));
            return chance;
        }

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

