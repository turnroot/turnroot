using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Turnroot.Graphics3D
{
    [RequireComponent(typeof(NavMeshAgent), typeof(Animator), typeof(AudioSource))]
    public class EnvironmentalNavAgent : MonoBehaviour
    {
        [Header("Components")]
        public NavMeshAgent agent;
        public Animator animator;

        public AudioClip[] neutralSounds;
        private AudioClip currentSound;

        public bool randomizeStartPosition = false;

        [Header("Animation")]
        public AnimationClip walkClip;
        public AnimationClip[] idleClips;
        public float walkAnimationSpeedMultiplier = .7f;

        [Range(0.05f, 1f)]
        public float blendDuration = 0.25f;

        [Header("Wander")]
        public float minWalkSpeed = 0.4f;
        public float maxWalkSpeed = 0.9f;

        public float minIdleTime = 4f;
        public float maxIdleTime = 12f;
        public float minWalkTime = 2f;
        public float maxWalkTime = 5f;
        public float minDestinationChangeInterval = 10f;
        public float maxDestinationChangeInterval = 20f;

        [Header("Wander - Path Noise")]
        public float noiseAmount = 2.5f;
        public float noiseFrequency = 0.8f;
        public float noiseSpeed = 0.15f;

        [Header("Wander - Destination")]
        public float wanderRadius = 8f;

        private enum WanderState
        {
            Idle,
            Walking,
        }

        private WanderState _state = WanderState.Idle;

        private float _stateTimer;
        private float _destinationChangeTimer;
        private Vector3 _wanderTarget;
        private float _noiseOffset;
        private int _currentIdleIndex;

        private PlayableGraph _playableGraph;
        private AnimationMixerPlayable _mixer;
        private AnimationClipPlayable _fromPlayable;
        private AnimationClipPlayable _toPlayable;
        private float _crossfadeTimer;
        private bool _crossfading;

        private void Start()
        {
            _noiseOffset = Random.Range(0f, 1000f);
            if (animator != null)
            {
                InitPlayableGraph();
            }

            PickNewDestination();
            if (randomizeStartPosition)
            {
                if (TryGetComponent<RandomizePosition>(out var randomizer))
                {
                    randomizer.SetPosition(transform);
                }
                else
                {
                    "Cannot randomize start position- add a RandomizePosition component".LogWarning();
                }
            }
            EnterIdle();
        }

        private void OnDestroy()
        {
            if (_playableGraph.IsValid())
            {
                _playableGraph.Destroy();
            }
        }

        private void Update()
        {
            _stateTimer -= Time.deltaTime;
            _destinationChangeTimer -= Time.deltaTime;

            if (_destinationChangeTimer <= 0f)
            {
                PickNewDestination();
            }

            UpdateCrossfade();

            switch (_state)
            {
                case WanderState.Idle:
                    UpdateIdle();
                    break;
                case WanderState.Walking:
                    UpdateWalking();
                    break;
            }
        }

        private void EnterIdle()
        {
            _state = WanderState.Idle;
            _stateTimer = Random.Range(minIdleTime, maxIdleTime);
            agent.isStopped = true;

            if (idleClips != null && idleClips.Length > 0)
            {
                _currentIdleIndex = Random.Range(0, idleClips.Length);
                PlayClip(idleClips[_currentIdleIndex]);
            }
        }

        private void EnterWalking()
        {
            _state = WanderState.Walking;
            _stateTimer = Random.Range(minWalkTime, maxWalkTime);
            agent.speed = Random.Range(minWalkSpeed, maxWalkSpeed);
            agent.isStopped = false;
            agent.SetDestination(_wanderTarget);

            PlayClip(walkClip);
            _toPlayable.SetSpeed(agent.speed * walkAnimationSpeedMultiplier);
        }

        private void UpdateIdle()
        {
            if (_stateTimer <= 0f)
            {
                EnterWalking();
            }
        }

        private void UpdateWalking()
        {
            float noiseTime = Time.time * noiseSpeed + _noiseOffset;
            float lateral =
                (Mathf.PerlinNoise(noiseTime, noiseTime * noiseFrequency) - 0.5f)
                * 2f
                * noiseAmount;

            Vector3 toTarget = (_wanderTarget - transform.position).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, toTarget);
            Vector3 noisyTarget = _wanderTarget + right * lateral;

            if (
                NavMesh.SamplePosition(
                    noisyTarget,
                    out NavMeshHit hit,
                    noiseAmount * 2f,
                    NavMesh.AllAreas
                )
            )
            {
                agent.SetDestination(hit.position);
            }
            else
            {
                agent.SetDestination(_wanderTarget);
            }

            bool reachedDestination =
                !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f;

            if (_stateTimer <= 0f || reachedDestination)
            {
                PickNewDestination();
                EnterIdle();
            }
        }

        private void PickNewDestination()
        {
            _destinationChangeTimer = Random.Range(
                minDestinationChangeInterval,
                maxDestinationChangeInterval
            );

            Vector2 circle = Random.insideUnitCircle * wanderRadius;
            Vector3 candidate = transform.position + new Vector3(circle.x, 0f, circle.y);

            if (
                NavMesh.SamplePosition(
                    candidate,
                    out NavMeshHit hit,
                    wanderRadius,
                    NavMesh.AllAreas
                )
            )
            {
                _wanderTarget = hit.position;
            }
        }

        private void InitPlayableGraph()
        {
            _playableGraph = PlayableGraph.Create(gameObject.name + "_EnvNav");
            _playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            _mixer = AnimationMixerPlayable.Create(_playableGraph, 2);
            var output = AnimationPlayableOutput.Create(_playableGraph, "Animation", animator);
            output.SetSourcePlayable(_mixer);
            _playableGraph.Play();
        }

        private void PlayClip(AnimationClip clip)
        {
            if (clip == null || !_playableGraph.IsValid())
            {
                return;
            }

            bool hasFrom = _toPlayable.IsValid();

            if (_fromPlayable.IsValid())
            {
                _mixer.DisconnectInput(0);
                _fromPlayable.Destroy();
                _fromPlayable = default;
            }

            if (hasFrom)
            {
                _mixer.DisconnectInput(1);
                _fromPlayable = _toPlayable;
                _toPlayable = default;
                _mixer.ConnectInput(0, _fromPlayable, 0);
                _mixer.SetInputWeight(0, 1f);
            }

            _toPlayable = AnimationClipPlayable.Create(_playableGraph, clip);
            _mixer.ConnectInput(1, _toPlayable, 0);

            if (hasFrom)
            {
                _mixer.SetInputWeight(1, 0f);
                _crossfadeTimer = 0f;
                _crossfading = true;
            }
            else
            {
                _mixer.SetInputWeight(0, 0f);
                _mixer.SetInputWeight(1, 1f);
                _crossfading = false;
            }
        }

        private void UpdateCrossfade()
        {
            if (!_crossfading)
            {
                return;
            }

            _crossfadeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_crossfadeTimer / blendDuration);
            _mixer.SetInputWeight(0, 1f - t);
            _mixer.SetInputWeight(1, t);
            if (t >= 1f)
            {
                _crossfading = false;
                if (_fromPlayable.IsValid())
                {
                    _mixer.DisconnectInput(0);
                    _fromPlayable.Destroy();
                    _fromPlayable = default;
                }
            }
        }

        public OperationResult EmitSound()
        {
            AudioSource source = GetComponent<AudioSource>();

            if (neutralSounds != null && neutralSounds.Length > 0)
            {
                AudioClip newClip;
                do
                {
                    newClip = neutralSounds[Random.Range(0, neutralSounds.Length)];
                } while (newClip == currentSound && neutralSounds.Length > 1);
                currentSound = newClip;
                source.PlayOneShot(currentSound);
                return OperationResult.Successful();
            }
            else
            {
                return OperationResult.Failure(
                    "No neutral sounds assigned to EnvironmentalNavAgent."
                );
            }
        }
    }
}
