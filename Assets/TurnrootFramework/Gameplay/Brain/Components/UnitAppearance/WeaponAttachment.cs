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
        /// <summary>
        /// Spawns and attaches the equipped weapon to the unit model with offset.
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

            var weaponInstance = TryInstantiatePrefab(weaponPrefab, model.transform, $"{equippedWeapon.Template.name}_Weapon", "AttachWeaponToUnit");
            if (weaponInstance == null)
            {
                return OperationResult.Failure($"Failed to instantiate weapon prefab for {unit.CharacterTemplate?.DisplayName}");
            }

            // Apply offset - since all models use the same rig structure,
            // the weapon keeps its own skeleton and just needs positioning
            weaponInstance.transform.localPosition = unit.CharacterTemplate.HandItemOffset;
            weaponInstance.transform.localRotation = Quaternion.identity;
            weaponInstance.transform.localScale = Vector3.one;

            unit.CurrentWeaponPrefab = weaponInstance;

            return OperationResult.Successful();
        }

        /// <summary>
        /// Spawns and attaches the equipped shield to the unit model with offset.
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

            var shieldInstance = TryInstantiatePrefab(shieldPrefab, model.transform, $"{equippedShield.Template.name}_Shield", "AttachShieldToUnit");
            if (shieldInstance == null)
            {
                return OperationResult.Failure($"Failed to instantiate shield prefab for {unit.CharacterTemplate?.DisplayName}");
            }

            // Apply offset - since all models use the same rig structure,
            // the shield keeps its own skeleton and just needs positioning
            shieldInstance.transform.localPosition = unit.CharacterTemplate.ShieldOffset;
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
            return !validation.Success ? validation
                : !_unitModels.TryGetValue(unit.Id, out var model) ? OperationResult.Successful()
                : AttachWeaponToUnit(unit, model);
        }

        public OperationResult UpdateUnitShield(CharacterInstance unit)
        {
            var validation = OperationResultGuards.RequireNotNull(unit, nameof(unit));
            return !validation.Success ? validation
                : !_unitModels.TryGetValue(unit.Id, out var model) ? OperationResult.Successful()
                : AttachShieldToUnit(unit, model);
        }
    }
}
