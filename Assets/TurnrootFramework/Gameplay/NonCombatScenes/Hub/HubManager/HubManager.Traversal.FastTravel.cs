using System;
using System.Collections;
using NaughtyAttributes;
using Turnroot.UI;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [Serializable]
    public struct HubFastTravelOption
    {
        public UiChoice Choice;
        public HubTeleportPoint TeleportPoint;
    }

    public partial class HubManager : MonoBehaviour
    {
        [BoxGroup("Traversal Fast Travel")]
        [InfoBox("UI fade shown when opening the traversal fast-travel location list.")]
        public UIFade FastTravelChoicesFade;

        [BoxGroup("Traversal Fast Travel")]
        [InfoBox("Ordered options shown when ToggleDetails opens fast travel.")]
        public HubFastTravelOption[] FastTravelOptions;

        [BoxGroup("Traversal Fast Travel")]
        [Min(0f)]
        [Tooltip("Delay between starting travel FX and the actual teleport.")]
        public float FastTravelTeleportDelay = 0.65f;

        [BoxGroup("Traversal Fast Travel")]
        [Min(0f)]
        [Tooltip("Small delay after arrival FX before movement is restored.")]
        public float FastTravelRecoveryDelay = 0.2f;

        [BoxGroup("Traversal Fast Travel")]
        [InfoBox("Particle prefab spawned on the avatar model before teleport.")]
        public ParticleSystem FastTravelDepartureFxPrefab;

        [BoxGroup("Traversal Fast Travel")]
        [InfoBox("Particle prefab spawned on the avatar model after teleport.")]
        public ParticleSystem FastTravelArrivalFxPrefab;

        [BoxGroup("Traversal Fast Travel")]
        [InfoBox("Audio source used for fast-travel SFX.")]
        public AudioSource FastTravelAudioSource;

        [BoxGroup("Traversal Fast Travel")]
        [Tooltip("SFX played when fast travel begins.")]
        public AudioClip FastTravelDepartureClip;

        [BoxGroup("Traversal Fast Travel")]
        [Tooltip("SFX played after arrival teleport completes.")]
        public AudioClip FastTravelArrivalClip;

        private UiChoice[] _fastTravelNavigableChoices;
        private HubFastTravelOption[] _fastTravelNavigableOptions;
        private int _fastTravelChoiceIndex;
        private bool _fastTravelMenuOpen;
        private bool _isFastTravelInProgress;
        private bool _isTraversalMovementLocked;
        private Coroutine _fastTravelRoutine;

        private const float DefaultFxCleanupDelaySeconds = 2f;
        private const float LoopingFxMinimumLifetimeSeconds = 3f;
        private const float MinimumFxCleanupDelaySeconds = 1f;
        private const float FxCleanupSafetyBufferSeconds = 0.25f;

        private bool TryHandleFastTravelInput(string action)
        {
            if (CurrentInputMode != HubInputMode.Traversal)
            {
                return false;
            }

            if (_isFastTravelInProgress)
            {
                return true;
            }

            if (_fastTravelMenuOpen)
            {
                if (action is InputActionConstants.Back or InputActionConstants.Cancel)
                {
                    CloseFastTravelMenu();
                    return true;
                }

                if (_fastTravelNavigableChoices == null || _fastTravelNavigableChoices.Length == 0)
                {
                    return true;
                }

                UiChoiceHandler.HandleNavigation(
                    action,
                    _fastTravelNavigableChoices,
                    ref _fastTravelChoiceIndex,
                    _fastTravelNavigableChoices.Length,
                    OnFastTravelChoiceSelected
                );

                UpdateFastTravelChoiceSelection();
                return true;
            }

            if (action == InputActionConstants.ToggleDetails)
            {
                OpenFastTravelMenu();
                return true;
            }

            return false;
        }

        private void OpenFastTravelMenu()
        {
            if (_isFastTravelInProgress || _fastTravelMenuOpen)
            {
                return;
            }

            if (
                !ValidationHelper.ValidateNotNull(
                    nameof(OpenFastTravelMenu),
                    (FastTravelChoicesFade, nameof(FastTravelChoicesFade))
                )
            )
            {
                return;
            }

            var optionsBuild = BuildFastTravelOptions();
            if (!optionsBuild.Success)
            {
                $"HubManager: Fast travel menu cannot open. {optionsBuild.ErrorMessage}".LogWarning();
                return;
            }

            _fastTravelMenuOpen = true;
            _fastTravelChoiceIndex = 0;

            FastTravelChoicesFade?.Show();
            UpdateFastTravelChoiceSelection();
        }

        private void CloseFastTravelMenu(bool force = false)
        {
            if (!_fastTravelMenuOpen && !force)
            {
                return;
            }

            _fastTravelMenuOpen = false;
            FastTravelChoicesFade?.Hide();
            DeselectAllFastTravelChoices();
        }

        private OperationResult BuildFastTravelOptions()
        {
            if (FastTravelOptions == null || FastTravelOptions.Length == 0)
            {
                _fastTravelNavigableChoices = Array.Empty<UiChoice>();
                _fastTravelNavigableOptions = Array.Empty<HubFastTravelOption>();
                return OperationResult.Failure("No FastTravelOptions are configured.");
            }

            int validCount = 0;
            for (int i = 0; i < FastTravelOptions.Length; i++)
            {
                if (FastTravelOptions[i].Choice != null)
                {
                    validCount++;
                }
            }

            if (validCount == 0)
            {
                _fastTravelNavigableChoices = Array.Empty<UiChoice>();
                _fastTravelNavigableOptions = Array.Empty<HubFastTravelOption>();
            }
            else
            {
                _fastTravelNavigableChoices = new UiChoice[validCount];
                _fastTravelNavigableOptions = new HubFastTravelOption[validCount];

                int writeIndex = 0;
                for (int i = 0; i < FastTravelOptions.Length; i++)
                {
                    var option = FastTravelOptions[i];
                    if (option.Choice == null)
                    {
                        continue;
                    }

                    _fastTravelNavigableChoices[writeIndex] = option.Choice;
                    _fastTravelNavigableOptions[writeIndex] = option;
                    writeIndex++;
                }
            }

            if (_fastTravelNavigableChoices.Length == 0)
            {
                return OperationResult.Failure(
                    "FastTravelOptions are configured, but none have a valid UiChoice."
                );
            }

            if (
                _fastTravelChoiceIndex < 0
                || _fastTravelChoiceIndex >= _fastTravelNavigableChoices.Length
            )
            {
                _fastTravelChoiceIndex = 0;
            }

            return OperationResult.Successful();
        }

        private void UpdateFastTravelChoiceSelection()
        {
            if (_fastTravelNavigableChoices == null)
            {
                return;
            }

            for (int i = 0; i < _fastTravelNavigableChoices.Length; i++)
            {
                var choice = _fastTravelNavigableChoices[i];
                if (choice == null)
                {
                    continue;
                }

                if (i == _fastTravelChoiceIndex)
                {
                    choice.Select();
                }
                else
                {
                    choice.Deselect();
                }
            }
        }

        private void DeselectAllFastTravelChoices()
        {
            if (_fastTravelNavigableChoices == null)
            {
                return;
            }

            for (int i = 0; i < _fastTravelNavigableChoices.Length; i++)
            {
                _fastTravelNavigableChoices[i]?.Deselect();
            }
        }

        private void OnFastTravelChoiceSelected()
        {
            if (
                _fastTravelNavigableOptions == null
                || _fastTravelChoiceIndex < 0
                || _fastTravelChoiceIndex >= _fastTravelNavigableOptions.Length
            )
            {
                return;
            }

            var option = _fastTravelNavigableOptions[_fastTravelChoiceIndex];
            var destination = option.TeleportPoint;
            var destinationValidation = OperationResultGuards.RequireNotNull(
                destination.Point,
                $"{nameof(HubFastTravelOption)}.{nameof(HubFastTravelOption.TeleportPoint)}.{nameof(HubTeleportPoint.Point)}"
            );
            if (!destinationValidation.Success)
            {
                $"HubManager: Cannot start fast travel. {destinationValidation.ErrorMessage}".LogWarning();
                return;
            }

            CloseFastTravelMenu();
            StartFastTravel(destination);
        }

        private void StartFastTravel(HubTeleportPoint destination)
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

        private IEnumerator RunFastTravelSequence(HubTeleportPoint destination)
        {
            _isFastTravelInProgress = true;
            _isTraversalMovementLocked = true;

            try
            {
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

                var teleportResult = PerformFastTravelTeleport(destination);
                if (!teleportResult.Success)
                {
                    $"HubManager: Fast travel teleport step failed. {teleportResult.ErrorMessage}".LogWarning();
                    yield break;
                }

                PlayFastTravelSfx(FastTravelArrivalClip);
                SpawnAndPlayFastTravelFx(FastTravelArrivalFxPrefab);

                if (FastTravelRecoveryDelay > 0f)
                {
                    yield return new WaitForSeconds(FastTravelRecoveryDelay);
                }
            }
            finally
            {
                _isTraversalMovementLocked = false;
                _isFastTravelInProgress = false;
                _fastTravelRoutine = null;
            }
        }

        private OperationResult PerformFastTravelTeleport(HubTeleportPoint destination)
        {
            var destinationValidation = OperationResultGuards.RequireNotNull(
                destination.Point,
                "destination.Point"
            );
            if (!destinationValidation.Success)
            {
                return destinationValidation;
            }

            var traversalRoot = MovementRig != null ? MovementRig : _avatarRoot;
            if (traversalRoot == null)
            {
                return OperationResult.Failure(
                    "No traversal root is available. Assign MovementRig or ensure avatar binding exists."
                );
            }

            Vector3 previousPosition = traversalRoot.position;
            Vector3 targetPosition = destination.Point.position;

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
                MovementRig.rotation = Quaternion.Euler(0f, destination.Point.eulerAngles.y, 0f);
            }

            if (CameraYawRoot != null)
            {
                CameraYawRoot.position = traversalRoot.position;
            }

            CurrentLocationName = destination.Name;
            CurrentLocationPoint = destination.Point;
            CurrentTraversalAvatarPoint = destination.Point;

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

            var fx = Instantiate(prefab, anchor.position, anchor.rotation, anchor);
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

        private OperationResult ValidateFastTravelStart(HubTeleportPoint destination)
        {
            var validation = OperationResultGuards.All(
                OperationResultGuards.RequireNotNull(destination.Point, "destination.Point"),
                OperationResultGuards.RequireNotNull(
                    FastTravelDepartureFxPrefab,
                    nameof(FastTravelDepartureFxPrefab)
                ),
                OperationResultGuards.RequireNotNull(
                    FastTravelArrivalFxPrefab,
                    nameof(FastTravelArrivalFxPrefab)
                ),
                OperationResultGuards.RequireNotNull(
                    FastTravelAudioSource,
                    nameof(FastTravelAudioSource)
                ),
                OperationResultGuards.RequireNotNull(
                    FastTravelDepartureClip,
                    nameof(FastTravelDepartureClip)
                ),
                OperationResultGuards.RequireNotNull(
                    FastTravelArrivalClip,
                    nameof(FastTravelArrivalClip)
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
    }
}
