using System.Collections.Generic;

namespace Turnroot.Characters.Stats
{
    /// <summary>
    /// Interface for any entity that has bounded and unbounded character stats.
    /// Implemented by CharacterData (templates) and CharacterInstance (runtime state).
    /// Provides unified access to stats for shared operations.
    /// </summary>
    public interface IHasStats
    {
        /// <summary>
        /// Get all bounded stats (HP, Stamina, etc. with min/max).
        /// </summary>
        List<BoundedCharacterStat> BoundedStats { get; }

        /// <summary>
        /// Get all unbounded stats (Strength, Speed, etc.).
        /// </summary>
        List<CharacterStat> UnboundedStats { get; }

        /// <summary>
        /// Get a specific bounded stat by type.
        /// </summary>
        BoundedCharacterStat GetBoundedStat(BoundedStatType type);

        /// <summary>
        /// Get a specific unbounded stat by type.
        /// </summary>
        CharacterStat GetUnboundedStat(UnboundedStatType type);
    }
}
