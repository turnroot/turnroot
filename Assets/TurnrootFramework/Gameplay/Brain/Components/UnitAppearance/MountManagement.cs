using Turnroot.Characters;
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
                var msg =
                    "Class "
                    + classData.GetClassName()
                    + " is mounted but has no mount prefab configured";
                LogWarning(msg);
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

            // Set up animator - use MountAnimator if provided, otherwise use default
            var animator = mountInstance.GetComponent<Animator>() ?? mountInstance.AddComponent<Animator>();

            // Assign animator controller - prefer mount-specific, fall back to default
            var controllerToUse = classData.Identity.MountAnimator ?? _settings?.DefaultUnitAnimatorController;

            if (controllerToUse != null)
            {
                animator.runtimeAnimatorController = controllerToUse;
            }
            else
            {
                var displayName = unit.CharacterTemplate?.DisplayName ?? "<unknown>";
                LogWarning($"No animator controller available for mount of {displayName}. Set MountAnimator on class or DefaultUnitAnimatorController in settings.");
            }

            // Set up walk animation for the mount
            SetupWalkAnimation(mountInstance, unit);

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

            // Look up the actual unit model from the dictionary to ensure we have the correct reference
            if (
                !_unitModels.TryGetValue(unit.Id, out GameObject actualUnitModel)
                || actualUnitModel == null
            )
            {
                return OperationResult.Failure(
                    $"Cannot dismount {unit.CharacterTemplate?.DisplayName}: unit model not found in registry"
                );
            }

            // Store mount's world position before dismounting
            var worldPosition = mountModel.transform.position;
            var worldRotation = mountModel.transform.rotation;
            var originalParent = mountModel.transform.parent;

            // Re-parent unit back to original parent
            actualUnitModel.transform.SetParent(originalParent, false);
            actualUnitModel.transform.position = worldPosition;
            actualUnitModel.transform.rotation = worldRotation;

            ClearMountFromUnit(unit);

            return OperationResult.Successful();
        }

        public void ClearMountFromUnit(CharacterInstance unit)
        {
            if (unit == null)
            {
                return;
            }

            // Get the mount instance from dictionary (single source of truth)
            GameObject mountToDestroy = null;
            if (_mountModels.ContainsKey(unit.Id))
            {
                mountToDestroy = _mountModels[unit.Id];
            }
            else if (unit.CurrentMountModel != null)
            {
                // Fallback if dictionary is out of sync
                mountToDestroy = unit.CurrentMountModel;
            }

            // If there's a mount to destroy, detach the unit model first
            if (mountToDestroy != null)
            {
                // Re-parent the unit model back to the mount's parent before destroying the mount
                if (_unitModels.TryGetValue(unit.Id, out GameObject unitModel) && unitModel != null)
                {
                    // Preserve world transform
                    var mountParent = mountToDestroy.transform.parent;
                    unitModel.transform.SetParent(mountParent, true);
                }

                // Now safely destroy the mount
                mountToDestroy.SetActive(false);
                Destroy(mountToDestroy);
            }

            // Clean up references (only destroy once via mountToDestroy above)
            if (_mountModels.ContainsKey(unit.Id))
            {
                _mountModels.Remove(unit.Id);
            }

            unit.CurrentMountModel = null;
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
