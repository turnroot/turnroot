using Turnroot.Characters;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class UnitAppearanceBrain
    {
        /// <summary>
        /// Try to instantiate a prefab under <paramref name="parent"/> with a consistent name and logging.
        /// Returns the created GameObject or null on failure.
        /// </summary>
        public static GameObject TryInstantiatePrefab(GameObject prefab, Transform parent, string name = null, string context = null)
        {
            if (prefab == null)
            {
                if (!string.IsNullOrEmpty(context))
                {
                    // We cannot call instance LogWarning from static context; use the logger directly.
                    Turnroot.Utilities.TurnrootLogger.Log($"{context}: prefab is null", Turnroot.Utilities.TurnrootLogger.LogLevel.Warning);
                }
                return null;
            }

            var instance = UnityEngine.Object.Instantiate(prefab, parent);
            if (instance == null)
            {
                Turnroot.Utilities.TurnrootLogger.Log($"{context ?? "UnitAppearanceBrain"}: Failed to instantiate prefab '{prefab.name}'", Turnroot.Utilities.TurnrootLogger.LogLevel.Warning);
                return null;
            }

            if (!string.IsNullOrEmpty(name))
            {
                instance.name = name;
            }

            return instance;
        }

        /// <summary>
        /// Assigns a runtime animator controller if present, logs a single consistent warning if missing,
        /// and optionally calls <see cref="SetupWalkAnimation"/> to complete setup.
        /// </summary>
        public void AssignAnimatorController(Animator animator, RuntimeAnimatorController controllerToUse, GameObject model = null, CharacterInstance unit = null, bool callSetupWalk = false)
        {
            if (animator == null)
            {
                return;
            }

            if (controllerToUse != null)
            {
                animator.runtimeAnimatorController = controllerToUse;
            }
            else
            {
                var displayName = unit?.CharacterTemplate?.DisplayName ?? model?.name ?? "<unknown>";
                LogWarning($"No animator controller available for {displayName}. Set MountAnimator on class or DefaultUnitAnimatorController in settings.");
            }

            if (callSetupWalk && model != null && unit != null)
            {
                SetupWalkAnimation(model, unit);
            }
        }
    }
}