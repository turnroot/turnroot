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
        #region Travel Execution

        private void StartFastTravel(UnlockableWorldLocationOption destination)
        {
            if (_isFastTravelInProgress)
            {
                return;
            }

            var validation = ValidateFastTravelStart(destination);
            if (!validation.Success)
            {
                $"HubManager: Fast travel aborted. {validation.ErrorMessage}".LogWarning();
                return;
            }

            if (_fastTravelRoutine != null)
            {
                StopCoroutine(_fastTravelRoutine);
                _fastTravelRoutine = null;
            }

            _fastTravelRoutine = StartCoroutine(RunFastTravelSequence(destination));
        }

        private IEnumerator RunFastTravelSequence(UnlockableWorldLocationOption destination)
        {
            _isFastTravelInProgress = true;
            _isTraversalMovementLocked = true;

            SetInput(Vector2.zero, _lookInput);
            SetWalkingState(false);
            if (NavMeshAgent != null)
            {
                NavMeshAgent.velocity = Vector3.zero;
            }

            SpawnAndPlayFastTravelFx(FastTravelDepartureFxPrefab);
            PlayFastTravelSfx(FastTravelDepartureClip);

            if (FastTravelTeleportDelay > 0f)
            {
                yield return new WaitForSeconds(FastTravelTeleportDelay);
            }

            OperationResult sequenceResult = PerformFastTravelTeleport(destination);
            if (!sequenceResult.Success)
            {
                CompleteFastTravelOperation(sequenceResult, "teleport");
                yield break;
            }

            SpawnAndPlayFastTravelFx(FastTravelArrivalFxPrefab);

            if (FastTravelRecoveryDelay > 0f)
            {
                yield return new WaitForSeconds(FastTravelRecoveryDelay);
            }

            CompleteFastTravelOperation(sequenceResult, "complete");
        }

        private void CompleteFastTravelOperation(OperationResult result, string stage)
        {
            if (!result.Success)
            {
                $"HubManager: Fast travel {stage} failed. {result.ErrorMessage}".LogWarning();
            }

            _isTraversalMovementLocked = false;
            _isFastTravelInProgress = false;
            _fastTravelRoutine = null;
        }

        private OperationResult PerformFastTravelTeleport(UnlockableWorldLocationOption destination)
        {
            var traversalRoot = MovementRig != null ? MovementRig : _avatarRoot;
            if (traversalRoot == null)
            {
                return OperationResult.Failure(
                    "No traversal root is available. Assign MovementRig or ensure avatar binding exists."
                );
            }

            Vector3 previousPosition = traversalRoot.position;
            Vector3 targetPosition = destination.UnlockableLocation.fastTravelPoint.position;

            if (NavMeshAgent != null && NavMeshAgent.enabled)
            {
                if (!NavMeshAgent.Warp(targetPosition))
                {
                    traversalRoot.position = targetPosition;
                }
            }
            else
            {
                traversalRoot.position = targetPosition;
            }

            if (MovementRig != null)
            {
                MovementRig.rotation = Quaternion.Euler(
                    0f,
                    destination.UnlockableLocation.fastTravelPoint.eulerAngles.y,
                    0f
                );
            }

            if (CameraYawRoot != null)
            {
                CameraYawRoot.position = traversalRoot.position;
            }

            CurrentLocationName = destination.LocationName;
            CurrentLocationPoint = destination.UnlockableLocation.fastTravelPoint;
            CurrentTraversalAvatarPoint = destination.UnlockableLocation.fastTravelPoint;

            Vector3 delta = traversalRoot.position - previousPosition;
            if (TraversalVcam != null && delta.sqrMagnitude > 0.0001f)
            {
                TraversalVcam.OnTargetObjectWarped(traversalRoot, delta);
            }
            else if (GeneralCamera != null)
            {
                GeneralCamera.transform.position += delta;
            }

            return OperationResult.Successful();
        }

        #endregion
    }
}