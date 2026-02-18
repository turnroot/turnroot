using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using Turnroot.Gameplay.Objects;
using Turnroot.Gameplay.Objects.Components;
using Turnroot.GameSettings;
using UnityEngine;

namespace Turnroot.Characters.CharacterClass
{
    /// <summary>
    /// Requirements for equipping or qualifying for a character class.
    /// </summary>
    [Serializable]
    public class ClassRequirements
    {
        [Header("Certification")]
        [Tooltip("Optional certification item required to access this class")]
        public ObjectItem CertificationItem;

        [Header("Weapon Types")]
        [Tooltip("List of all weapon types allowed for this class")]
        public List<WeaponType> AllowedWeaponTypes = new();

        [Header("Experience Requirements")]
        [Tooltip("Minimum level required in the previous class to qualify for this class")]
        [Range(0, 99)]
        public int MinimumLevelRequirement = 1;

        [ShowIf(nameof(ShowRequirementFields))]
        [Tooltip("Experience rank requirements (weapon/skill ranks) required to access this class")]
        public List<ExperienceRequirement> ExperienceRequirements = new();

        [Header("Species Restrictions")]
        [Tooltip("If not empty, only characters of these species can use this class")]
        public List<SpeciesType> AllowedSpecies = new();

        [Header("Stat Minimums")]
        [ShowIf(nameof(ShowRequirementFields))]
        [Tooltip(
            "Minimum bounded stat requirements to change into this class; leave empty for none"
        )]
        public List<Stats.BoundedCharacterStat> MinimumStats = new();

        [Header("Promotion Paths")]
        [HideInInspector]
        [Tooltip("List of class promotion targets this class can advance into")]
        public List<CharacterClassData> PromotionPaths = new();

        // Inspector helper used by ShowIf to hide promotion-path UI when the project is
        // configured to use requirement-based class selection.
        private bool ShowPromotionPaths()
        {
            var settings = GameplayGeneralSettings.Instance;
            return settings != null
                && settings.GetClassSelectionMode()
                    == GameplayGeneralSettings.ClassSelectionMode.PromotionBased;
        }

        // Show requirement-specific fields only when project mode is RequirementBased.
        private bool ShowRequirementFields()
        {
            var settings = GameplayGeneralSettings.Instance;
            return settings != null
                && settings.GetClassSelectionMode()
                    == GameplayGeneralSettings.ClassSelectionMode.RequirementBased;
        }

        /// <summary>
        /// Checks whether a character with the given level and species can equip this class.
        /// </summary>
        public bool CanEquipClass(int characterLevel, SpeciesType characterSpecies)
        {
            // Check level requirement
            if (characterLevel < MinimumLevelRequirement)
            {
                return false;
            }

            // Check species restriction (if any)
            return AllowedSpecies.Count <= 0 || AllowedSpecies.Contains(characterSpecies);
        }

        /// <summary>
        /// Checks whether the given weapon type is allowed by this class.
        /// </summary>
        public bool IsWeaponTypeAllowed(WeaponType weaponType) =>
            AllowedWeaponTypes.Contains(weaponType);

        /// <summary>
        /// Checks whether this class has any promotion paths available.
        /// </summary>
        public bool HasPromotionPaths() => PromotionPaths != null && PromotionPaths.Count > 0;

        /// <summary>
        /// Gets human-readable description of requirements.
        /// </summary>
        public string GetRequirementsDescription()
        {
            var parts = new List<string>();

            if (MinimumLevelRequirement > 1)
            {
                parts.Add($"Level {MinimumLevelRequirement}+");
            }

            if (MinimumStats != null && MinimumStats.Count > 0)
            {
                var statParts = MinimumStats
                    .Where(s => s != null)
                    .Select(s => $"{s.DisplayName} {s.GetCurrent()}");
                parts.Add($"Stats: {string.Join(", ", statParts)}");
            }

            if (ExperienceRequirements != null && ExperienceRequirements.Count > 0)
            {
                var expParts = ExperienceRequirements
                    .Where(e => !string.IsNullOrEmpty(e.experienceTypeId))
                    .Select(e => $"{e.experienceTypeId}:{e.minimumRank.Value}");
                parts.Add($"Experience: {string.Join(", ", expParts)}");
            }

            if (CertificationItem != null)
            {
                parts.Add($"Requires {CertificationItem.name}");
            }

            if (AllowedSpecies.Count > 0)
            {
                var speciesNames = string.Join(", ", AllowedSpecies.ConvertAll(s => s.Name));
                parts.Add($"Species: {speciesNames}");
            }

            return parts.Count > 0 ? string.Join(" | ", parts) : "No requirements";
        }
    }
}
