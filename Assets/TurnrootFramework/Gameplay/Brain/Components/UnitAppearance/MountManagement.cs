using Turnroot.Characters;
using Turnroot.GameSettings;
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
                TurnrootLogger.Log(
                    $"Class {classData.GetClassName()} is mounted but has no mount prefab configured",
                    TurnrootLogger.LogLevel.Warning
                );
                return OperationResult.Successful();
            }

            ClearMountFromUnit(unit);

            var mountPrefab = classData.Identity.MountPrefab;
            var mountInstance = Instantiate(mountPrefab, unitModel.transform.parent);
            if (mountInstance == null)
            {
                return OperationResult.Failure(
                    $"Failed to instantiate mount prefab for {unit.CharacterTemplate?.DisplayName}"
                );
            }

            mountInstance.name = $"{unit.CharacterTemplate?.DisplayName}_Mount_{unit.Id}";
            mountInstance.transform.SetPositionAndRotation(
                unitModel.transform.position,
                unitModel.transform.rotation
            );
            mountInstance.transform.localScale = unitModel.transform.localScale;

            // Set up animator if provided
            if (classData.Identity.MountAnimator != null)
            {
                var animator = mountInstance.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = mountInstance.AddComponent<Animator>();
                }
                animator.runtimeAnimatorController = classData.Identity.MountAnimator;
            }

            // Make unit a child of the mount with offset
            unitModel.transform.SetParent(mountInstance.transform, false);
            unitModel.transform.SetLocalPositionAndRotation(
                classData.Identity.MountOffset,
                Quaternion.identity
            );
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

            var mountModel = unit.CurrentMountModel;

            // Store mount's world position before dismounting
            var worldPosition = mountModel.transform.position;
            var worldRotation = mountModel.transform.rotation;
            var originalParent = mountModel.transform.parent;

            // Re-parent unit back to original parent
            unitModel.transform.SetParent(originalParent, false);
            unitModel.transform.position = worldPosition;
            unitModel.transform.rotation = worldRotation;

            ClearMountFromUnit(unit);

            return OperationResult.Successful();
        }

        public void ClearMountFromUnit(CharacterInstance unit)
        {
            if (unit == null)
            {
                return;
            }

            if (unit.CurrentMountModel != null)
            {
                unit.CurrentMountModel.SetActive(false);
                Destroy(unit.CurrentMountModel);
                unit.CurrentMountModel = null;
            }

            if (_mountModels.ContainsKey(unit.Id))
            {
                var mount = _mountModels[unit.Id];
                if (mount != null)
                {
                    mount.SetActive(false);
                    Destroy(mount);
                }
                _mountModels.Remove(unit.Id);
            }

            unit.IsMounted = false;
        }

        public bool ShouldUnitBeMounted(CharacterInstance unit)
        {
            if (unit == null)
            {
                return false;
            }

            var classData = unit.CurrentClassTemplate;
            return classData != null
                && classData.Identity != null
                && classData.Identity.IsMountedClass()
                && classData.Identity.HasMountVisuals();
        }
    }
}
