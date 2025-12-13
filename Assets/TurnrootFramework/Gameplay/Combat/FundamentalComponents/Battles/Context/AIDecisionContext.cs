using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Locations;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Context
{
    /// <summary>
    /// Focused context for AI decision-making.
    /// Contains data needed for the AI to evaluate moves and actions.
    /// </summary>
    public readonly struct AIDecisionContext
    {
        private readonly BattleContext _context;

        public AIDecisionContext(BattleContext context)
        {
            _context = context;
        }

        #region Map Access

        /// <summary>
        /// The battle map grid.
        /// </summary>
        public MapGrid MapGrid => _context.mapGrid;

        #endregion

        #region Unit Information

        /// <summary>
        /// The AI-controlled unit making decisions.
        /// </summary>
        public CharacterInstance AIUnit => _context.UnitInstance;

        /// <summary>
        /// All allied units (from AI's perspective).
        /// </summary>
        public IReadOnlyList<CharacterInstance> Allies => _context.Allies;

        /// <summary>
        /// All enemy units (from AI's perspective).
        /// </summary>
        public IReadOnlyList<CharacterInstance> Enemies => _context.Targets;

        /// <summary>
        /// Third party (neutral) units.
        /// </summary>
        public IReadOnlyList<CharacterInstance> ThirdParty => _context.ThirdParty;

        #endregion

        #region Available Actions

        /// <summary>
        /// Skills available to the AI unit.
        /// </summary>
        public IReadOnlyList<Skill> AvailableSkills => _context.ActiveSkills;

        /// <summary>
        /// Gets how many times a skill has been used this battle.
        /// </summary>
        public int GetSkillUseCount(Skill skill) =>
            _context.SkillUseCount.TryGetValue(skill, out int count) ? count : 0;

        #endregion

        #region Environment

        /// <summary>
        /// Current environmental conditions.
        /// </summary>
        public Environment.EnvironmentalConditions Environment => _context.EnvironmentalConditions;

        #endregion

        #region Adjacency

        /// <summary>
        /// Units adjacent to the AI unit.
        /// </summary>
        public Adjacency AdjacentUnits => _context.AdjacentUnits;

        #endregion

        #region Decision Helpers

        /// <summary>
        /// Gets an AI-specific decision cache value.
        /// Use for storing intermediate AI calculations.
        /// </summary>
        public T GetDecisionData<T>(string key, T defaultValue = default) =>
            _context.GetCustomData($"ai_{key}", defaultValue);

        /// <summary>
        /// Sets an AI-specific decision cache value.
        /// Use for caching intermediate AI calculations within a single decision.
        /// </summary>
        public void SetDecisionData(string key, object value) =>
            _context.SetCustomData($"ai_{key}", value);

        /// <summary>
        /// Clears all AI decision cache data.
        /// Call this at the start of each AI decision phase.
        /// </summary>
        public void ClearDecisionCache()
        {
            var keysToRemove = new List<string>();
            foreach (var key in _context.CustomData.Keys)
            {
                if (key.StartsWith("ai_"))
                {
                    keysToRemove.Add(key);
                }
            }
            foreach (var key in keysToRemove)
            {
                _context.CustomData.Remove(key);
            }
        }

        #endregion

        /// <summary>
        /// Gets the underlying BattleContext for cases where full access is needed.
        /// </summary>
        public BattleContext GetFullContext() => _context;
    }
}
