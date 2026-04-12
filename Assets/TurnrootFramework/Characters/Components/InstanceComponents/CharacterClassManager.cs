using System;
using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters.CharacterClass;
using Turnroot.Gameplay.Objects;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Characters
{
    /// <summary>
    /// Handles class management, class changes, and class-related requirements.
    /// </summary>
    public partial class CharacterInstance
    {
        [NonSerialized]
        public List<ObjectItemInstance> RangeWeaponsCache = new();

        [NonSerialized]
        public Dictionary<string, Material> classNameToOutfitMaterials = new();

        #region Battle Helpers

        public ObjectItemInstance GetEquippedWeapon()
        {
            if (_currentClass?.ClassData == null)
            {
                return null;
            }

            var allowedWeapons = _currentClass.ClassData.Requirements?.AllowedWeaponTypes;
            var inventory = _inventoryInstance.Items();

            return inventory.FirstOrDefault(w =>
                w?.Template != null
                && w.IsEquipped
                && (
                    allowedWeapons == null
                    || allowedWeapons.Count == 0
                    || allowedWeapons.Contains(w.Template.WeaponType)
                )
            );
        }

        public ObjectItemInstance GetEquippedShield()
        {
            return _inventoryInstance
                .Items()
                .FirstOrDefault(item =>
                    item?.Template != null
                    && item.IsEquipped
                    && item.Template.Subtype == Gameplay.Objects.Components.ObjectSubtype.Shield
                );
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
            int maxRange = (settings?.UnitCanAttackWithoutWeapons ?? false) ? 1 : 0;
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

                    $"GetMaxRange: Something has gone terribly wrong for {_characterTemplate.DisplayName}, unitId={Id}".LogError();
                }
                // If it's not null but it IS empty, that's fine, they just don't have a weapon equipped
            }
            return maxRange;
        }

        #endregion

        #region Class Management

        public CharacterClassDataInstance GetCurrentClass() => _currentClass;

        #region Change
        public OperationResult ChangeClass(
            CharacterClassData newClassData,
            bool applyClassChangeBonuses = true
        )
        {
            var validation = OperationResultGuards.RequireNotNull(
                newClassData,
                nameof(newClassData)
            );
            if (!validation.Success)
            {
                return validation;
            }

            // Validate class change policy (promotion vs requirement-based rules)
            var selectionValidation = ValidateClassChangeSelectionMode(newClassData);
            if (!selectionValidation.Success)
            {
                return selectionValidation;
            }

            // Remove old class (if present) and clear bonuses
            if (_currentClass != null)
            {
                if (_currentClass.ClassData != null)
                {
                    _currentClass.RemoveClassBonuses(this);
                }
                else if (!ClassRecoveryHandled)
                {
                    $"CharacterInstance.ChangeClass: Previous class instance for {(_characterTemplate?.name ?? Id)} has missing classData; skipping RemoveClassBonuses".LogWarning();
                }

                _currentClass.Dispose();
                _currentClass = null;
            }

            bool isFirstTime = !_equippedClassHistory.Contains(newClassData);
            var effectiveRenderer = _meshRenderer;

            _currentClass = new CharacterClassDataInstance(
                this,
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

            GetAvailableWeapons();

            // Persist stat changes that resulted from bonuses / minimums / caps.
            // Guard with applyClassChangeBonuses so we skip when ChangeClass is called
            // during initialization / deserialization (those paths pass false) and avoid
            // overwriting already-saved stats with fresh template values.
            if (applyClassChangeBonuses)
            {
                PersistStatsToLtm();
                NeedsPersist = true;
            }

            return OperationResult.Successful();
        }

        private OperationResult ValidateClassChangeSelectionMode(CharacterClassData newClassData)
        {
            var selectionMode =
                GameplayGeneralSettings.Instance?.GetClassSelectionMode()
                ?? GameplayGeneralSettings.ClassSelectionMode.PromotionBased;

            if (selectionMode == GameplayGeneralSettings.ClassSelectionMode.PromotionBased)
            {
                // In promotion-based mode only allow classes reachable by promotion paths from the
                // current class (when a current class exists). Starting/initial assignment is still allowed.
                if (_currentClass != null && _currentClass.ClassData != null)
                {
                    var paths = _currentClass.ClassData.Requirements?.PromotionPaths;
                    if (paths == null || !paths.Contains(newClassData))
                    {
                        return OperationResult.Failure(
                            "Target class is not a valid promotion for the current class (project is PromotionBased)."
                        );
                    }
                }

                return OperationResult.Successful();
            }

            // Requirement-based selection: use probabilistic exam
            var chance = CalculateRequirementPassChance(newClassData);

            return chance
                <= GameplayGeneralSettings.Instance.MinimumPercentChanceToAttemptClassChange
                    ? OperationResult.Failure(
                        "Character does not meet requirements and may not attempt the class exam."
                    )
                : UnityEngine.Random.value > chance
                    ? OperationResult.Failure($"Class exam failed ({chance * 100f:0}%).")
                : OperationResult.Successful();
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

        #endregion

        #endregion
    }
}
