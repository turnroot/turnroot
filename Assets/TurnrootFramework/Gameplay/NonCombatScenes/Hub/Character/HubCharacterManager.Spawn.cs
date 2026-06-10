using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Character
{
    public partial class HubCharacterManager
    {
        public void HandleTraversalEntered(Transform spawnPoint, HubSublocationName locationName)
        {
            if (spawnPoint == null)
            {
                return;
            }

            if (_activeCharacter != null)
            {
                return;
            }

            _activeAvatarPoint = spawnPoint;
            SpawnAvatarModel();
            _activeAvatarPoint = null;
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

        private void SpawnAvatarModel()
        {
            RebuildAvatarModelAtPoint(_activeAvatarPoint);
        }

        private void RebuildAvatarModelAtPoint(Transform avatarPoint)
        {
            if (avatarPoint == null || _brain == null)
            {
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

        private Transform ResolveTraversalStartPoint()
        {
            return GetHubManager()?.TraversalStartAvatarPoint;
        }

        private Transform ResolveCurrentTraversalPoint()
        {
            return GetHubManager()?.CurrentTraversalAvatarPoint;
        }

        private void BindThirdPersonAdapterToAvatar(GameObject model)
        {
            if (model == null)
            {
                return;
            }

            var adapter = GetThirdPersonAdapter();
            if (adapter == null)
            {
                return;
            }

            adapter.BindAvatar(model);
        }

        private void ClearThirdPersonAdapterAvatarIfMatches(GameObject model)
        {
            if (model == null)
            {
                return;
            }

            var adapter = GetThirdPersonAdapter();
            if (adapter == null)
            {
                return;
            }

            adapter.ClearAvatarBindingIfMatches(model);
        }

        private HubManager GetHubManager()
        {
            _hubManager ??= FindFirstObjectByType<HubManager>();
            return _hubManager;
        }

        private HubThirdPersonAdapter GetThirdPersonAdapter() =>
            GetHubManager()?.SublocationInput?.ThirdPersonAdapter;
    }
}
