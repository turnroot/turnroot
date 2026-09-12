using System.Collections.Generic;
using Turnroot.Characters;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class UnitAppearanceBrain
    {
        private readonly Dictionary<int, Coroutine> _hubIdleCoroutines = new();

        /// <summary>
        /// Configures a hub unit model to play the current class's idle animations
        /// on loop, blending randomly through them
        ///
        /// Call this after <see cref="CreateModelForUnit"/> both when HubTeamLocations spawns hub
        /// unit models and when HubCharacter spawns its avatar model
        /// </summary>
        public void SetupHubIdleAnimation(GameObject model, CharacterInstance unit)
        {
            if (model == null || unit == null)
            {
                return;
            }

            var animator = model.GetComponent<Animator>();

            // CreateModelForUnit doesn't create an controller, just use the override

            var baseController = new RuntimeAnimatorController();
            animator.runtimeAnimatorController = baseController;

            var idleClips = ResolveHubIdleClips(unit);
            if (idleClips == null || idleClips.Length == 0)
            {
                return;
            }

            // so we make the base controller so the AOC has something to override,
            // then immediately just override it. Seems sloppy but I don't know a better way at the moment

            var overrideController = new AnimatorOverrideController(baseController);

            var firstClip = idleClips[Random.Range(0, idleClips.Length)];
            if (firstClip != null)
            {
                overrideController[IdleState] = firstClip;
            }

            // Stop any existing idle loop for this model before starting a new one
            int modelId = model.GetInstanceID();
            if (
                _hubIdleCoroutines.TryGetValue(modelId, out var existingRoutine)
                && existingRoutine != null
            )
            {
                StopCoroutine(existingRoutine);
            }

            animator.runtimeAnimatorController = overrideController;
            animator.enabled = true;

            StartCoroutine(PlayIdleAnimationNextFrame(animator));
            _hubIdleCoroutines[modelId] = StartCoroutine(IdleVariationRoutine(animator, idleClips));
        }

        private AnimationClip[] ResolveHubIdleClips(CharacterInstance unit)
        {
            var classData = unit.GetCurrentClass()?.ClassData;
            return classData?.IdleAnimations;
        }
    }
}
