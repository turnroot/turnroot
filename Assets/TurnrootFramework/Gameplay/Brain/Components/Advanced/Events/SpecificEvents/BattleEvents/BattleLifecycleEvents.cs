namespace Turnroot.Gameplay.Brain.Events
{
    /// <summary>
    /// Published when a battle starts.
    /// </summary>
    public class BattleStartedEvent : BattleEvent
    {
        public string BattleId { get; }

        public BattleStartedEvent(string battleId = null)
        {
            BattleId = battleId ?? System.Guid.NewGuid().ToString();
            TurnNumber = 1;
        }
    }

    /// <summary>
    /// Published when a battle ends.
    /// </summary>
    public class BattleEndedEvent : BattleEvent
    {
        /// <summary>
        /// Represents the possible outcomes of a battle.
        /// </summary>
        public enum BattleResult
        {
            Victory,
            Defeat,
            Retreat,
            Draw,
        }

        public BattleResult Result { get; }
        public string BattleId { get; }

        public BattleEndedEvent(BattleResult result, int finalTurn, string battleId = null)
        {
            Result = result;
            TurnNumber = finalTurn;
            BattleId = battleId;
        }
    }
}
