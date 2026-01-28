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
        public List<ObjectItemInstance> RangeWeaponsCache = new();
        public Dictionary<string, Material> classNameToOutfitMaterials = new();

        #region Battle Helpers

        public ObjectItemInstance GetEquippedWeapon()
        {
            var allowedWeapons = _currentClass.ClassData.Requirements?.AllowedWeaponTypes;

            var inventory = _inventoryInstance.Items();

            // return the weapon in slot 0 (allow all when allowedWeapons is null/empty)
            foreach (
                var weapon in inventory.Where(w =>
                    w.Template != null
                    && (
                        allowedWeapons == null
                        || allowedWeapons.Count == 0
                        || allowedWeapons.Contains(w.Template.WeaponType)
                    )
                    && w.Slot == 0
                )
            )
            {
                return weapon;
            }
            return null;
        }

        public void GetAvailableWeapons()
        {
            // Avoid scanning weapons if class or inventory isn't set up yet
            if (_currentClass == null || _currentClass.ClassData == null)
            {
                RangeWeaponsCache = new List<ObjectItemInstance>();
                return;
            }

            var allowedWeapons = _currentClass.ClassData.Requirements?.AllowedWeaponTypes; // null/empty = allow all
            var inventory = _inventoryInstance?.Items() ?? Enumerable.Empty<ObjectItemInstance>();
            bool hasWeapon = false;
            var weapons = new List<ObjectItemInstance>();

            foreach (var weapon in inventory.Where(w => w?.Template != null))
            {
                // Unequippable items are exempt from class weapon restrictions and always considered available.
                if (weapon.Template.IsUnequippable)
                {
                    weapons.Add(weapon);
                    hasWeapon = true;
                    continue;
                }

                if (
                    allowedWeapons != null
                    && allowedWeapons.Count > 0
                    && !allowedWeapons.Contains(weapon.Template.WeaponType)
                )
                {
                    continue;
                }

                weapons.Add(weapon);
                hasWeapon = true;
            }

            RangeWeaponsCache = hasWeapon ? weapons : new List<ObjectItemInstance>();
        }

        public int GetMinRange()
        {
            if (_currentClass == null || _currentClass.ClassData == null)
            {
                return 0;
            }

            int minRange = int.MaxValue;
            bool hasWeapon = false;
            if (RangeWeaponsCache != null && RangeWeaponsCache.Count > 0)
            {
                foreach (var weapon in RangeWeaponsCache)
                {
                    hasWeapon = true;
                    minRange = Mathf.Min(minRange, weapon.Template.LowerRange);
                }
            }

            return hasWeapon ? minRange : 0;
        }

        public int GetMaxRange()
        {
            int maxRange = settings.UnitCanAttackWithoutWeapons ? 1 : 0;
            if (RangeWeaponsCache != null && RangeWeaponsCache.Count > 0)
            {
                foreach (var weapon in RangeWeaponsCache)
                {
                    maxRange = Mathf.Max(maxRange, weapon.Template.UpperRange);
                }
            }
            else
            {
                if (RangeWeaponsCache == null)
                {
                    // if it IS null, something is incredibly broken!!!!!!
                    TurnrootLogger.Log(
                        $"GetMaxRange: Something has gone terribly wrong for {_characterTemplate.DisplayName}, unitId={Id}",
                        TurnrootLogger.LogLevel.Error
                    );
                }
                // If it's not null but it IS empty, that's fine, they just don't have a weapon equipped
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

            // Refresh available weapons now that allowed types may have changed
            GetAvailableWeapons();

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
