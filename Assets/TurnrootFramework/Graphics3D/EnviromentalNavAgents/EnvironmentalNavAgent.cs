using UnityEngine;
using UnityEngine.AI;

namespace Turnroot.Graphics3D
{
    public class EnvironmentalNavAgent : MonoBehaviour
    {
        [Header("Components")]
        public NavMeshAgent agent;
        public Animator animator;

        [Header("Animation")]
        public AnimationClip walkClip;
        public AnimationClip[] idleClips;

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
        private string _currentAnimState;

        private void Start()
        {
            _noiseOffset = Random.Range(0f, 1000f);
            PickNewDestination();
            EnterIdle();
        }

        private void Update()
        {
            _stateTimer -= Time.deltaTime;
            _destinationChangeTimer -= Time.deltaTime;

            if (_destinationChangeTimer <= 0f)
            {
                PickNewDestination();
            }

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
                PlayAnimation(idleClips[_currentIdleIndex].name);
            }
        }

        private void EnterWalking()
        {
            _state = WanderState.Walking;
            _stateTimer = Random.Range(minWalkTime, maxWalkTime);
            agent.speed = Random.Range(minWalkSpeed, maxWalkSpeed);
            agent.isStopped = false;
            agent.SetDestination(_wanderTarget);

            if (walkClip != null)
                PlayAnimation(walkClip.name);
        }

        private void UpdateIdle()
        {
            if (_stateTimer <= 0f)
                EnterWalking();
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

        private void PlayAnimation(string stateName)
        {
            if (animator == null || stateName == _currentAnimState)
            {
                return;
            }

            _currentAnimState = stateName;
            animator.CrossFadeInFixedTime(stateName, blendDuration);
        }
    }
}
