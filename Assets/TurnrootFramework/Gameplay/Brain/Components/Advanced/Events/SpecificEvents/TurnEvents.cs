namespace Turnroot.Gameplay.Brain.Events
{
    /// <summary>
    /// Published when a new battle round begins (all factions have acted).
    /// </summary>
    public class TurnStartedEvent : BattleEvent
    {
        public TurnStartedEvent(int turnNumber)
        {
            TurnNumber = turnNumber;
        }
    }

    /// <summary>
    /// Published when a battle round ends.
    /// </summary>
    public class TurnEndedEvent : BattleEvent
    {
        public TurnEndedEvent(int turnNumber)
        {
            TurnNumber = turnNumber;
        }
    }

    /// <summary>
    /// Published when a faction's phase starts.
    /// </summary>
    public class FactionTurnStartedEvent : BattleEvent
    {
        public enum Faction
        {
            Player,
            Enemy,
            ThirdParty,
        }

        public Faction ActiveFaction { get; }

        public FactionTurnStartedEvent(Faction faction, int turnNumber)
        {
            ActiveFaction = faction;
            TurnNumber = turnNumber;
        }
    }

    /// <summary>
    /// Published when a faction's phase ends.
    /// </summary>
    public class FactionTurnEndedEvent : BattleEvent
    {
        public FactionTurnStartedEvent.Faction EndedFaction { get; }

        public FactionTurnEndedEvent(FactionTurnStartedEvent.Faction faction, int turnNumber)
        {
            EndedFaction = faction;
            TurnNumber = turnNumber;
        }
    }
}
