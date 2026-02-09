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
            var result = AttachEquipmentToUnit(
                equippedWeapon,
                model,
                unit.CharacterTemplate.HandItemOffset,
                "_Weapon",
                out var weaponInstance
            );
            unit.CurrentWeaponPrefab = weaponInstance;
            return result;
        }

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
            var result = AttachEquipmentToUnit(
                equippedShield,
                model,
                unit.CharacterTemplate.ShieldOffset,
                "_Shield",
                out var shieldInstance
            );
            unit.CurrentShieldPrefab = shieldInstance;
            return result;
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

        // ===== Helper Methods =====

        /// <summary>
        /// Common method to attach equipment (weapon or shield) to a unit model.
        /// </summary>
        private OperationResult AttachEquipmentToUnit(
            Objects.ObjectItemInstance equipment,
            GameObject model,
            Vector3 offset,
            string nameSuffix,
            out GameObject equipmentInstance
        )
        {
            equipmentInstance = null;

            if (equipment?.Template?.Prefab == null)
            {
                return OperationResult.Successful();
            }

            equipmentInstance = Instantiate(equipment.Template.Prefab, model.transform);
            if (equipmentInstance == null)
            {
                return OperationResult.Failure(
                    $"Failed to instantiate {equipment.Template.name} prefab"
                );
            }

            equipmentInstance.name = $"{equipment.Template.name}{nameSuffix}";
            equipmentInstance.transform.localPosition = offset;
            equipmentInstance.transform.localRotation = Quaternion.identity;
            equipmentInstance.transform.localScale = Vector3.one;

            return OperationResult.Successful();
        }
    }
}
