using System;
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.ItemTool;
using UnityEngine;

namespace Turnroot.Characters.CharacterClass
{
    /// <summary>
    /// Requirements for equipping or qualifying for a character class.
    /// Extracted from CharacterClassData for cleaner organization.
    /// </summary>
    [Serializable]
    public class ClassRequirements
    {
        [Header("Certification")]
        [Tooltip("Optional certification item required to access this class")]
        public ItemData CertificationItem;

        [Header("Weapon Types")]
        [Tooltip("List of all weapon types allowed for this class")]
        public List<WeaponType> AllowedWeaponTypes = new();

        [Header("Experience Requirements")]
        [Tooltip("Minimum level required in the previous class to qualify for this class")]
        [Range(0, 99)]
        public int MinimumLevelRequirement = 1;

        [Header("Species Restrictions")]
        [Tooltip("If not empty, only characters of these species can use this class")]
        public List<SpeciesType> AllowedSpecies = new();

        [Header("Promotion Paths")]
        [Tooltip("List of class promotion targets this class can advance into")]
        public List<CharacterClassData> PromotionPaths = new();

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
            if (AllowedSpecies.Count > 0 && !AllowedSpecies.Contains(characterSpecies))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks whether the given weapon type is allowed by this class.
        /// </summary>
        public bool IsWeaponTypeAllowed(WeaponType weaponType)
        {
            return AllowedWeaponTypes.Contains(weaponType);
        }

        /// <summary>
        /// Checks whether this class has any promotion paths available.
        /// </summary>
        public bool HasPromotionPaths()
        {
            return PromotionPaths != null && PromotionPaths.Count > 0;
        }

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

            if (CertificationItem != null)
            {
                parts.Add($"Requires {CertificationItem.ItemName}");
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
