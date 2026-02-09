using Turnroot.Characters;
using Turnroot.Characters.CharacterClass;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Handles mount spawning, attachment, and management for mounted unit classes.
    /// </summary>
    public partial class UnitAppearanceBrain
    {
        public OperationResult AttachMountToUnit(CharacterInstance unit, GameObject unitModel)
        {
            var validation = OperationResultGuards.All(
                OperationResultGuards.RequireNotNull(unit, nameof(unit)),
                OperationResultGuards.RequireNotNull(unitModel, nameof(unitModel))
            );
            if (!validation.Success)
            {
                return validation;
            }

            var classData = unit.CurrentClassTemplate;
            if (classData == null || classData.Identity == null)
            {
                return OperationResult.Successful();
            }

            if (!classData.Identity.IsMountedClass())
            {
                return OperationResult.Successful();
            }

            if (!classData.Identity.HasMountVisuals())
            {
                LogWarning(
                    $"Class {classData.GetClassName()} is mounted but has no mount prefab configured"
                );
                return OperationResult.Successful();
            }

            ClearMountFromUnit(unit);

            var mountInstance = InstantiateMount(unit, classData.Identity.MountPrefab, unitModel);
            if (mountInstance == null)
            {
                return OperationResult.Failure(
                    $"Failed to instantiate mount prefab for {unit.CharacterTemplate?.DisplayName}"
                );
            }

            SetupMountAnimator(mountInstance, classData, unit);
            SetupWalkAnimation(mountInstance, unit);
            AttachUnitToMount(unitModel, mountInstance, classData.Identity.MountOffset);

            _mountModels[unit.Id] = mountInstance;
            unit.CurrentMountModel = mountInstance;
            unit.IsMounted = true;

            return OperationResult.Successful();
        }

        public OperationResult DismountUnit(CharacterInstance unit, GameObject unitModel)
        {
            var validation = OperationResultGuards.All(
                OperationResultGuards.RequireNotNull(unit, nameof(unit)),
                OperationResultGuards.RequireNotNull(unitModel, nameof(unitModel))
            );
            if (!validation.Success)
            {
                return validation;
            }

            if (!unit.IsMounted || unit.CurrentMountModel == null)
            {
                return OperationResult.Successful();
            }

            if (
                !_unitModels.TryGetValue(unit.Id, out var actualUnitModel)
                || actualUnitModel == null
            )
            {
                return OperationResult.Failure(
                    $"Cannot dismount {unit.CharacterTemplate?.DisplayName}: unit model not found in registry"
                );
            }

            var mountModel = unit.CurrentMountModel;
            RestoreUnitFromMount(actualUnitModel, mountModel);
            ClearMountFromUnit(unit);

            return OperationResult.Successful();
        }

        public void ClearMountFromUnit(CharacterInstance unit)
        {
            if (unit == null)
            {
                return;
            }

            GameObject mountToDestroy = null;
            if (!_mountModels.TryGetValue(unit.Id, out mountToDestroy))
            {
                mountToDestroy = unit.CurrentMountModel;
            }

            if (mountToDestroy != null)
            {
                DetachUnitFromMount(unit.Id, mountToDestroy);
                mountToDestroy.SetActive(false);
                Destroy(mountToDestroy);
            }

            _mountModels.Remove(unit.Id);
            unit.CurrentMountModel = null;
            unit.IsMounted = false;
        }

        public bool ShouldUnitBeMounted(CharacterInstance unit)
        {
            return unit != null
                && unit.CurrentClassTemplate?.Identity != null
                && unit.CurrentClassTemplate.Identity.IsMountedClass()
                && unit.CurrentClassTemplate.Identity.HasMountVisuals();
        }

        // ===== Helper Methods =====

        private GameObject InstantiateMount(
            CharacterInstance unit,
            GameObject mountPrefab,
            GameObject unitModel
        )
        {
            var mountInstance = Instantiate(mountPrefab, unitModel.transform.parent);
            mountInstance.name = $"{unit.CharacterTemplate?.DisplayName}_Mount_{unit.Id}";
            mountInstance.transform.SetPositionAndRotation(
                unitModel.transform.position,
                unitModel.transform.rotation
            );
            mountInstance.transform.localScale = unitModel.transform.localScale;
            return mountInstance;
        }

        private void SetupMountAnimator(
            GameObject mountInstance,
            CharacterClassData classData,
            CharacterInstance unit
        )
        {
            var animator =
                mountInstance.GetComponent<Animator>() ?? mountInstance.AddComponent<Animator>();
            var controller =
                classData.Identity.MountAnimator ?? _settings?.DefaultUnitAnimatorController;

            if (controller != null)
            {
                animator.runtimeAnimatorController = controller;
            }
            else
            {
                LogWarning(
                    $"No animator controller available for mount of {unit.CharacterTemplate?.DisplayName}. "
                        + "Set MountAnimator on class or DefaultUnitAnimatorController in settings."
                );
            }
        }

        private void AttachUnitToMount(
            GameObject unitModel,
            GameObject mountInstance,
            Vector3 mountOffset
        )
        {
            unitModel.transform.SetParent(mountInstance.transform, false);
            unitModel.transform.SetLocalPositionAndRotation(mountOffset, Quaternion.identity);
        }

        private void RestoreUnitFromMount(GameObject unitModel, GameObject mountModel)
        {
            var worldPosition = mountModel.transform.position;
            var worldRotation = mountModel.transform.rotation;
            var originalParent = mountModel.transform.parent;

            unitModel.transform.SetParent(originalParent, false);
            unitModel.transform.position = worldPosition;
            unitModel.transform.rotation = worldRotation;
        }

        private void DetachUnitFromMount(string unitId, GameObject mountToDestroy)
        {
            if (_unitModels.TryGetValue(unitId, out var unitModel) && unitModel != null)
            {
                unitModel.transform.SetParent(mountToDestroy.transform.parent, true);
            }
        }
    }
}
