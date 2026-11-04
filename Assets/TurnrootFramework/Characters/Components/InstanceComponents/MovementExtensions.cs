using Turnroot.GameSettings;

namespace Turnroot.Characters
{
    /// <summary>
    /// Extension methods for CharacterInstance related to movement and mounting.
    /// </summary>
    public static class CharacterInstanceMovementExtensions
    {
        /// <summary>
        /// Gets the effective movement budget. A mounted class uses its class movement
        /// while mounted and the character's base movement when dismounted.
        /// </summary>
        public static int GetEffectiveMovement(this CharacterInstance instance)
        {
            if (instance == null)
            {
                return 0;
            }

            var movementStat = instance.GetUnboundedStat(Stats.UnboundedStatType.Movement);
            int movementBonus = movementStat?.BonusInt ?? 0;
            var classIdentity = instance.CurrentClassTemplate?.Identity;
            if (
                classIdentity != null
                && !(classIdentity.IsMountedClass() && !instance.IsMounted)
            )
            {
                return classIdentity.Movement + movementBonus;
            }

            return (movementStat?.CurrentInt ?? 0) + movementBonus;
        }

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
