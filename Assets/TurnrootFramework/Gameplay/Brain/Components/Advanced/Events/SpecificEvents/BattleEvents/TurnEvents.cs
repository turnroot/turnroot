namespace Turnroot.Gameplay.Brain.Events
{
    /// <summary>
    /// Typed event published when a new battle round begins.
    /// </summary>
    public class BattleRoundStartedEvent : BattleEvent
    {
        /// <summary>
        /// Creates a round-start event for the specified battle turn number.
        /// </summary>
        /// <param name="turnNumber">The round number that has just started.</param>
        public BattleRoundStartedEvent(int turnNumber)
        {
            TurnNumber = turnNumber;
        }
    }

    /// <summary>
    /// Typed event published when the current battle round ends.
    /// </summary>
    public class BattleRoundEndedEvent : BattleEvent
    {
        /// <summary>
        /// Creates a round-end event for the specified battle turn number.
        /// </summary>
        /// <param name="turnNumber">The round number that has just ended.</param>
        public BattleRoundEndedEvent(int turnNumber)
        {
            TurnNumber = turnNumber;
        }
    }

    /// <summary>
    /// Typed event published when a faction phase starts.
    /// </summary>
    public class FactionPhaseStartedEvent : BattleEvent
    {
        /// <summary>
        /// The faction whose phase is starting.
        /// </summary>
        public enum Faction
        {
            Player,
            Enemy,
            ThirdParty,
        }

        /// <summary>The faction whose phase has started.</summary>
        public Faction ActiveFaction { get; }

        /// <summary>
        /// Creates a faction-phase-start event.
        /// </summary>
        /// <param name="faction">The faction whose phase is starting.</param>
        /// <param name="turnNumber">The battle round in which the phase starts.</param>
        public FactionPhaseStartedEvent(Faction faction, int turnNumber)
        {
            ActiveFaction = faction;
            TurnNumber = turnNumber;
        }
    }

    /// <summary>
    /// Typed event published when a faction phase ends.
    /// </summary>
    public class FactionPhaseEndedEvent : BattleEvent
    {
        /// <summary>The faction whose phase has ended.</summary>
        public FactionPhaseStartedEvent.Faction EndedFaction { get; }

        /// <summary>
        /// Creates a faction-phase-end event.
        /// </summary>
        /// <param name="faction">The faction whose phase has ended.</param>
        /// <param name="turnNumber">The battle round in which the phase ended.</param>
        public FactionPhaseEndedEvent(FactionPhaseStartedEvent.Faction faction, int turnNumber)
        {
            EndedFaction = faction;
            TurnNumber = turnNumber;
        }
    }
}
