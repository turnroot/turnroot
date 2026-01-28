using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters.CharacterClass;
using Turnroot.Gameplay.Objects;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Characters
{
    /// <summary>
    /// Handles class management, class changes, and class-related requirements.
    /// </summary>
    public partial class CharacterInstance
    {
        public Dictionary<string, Material> classNameToOutfitMaterials = new();

        #region Battle Helpers

        public ObjectItemInstance GetEquippedWeapon()
        {
            var allowedWeapons = _currentClass.ClassData.Requirements.AllowedWeaponTypes;

            var inventory = _inventoryInstance.Items();

            // return the weapon in slot 0
            foreach (
                var weapon in inventory.Where(w =>
                    w.Template != null
                    && allowedWeapons.Contains(w.Template.WeaponType)
                    && w.Slot == 0
                )
            )
            {
                return weapon;
            }
            return null;
        }

        public List<ObjectItemInstance> GetAvailableWeapons()
        {
            var allowedWeapons = _currentClass.ClassData.Requirements.AllowedWeaponTypes;
            var inventory = _inventoryInstance.Items();
            bool hasWeapon = false;
            var weapons = new List<ObjectItemInstance>();

            foreach (
                var weapon in inventory.Where(w =>
                    w.Template != null && allowedWeapons.Contains(w.Template.WeaponType)
                )
            )
            {
                weapons.Add(weapon);
                hasWeapon = true;
            }

            if (!hasWeapon)
            {
                return null;
            }
            else
            {
                // return the first weapon in the lowest slot [0]
                return weapons;
            }
        }

        /// <summary>
        /// Returns the minimum attack range across all equippable weapons in the character's inventory, or 0 if no valid weapons are available.
        /// </summary>
        public int GetMinRange()
        {
            if (_currentClass == null || _currentClass.ClassData == null)
            {
                return 0;
            }

            var allowedWeapons = _currentClass.ClassData.Requirements.AllowedWeaponTypes;
            var inventory = _inventoryInstance.Items();
            int minRange = int.MaxValue;
            bool hasWeapon = false;

            foreach (
                var weapon in inventory.Where(w =>
                    w.Template != null && allowedWeapons.Contains(w.Template.WeaponType)
                )
            )
            {
                hasWeapon = true;
                minRange = Mathf.Min(minRange, weapon.Template.LowerRange);
            }

            return hasWeapon ? minRange : 0;
        }

        /// <summary>
        /// Returns the maximum attack range across all equippable weapons in the character's inventory, or 0 if no valid weapons are available.
        /// </summary>
        public int GetMaxRange()
        {
            if (_currentClass == null || _currentClass.ClassData == null)
            {
                return settings.UnitCanAttackWithoutWeapons ? 1 : 0;
            }

            var allowedWeapons = _currentClass.ClassData.Requirements.AllowedWeaponTypes;
            var inventory = _inventoryInstance.Items();
            int maxRange = settings.UnitCanAttackWithoutWeapons ? 1 : 0;

            foreach (
                var weapon in inventory.Where(w =>
                    w.Template != null && allowedWeapons.Contains(w.Template.WeaponType)
                )
            )
            {
                TurnrootLogger.Log(
                    $"CharacterInstance: Considering weapon '{weapon.Template.name}' with max range {weapon.Template.UpperRange} for max range calculation."
                );
                maxRange = Mathf.Max(maxRange, weapon.Template.UpperRange);
            }
            TurnrootLogger.Log(
                $"CharacterInstance: Computed max range for character '{_characterTemplate.name}' is {maxRange}."
            );

            return maxRange;
        }

        #endregion

        #region Class Management

        public CharacterClassDataInstance GetCurrentClass() => _currentClass;

        /// <summary>
        /// Change to a new class. Applies all class bonuses, enforces minimums/caps.
        /// Removes bonuses from old class if present.
        /// </summary>
        public OperationResult ChangeClass(
            CharacterClassData newClassData,
            bool applyClassChangeBonuses = true
        )
        {
            if (newClassData == null)
            {
                return OperationResult.Failure("newClassData is null");
            }

            // Validate class requirements if needed
            // TODO: Add validation for experience requirements, level requirements, etc.

            // Remove old class bonuses
            if (_currentClass != null)
            {
                _currentClass.RemoveClassBonuses(this);
                _currentClass.Dispose();
            }

            // Check if this class has been equipped before (compare by reference, not name)
            bool isFirstTime = !_equippedClassHistory.Contains(newClassData);

            var effectiveRenderer = _meshRenderer;

            // Create new class instance, passing the isFirstTime flag and the character's renderer.
            _currentClass = new CharacterClassDataInstance(
                _characterTemplate,
                newClassData,
                effectiveRenderer,
                isFirstTime
            );

            // Initialize visual representation if we have an effective renderer
            if (effectiveRenderer != null)
            {
                _currentClass.InitializeWithRenderer(effectiveRenderer);
            }

            // Apply class bonuses
            _currentClass.ApplyClassBonuses(this);

            // Apply one-time class change bonuses if first time (optionally skipped)
            if (isFirstTime)
            {
                if (applyClassChangeBonuses)
                {
                    _currentClass.ApplyClassChangeBonuses(this);
                }
                // Mark as equipped so first-time bonuses aren't applied again later
                _equippedClassHistory.Add(newClassData);
            }

            // Enforce stat minimums and caps
            _currentClass.EnforceStatMinimums(this);
            _currentClass.ApplyStatCaps(this);

            return OperationResult.Successful();
        }

        /// <summary>
        /// Check if character meets requirements to change to a specific class.
        /// </summary>
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
            if (
                classData.Requirements.AllowedSpecies.Count > 0
                && !classData.Requirements.AllowedSpecies.Contains(_characterTemplate.Species)
            )
            {
                return false;
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

        /// <summary>
        /// Validate that the target class follows proper tier progression.
        /// </summary>
        private bool ValidateClassTierProgression(CharacterClassData targetClass)
        {
            // If no current class, any tier is allowed (starting class)
            if (_currentClass == null || _currentClass.ClassData == null)
            {
                return true;
            }

            var currentTier = _currentClass.ClassData.Identity.ClassTier;
            var targetTier = targetClass.Identity.ClassTier;

            // Can only advance one tier at a time (Base -> Advanced not allowed)
            // Tier regression is allowed (Advanced -> Intermediate is valid)
            if (targetTier > currentTier + 1)
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    $"Cannot change from {currentTier} class to {targetTier} class - must progress one tier at a time"
                );
#endif
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check if character's current class allows a specific weapon type.
        /// </summary>
        public bool CanEquipWeaponType(Gameplay.Objects.Components.WeaponType weaponType)
        {
            if (_currentClass == null || _currentClass.ClassData == null)
            {
                return true; // No class restrictions
            }

            var allowedTypes = _currentClass.ClassData.Requirements.AllowedWeaponTypes;

            // Empty list means no restrictions (can equip anything)
            return allowedTypes == null
                || allowedTypes.Count == 0
                || allowedTypes.Contains(weaponType);
        }

        /// <summary>
        /// Get available promotion paths based on current class.
        /// </summary>
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

        /// <summary>
        /// Check if character has previously equipped a specific class.
        /// </summary>
        public bool HasEquippedClass(CharacterClassData classData) =>
            classData != null && _equippedClassHistory.Contains(classData);

        #endregion
    }
}
