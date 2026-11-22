using System;
using System.Collections;
using NaughtyAttributes;
using Turnroot.NonCombatScenes.Abstract;
using Turnroot.UI;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class HubManager : MonoBehaviour
    {
        #region Travel Effects and Validation

        private void PlayFastTravelSfx(AudioClip clip)
        {
            if (FastTravelAudioSource == null || clip == null)
            {
                return;
            }

            FastTravelAudioSource.PlayOneShot(clip);
        }

        private ParticleSystem SpawnAndPlayFastTravelFx(ParticleSystem prefab)
        {
            if (prefab == null)
            {
                return null;
            }

            Transform anchor = _avatarRoot != null ? _avatarRoot : MovementRig;
            if (anchor == null)
            {
                return null;
            }

            // particles up and down
            Quaternion rotationStraightUp = Quaternion.Euler(90f, 0f, 0f);

            var fx = Instantiate(prefab, anchor.position, rotationStraightUp, anchor);
            fx.Play();

            float cleanupDelay = GetFxCleanupDelay(fx);
            Destroy(fx.gameObject, cleanupDelay);

            return fx;
        }

        private static float GetFxCleanupDelay(ParticleSystem fx)
        {
            if (fx == null)
            {
                return DefaultFxCleanupDelaySeconds;
            }

            var main = fx.main;
            float lifetime = main.duration + main.startLifetime.constantMax;
            if (main.loop)
            {
                lifetime = Mathf.Max(lifetime, LoopingFxMinimumLifetimeSeconds);
            }

            return Mathf.Max(MinimumFxCleanupDelaySeconds, lifetime + FxCleanupSafetyBufferSeconds);
        }

        private void HandleFastTravelModeChanged(HubInputMode mode)
        {
            if (mode == HubInputMode.Traversal)
            {
                return;
            }

            CloseFastTravelMenu(force: true);

            if (_fastTravelRoutine != null)
            {
                StopCoroutine(_fastTravelRoutine);
                _fastTravelRoutine = null;
            }

            _isFastTravelInProgress = false;
            _isTraversalMovementLocked = false;
        }

        private OperationResult ValidateFastTravelStart(UnlockableWorldLocationOption destination)
        {
            var validation = OperationResultGuards.All(
                OperationResultGuards.RequireNotNull(
                    destination.UnlockableLocation,
                    "destination.UnlockableLocation"
                ),
                OperationResultGuards.RequireNotNull(
                    destination.UnlockableLocation?.fastTravelPoint,
                    "destination.UnlockableLocation.fastTravelPoint"
                )
            );
            return !validation.Success ? validation
                : CurrentInputMode != HubInputMode.Traversal
                    ? OperationResult.Failure(
                        "Fast travel can only start while the hub is in Traversal mode."
                    )
                : MovementRig == null && _avatarRoot == null
                    ? OperationResult.Failure(
                        "No fast-travel anchor is available (MovementRig and avatar root are both null)."
                    )
                : OperationResult.Successful();
        }
        #endregion
    }
}