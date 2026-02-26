namespace Turnroot.Gameplay.Brain.Commands
{
    /// <summary>
    /// Common key names used when persisting command undo state.
    /// String constants are used to avoid hardcoded literals appearing in multiple locations.
    /// </summary>
    public static class UndoStateKeys
    {
        // generic
        public const string From = "from";

        // spawn/move
        public const string WasSpawned = "wasSpawned";

        // health/damage
        public const string PrevHP = "prevHP";
        public const string WasDefeated = "wasDefeated";

        // attacker/target tracking
        public const string PrevLastTarget = "prevLastTarget";
        public const string PrevLastAttackerOfTarget = "prevLastAttackerOfTarget";
        public const string PrevTargetLastAttacker = "prevTargetLastAttacker";

        // add more keys here as needed
    }
}
