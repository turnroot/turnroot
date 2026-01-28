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
                // TODO: Figure out why this isn't seeing weapons in inventory
            }

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

            if (_currentClass != null)
            {
                if (_currentClass.ClassData != null)
                {
                    _currentClass.RemoveClassBonuses(this);
                }
                else
                {
                    // If recovery/assignment was already handled elsewhere, suppress the noisy warning.
                    if (!ClassRecoveryHandled)
                    {
                        TurnrootLogger.Log(
                            $"CharacterInstance.ChangeClass: Previous class instance for {(_characterTemplate?.name ?? Id)} has missing classData; skipping RemoveClassBonuses",
                            TurnrootLogger.LogLevel.Warning
                        );
                    }
                }

                _currentClass.Dispose();
            }

            bool isFirstTime = !_equippedClassHistory.Contains(newClassData);

            var effectiveRenderer = _meshRenderer;

            _currentClass = new CharacterClassDataInstance(
                _characterTemplate,
                newClassData,
                effectiveRenderer,
                isFirstTime
            );

            if (effectiveRenderer != null)
            {
                _currentClass.InitializeWithRenderer(effectiveRenderer);
            }

            _currentClass.ApplyClassBonuses(this);

            if (isFirstTime)
            {
                if (applyClassChangeBonuses)
                {
                    _currentClass.ApplyClassChangeBonuses(this);
                }
                _equippedClassHistory.Add(newClassData);
            }

            _currentClass.EnforceStatMinimums(this);
            _currentClass.ApplyStatCaps(this);

            return OperationResult.Successful();
        }

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
                TurnrootLogger.Log(
                    $"Cannot change from {currentTier} class to {targetTier} class - must progress one tier at a time",
                    TurnrootLogger.LogLevel.Warning
                );

                return false;
            }

            return true;
        }

        public bool CanEquipWeaponType(Gameplay.Objects.Components.WeaponType weaponType)
        {
            if (_currentClass == null || _currentClass.ClassData == null)
            {
                return true;
            }

            var allowedTypes = _currentClass.ClassData.Requirements.AllowedWeaponTypes;

            return allowedTypes == null
                || allowedTypes.Count == 0
                || allowedTypes.Contains(weaponType);
        }

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
    }
}
