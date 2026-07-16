using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Character
{
    public partial class HubCharacterManager
    {
        public void HandleTraversalEntered(Transform spawnPoint, HubSublocationName locationName)
        {
            var validation = ValidateTraversalEnterRequest(spawnPoint);
            if (!validation.Success)
            {
                $"HubCharacterManager: Traversal enter rejected. {validation.ErrorMessage}".LogError();
                return;
            }

            _activeAvatarPoint = spawnPoint;
            SpawnAvatarModel();
            _activeAvatarPoint = null;
        }

        private OperationResult ValidateTraversalEnterRequest(Transform spawnPoint)
        {
            var spawnValidation = OperationResultGuards.RequireNotNull(
                spawnPoint,
                nameof(spawnPoint)
            );
            return !spawnValidation.Success ? spawnValidation
                : _activeCharacter != null
                    ? OperationResult.Failure(
                        "Cannot enter traversal while a character interaction is active."
                    )
                : OperationResult.Successful();
        }

        public void HandleHubOverviewEntered()
        {
            DestroyCurrentAvatarModel();

            if (_turnCoroutine != null)
            {
                StopCoroutine(_turnCoroutine);
                _turnCoroutine = null;
            }

            CharacterInteraction?.HideActionsMenu();
            _activeCharacter = null;
            _activeAvatarPoint = null;
        }

        private void SpawnAvatarModel() => RebuildAvatarModelAtPoint(_activeAvatarPoint);

        private void RebuildAvatarModelAtPoint(Transform avatarPoint)
        {
            var validation = OperationResultGuards.All(
                OperationResultGuards.RequireNotNull(avatarPoint, nameof(avatarPoint)),
                OperationResultGuards.RequireNotNull(_brain, nameof(_brain))
            );
            if (!validation.Success)
            {
                $"HubCharacterManager: Avatar rebuild skipped. {validation.ErrorMessage}".LogError();
                return;
            }

            DestroyCurrentAvatarModel();

            var avatarInstance = _brain.gamewideContextBrain?.GetOrCreateAvatarInstance();
            if (avatarInstance == null)
            {
                $"HubCharacterManager '{name}': Could not find Avatar character instance in persistent roster.".LogWarning();
                return;
            }

            var model = _brain.unitAppearanceBrain?.CreateModelForUnit(avatarInstance);
            if (model == null)
            {
                $"HubCharacterManager '{name}': Failed to create avatar model for {avatarInstance.CharacterTemplate?.DisplayName}.".LogWarning();
                return;
            }

            model.transform.SetPositionAndRotation(avatarPoint.position, avatarPoint.rotation);
            model.transform.SetParent(avatarPoint, worldPositionStays: true);

            _brain.unitAppearanceBrain.SetupHubIdleAnimation(model, avatarInstance);
            BindThirdPersonAdapterToAvatar(model);
            _avatarModel = model;
        }

        private void DestroyCurrentAvatarModel()
        {
            if (_avatarModel == null)
            {
                return;
            }

            ClearThirdPersonAdapterAvatarIfMatches(_avatarModel);
            Destroy(_avatarModel);
            _avatarModel = null;
        }

        private void EnsureHubTraversalAvatarSpawned()
        {
            if (_activeCharacter != null || _avatarModel != null || _brain == null)
            {
                return;
            }

            Transform spawnPoint = ResolveCurrentTraversalPoint();
            if (spawnPoint == null)
            {
                if (!SpawnAvatarOnTraversalStart)
                {
                    return;
                }

                spawnPoint = ResolveTraversalStartPoint();
            }

            if (spawnPoint == null)
            {
                "HubCharacterManager: Could not find traversal spawn point. Assign a teleport point or HubManager.TraversalStartAvatarPoint.".LogWarning();
                return;
            }

            _activeAvatarPoint = spawnPoint;
            SpawnAvatarModel();
            _activeAvatarPoint = null;
        }

        private Transform ResolveTraversalStartPoint() =>
            GetHubManager()?.TraversalStartAvatarPoint;

        private Transform ResolveCurrentTraversalPoint() =>
            GetHubManager()?.CurrentTraversalAvatarPoint;

        private void BindThirdPersonAdapterToAvatar(GameObject model)
        {
            var validation = OperationResultGuards.RequireNotNull(model, nameof(model));
            if (!validation.Success)
            {
                $"HubCharacterManager: Adapter bind failed. {validation.ErrorMessage}".LogError();
                return;
            }

            _hubManager.BindAvatar(model);
        }

        private void ClearThirdPersonAdapterAvatarIfMatches(GameObject model)
        {
            if (model == null)
            {
                return;
            }

            _hubManager.ClearAvatarBindingIfMatches(model);
        }

        private HubManager GetHubManager()
        {
            _hubManager ??= HubManager.GetCurrent();
            return _hubManager;
        }
    }
}
