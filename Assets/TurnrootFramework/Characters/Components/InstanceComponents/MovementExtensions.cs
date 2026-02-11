using Turnroot.GameSettings;

namespace Turnroot.Characters
{
    /// <summary>
    /// Extension methods for CharacterInstance related to movement and mounting.
    /// </summary>
    public static class CharacterInstanceMovementExtensions
    {
        /// <summary>
        /// Gets the effective movement type for this character, accounting for mount status.
        /// If a mounted class is dismounted, returns Infantry. Otherwise returns class movement type.
        /// </summary>
        public static MovementType GetEffectiveMovementType(this CharacterInstance instance)
        {
            if (instance == null)
            {
                return MovementType.Infantry;
            }

            var classData = instance.CurrentClassTemplate;
            if (classData == null || classData.Identity == null)
            {
                return MovementType.Infantry;
            }

            var classMovementType = classData.Identity.MovementType;

            // If class is mounted but unit is dismounted, treat as infantry
            return classData.Identity.IsMountedClass() && !instance.IsMounted
                ? MovementType.Infantry
                : classMovementType;
        }

        /// <summary>
        /// Checks if this character can be mounted (has a mounted class with mount visuals).
        /// </summary>
        public static bool CanBeMounted(this CharacterInstance instance)
        {
            if (instance == null)
            {
                return false;
            }

            var classData = instance.CurrentClassTemplate;
            return classData.Identity.HasMountVisuals() == true;
        }
    }
}
