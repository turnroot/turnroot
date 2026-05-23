using Turnroot.Characters;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Handles weapon and shield attachment and updates for unit models.
    /// </summary>
    public partial class UnitAppearanceBrain
    {
        private const string WeaponBoneName = "hand.r";
        private const string ShieldBoneName = "forearm.r";

        /// <summary>
        /// Finds a named bone Transform anywhere in the model hierarchy.
        /// </summary>
        private static Transform FindBone(GameObject model, string boneName)
        {
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == boneName)
                {
                    return t;
                }
            }
            return null;
        }

        /// <summary>
        /// Spawns and attaches the equipped weapon as a child of the right hand bone (hand.r).
        /// All local transforms are zeroed so the weapon follows the bone directly.
        /// </summary>
        public OperationResult AttachWeaponToUnit(CharacterInstance unit, GameObject model)
        {
            var validation = OperationResultGuards.All(
                OperationResultGuards.RequireNotNull(unit, nameof(unit)),
                OperationResultGuards.RequireNotNull(model, nameof(model))
            );
            if (!validation.Success)
            {
                return validation;
            }

            ClearWeaponFromUnit(unit);

            var equippedWeapon = unit.GetEquippedWeapon();
            if (equippedWeapon == null || equippedWeapon.Template == null)
            {
                return OperationResult.Successful();
            }

            var weaponPrefab = equippedWeapon.Template.Prefab;
            if (weaponPrefab == null)
            {
                return OperationResult.Successful();
            }

            var handBone = FindBone(model, WeaponBoneName);
            var parent = handBone != null ? handBone : model.transform;
            if (handBone == null)
            {
                LogWarning(
                    $"AttachWeaponToUnit: '{WeaponBoneName}' bone not found on {unit.CharacterTemplate?.DisplayName}; attaching to model root."
                );
            }

            var weaponInstance = TryInstantiatePrefab(
                weaponPrefab,
                parent,
                $"{equippedWeapon.Template.name}_Weapon",
                "AttachWeaponToUnit"
            );
            if (weaponInstance == null)
            {
                return OperationResult.Failure(
                    $"Failed to instantiate weapon prefab for {unit.CharacterTemplate?.DisplayName}"
                );
            }

            weaponInstance.transform.localPosition = Vector3.zero;
            weaponInstance.transform.localRotation = Quaternion.identity;
            weaponInstance.transform.localScale = Vector3.one;

            unit.CurrentWeaponPrefab = weaponInstance;

            return OperationResult.Successful();
        }

        /// <summary>
        /// Spawns and attaches the equipped shield as a child of the right forearm bone (forearm.r).
        /// All local transforms are zeroed so the shield follows the bone directly.
        /// </summary>
        public OperationResult AttachShieldToUnit(CharacterInstance unit, GameObject model)
        {
            var validation = OperationResultGuards.All(
                OperationResultGuards.RequireNotNull(unit, nameof(unit)),
                OperationResultGuards.RequireNotNull(model, nameof(model))
            );
            if (!validation.Success)
            {
                return validation;
            }

            ClearShieldFromUnit(unit);

            var equippedShield = unit.GetEquippedShield();
            if (equippedShield == null || equippedShield.Template == null)
            {
                return OperationResult.Successful();
            }

            var shieldPrefab = equippedShield.Template.Prefab;
            if (shieldPrefab == null)
            {
                return OperationResult.Successful();
            }

            var forearmBone = FindBone(model, ShieldBoneName);
            var parent = forearmBone != null ? forearmBone : model.transform;
            if (forearmBone == null)
            {
                LogWarning(
                    $"AttachShieldToUnit: '{ShieldBoneName}' bone not found on {unit.CharacterTemplate?.DisplayName}; attaching to model root."
                );
            }

            var shieldInstance = TryInstantiatePrefab(
                shieldPrefab,
                parent,
                $"{equippedShield.Template.name}_Shield",
                "AttachShieldToUnit"
            );
            if (shieldInstance == null)
            {
                return OperationResult.Failure(
                    $"Failed to instantiate shield prefab for {unit.CharacterTemplate?.DisplayName}"
                );
            }

            shieldInstance.transform.localPosition = Vector3.zero;
            shieldInstance.transform.localRotation = Quaternion.identity;
            shieldInstance.transform.localScale = Vector3.one;

            unit.CurrentShieldPrefab = shieldInstance;

            return OperationResult.Successful();
        }

        public void ClearWeaponFromUnit(CharacterInstance unit)
        {
            if (unit?.CurrentWeaponPrefab != null)
            {
                Destroy(unit.CurrentWeaponPrefab);
                unit.CurrentWeaponPrefab = null;
            }
        }

        public void ClearShieldFromUnit(CharacterInstance unit)
        {
            if (unit?.CurrentShieldPrefab != null)
            {
                Destroy(unit.CurrentShieldPrefab);
                unit.CurrentShieldPrefab = null;
            }
        }

        public OperationResult UpdateUnitWeapon(CharacterInstance unit)
        {
            var validation = OperationResultGuards.RequireNotNull(unit, nameof(unit));
            if (!validation.Success)
            {
                return validation;
            }

            var model = GetModelForUnit(unit.Id);
            return model == null ? OperationResult.Successful() : AttachWeaponToUnit(unit, model);
        }

        public OperationResult UpdateUnitShield(CharacterInstance unit)
        {
            var validation = OperationResultGuards.RequireNotNull(unit, nameof(unit));
            if (!validation.Success)
            {
                return validation;
            }

            var model = GetModelForUnit(unit.Id);
            return model == null ? OperationResult.Successful() : AttachShieldToUnit(unit, model);
        }
    }
}
