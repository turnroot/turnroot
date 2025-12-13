using Turnroot.Characters;
using Turnroot.Skills.Nodes;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Context
{
    /// <summary>
    /// Focused context for skill node execution.
    /// Contains only the data that skill nodes need to execute.
    /// </summary>
    public readonly struct SkillExecutionContext
    {
        private readonly BattleContext _context;

        public SkillExecutionContext(BattleContext context)
        {
            _context = context;
        }

        #region Unit Information

        public CharacterInstance Unit => _context.UnitInstance;

        public bool HasUnit => _context.UnitInstance != null;

        #endregion

        #region Skill Information

        public Skill CurrentSkill => _context.CurrentSkill;

        public SkillGraph CurrentSkillGraph => _context.CurrentSkillGraph;

        public bool IsInterrupted => _context.IsInterrupted;

        public void Interrupt() => _context.IsInterrupted = true;

        #endregion

        #region Targets

        public System.Collections.Generic.IReadOnlyList<CharacterInstance> Targets =>
            _context.Targets;

        public bool HasTargets => _context.Targets != null && _context.Targets.Count > 0;

        public CharacterInstance PrimaryTarget => HasTargets ? _context.Targets[0] : null;

        #endregion

        #region Allies
        public System.Collections.Generic.IReadOnlyList<CharacterInstance> Allies =>
            _context.Allies;

        public bool HasAllies => _context.Allies != null && _context.Allies.Count > 0;

        #endregion

        #region Custom Data

        public T GetCustomData<T>(string key, T defaultValue = default) =>
            _context.GetCustomData(key, defaultValue);

        public void SetCustomData(string key, object value) => _context.SetCustomData(key, value);

        #endregion

        #region Combat State

        public bool IsCriticalHit => _context.IsCriticalHit;

        public void SetCriticalHit(CharacterInstance unit)
        {
            _context.IsCriticalHit = true;
            _context.CriticalHitUnit = unit;
        }

        public bool AnotherTurnGranted => _context.AnotherTurnGranted;

        public void GrantAnotherTurn(CharacterInstance unit)
        {
            _context.AnotherTurnGranted = true;
            _context.UnitTakingAnotherTurn = unit;
        }

        #endregion

        #region Environment

        public Environment.EnvironmentalConditions EnvironmentalConditions =>
            _context.EnvironmentalConditions;

        #endregion

        #region Skill Use Tracking

        public int GetSkillUseCount(Skill skill) =>
            _context.SkillUseCount.TryGetValue(skill, out int count) ? count : 0;

        public void IncrementSkillUseCount(Skill skill)
        {
            if (!_context.SkillUseCount.ContainsKey(skill))
            {
                _context.SkillUseCount[skill] = 0;
            }
            _context.SkillUseCount[skill]++;
        }

        #endregion
        public BattleContext GetFullContext() => _context;
    }
}
