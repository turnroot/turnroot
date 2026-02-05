using Turnroot.Characters;
using Turnroot.Gameplay.Objects;
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

            var weaponInstance = Instantiate(weaponPrefab, model.transform);
            if (weaponInstance == null)
            {
                return OperationResult.Failure(
                    $"Failed to instantiate weapon prefab for {unit.CharacterTemplate?.DisplayName}"
                );
            }

            weaponInstance.name = $"{equippedWeapon.Template.name}_Weapon";

            // Apply hand item offset
            weaponInstance.transform.localPosition = unit.CharacterTemplate.HandItemOffset;
            weaponInstance.transform.localRotation = Quaternion.identity;
            weaponInstance.transform.localScale = Vector3.one;

            RebindItemToUnitSkeleton(weaponInstance, model);

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

            var shieldInstance = Instantiate(shieldPrefab, model.transform);
            if (shieldInstance == null)
            {
                return OperationResult.Failure(
                    $"Failed to instantiate shield prefab for {unit.CharacterTemplate?.DisplayName}"
                );
            }

            shieldInstance.name = $"{equippedShield.Template.name}_Shield";

            // Apply shield offset
            shieldInstance.transform.localPosition = unit.CharacterTemplate.ShieldOffset;
            shieldInstance.transform.localRotation = Quaternion.identity;
            shieldInstance.transform.localScale = Vector3.one;

            RebindItemToUnitSkeleton(shieldInstance, model);

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

            if (!_unitModels.TryGetValue(unit.Id, out var model))
            {
                return OperationResult.Successful();
            }

            return AttachWeaponToUnit(unit, model);
        }

        public OperationResult UpdateUnitShield(CharacterInstance unit)
        {
            var validation = OperationResultGuards.RequireNotNull(unit, nameof(unit));
            if (!validation.Success)
            {
                return validation;
            }

            if (!_unitModels.TryGetValue(unit.Id, out var model))
            {
                return OperationResult.Successful();
            }

            return AttachShieldToUnit(unit, model);
        }

        private void RebindItemToUnitSkeleton(GameObject itemInstance, GameObject unitModel)
        {
            var unitRoot = FindCanonicalBoneRoot(unitModel.transform);
            if (unitRoot == null)
            {
                return;
            }

            var boneMap = new System.Collections.Generic.Dictionary<string, Transform>();
            BuildBoneMap(unitRoot, boneMap);

            var itemRenderers = itemInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var renderer in itemRenderers)
            {
                if (renderer.bones == null || renderer.bones.Length == 0)
                {
                    continue;
                }

                var newBones = new Transform[renderer.bones.Length];
                bool success = true;

                for (int i = 0; i < renderer.bones.Length; i++)
                {
                    if (
                        renderer.bones[i] != null
                        && boneMap.TryGetValue(renderer.bones[i].name, out var unitBone)
                    )
                    {
                        newBones[i] = unitBone;
                    }
                    else
                    {
                        success = false;
                        break;
                    }
                }

                if (success)
                {
                    renderer.bones = newBones;
                    renderer.rootBone = unitRoot;
                }
            }
        }

        private Transform FindCanonicalBoneRoot(Transform parent)
        {
            foreach (Transform child in parent)
            {
                var childName = child.name.ToLower();
                if (childName == "root" || childName == "armature" || childName.StartsWith("root."))
                {
                    return child;
                }

                if (!child.GetComponent<SkinnedMeshRenderer>())
                {
                    var result = FindCanonicalBoneRoot(child);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            return null;
        }
    }
}
