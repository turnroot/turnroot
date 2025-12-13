using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Context;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Environment;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Locations;
using Turnroot.Skills.Nodes;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    /// <summary>
    /// Runtime context for the entire battle.
    /// Contains all the dynamic data that skills and other systems need at runtime.
    /// </summary>
    public class BattleContext
    {
        /// <summary>
        /// Reference to the Brain for publishing events.
        /// Set this when creating the BattleContext.
        /// </summary>
        public Brain.Brain Brain { get; set; }

        /// <summary>
        /// Active map graph for this battle.
        /// </summary>
        public MapGrid mapGrid { get; set; }

        // Currently executing skill (if any)
        public Skill CurrentSkill { get; set; }

        // All skills and their graphs that can be executed in this battle
        public List<Skill> ActiveSkills { get; set; }
        public List<SkillGraph> ActiveSkillGraphs { get; set; }

        public Dictionary<Skill, int> SkillUseCount { get; set; }
        public CharacterInstance UnitInstance { get; set; }
        public List<CharacterInstance> Targets { get; set; }
        public List<CharacterInstance> Allies { get; set; }
        public List<CharacterInstance> ThirdParty { get; set; }
        public Adjacency AdjacentUnits { get; set; }

        // Currently executing skill graph (if any)
        public SkillGraph CurrentSkillGraph { get; set; }

        public EnvironmentalConditions EnvironmentalConditions { get; set; }
        public Dictionary<string, object> CustomData { get; private set; }

        public bool IsInterrupted { get; set; }

        // Combat state flags
        public bool IsCriticalHit { get; set; }
        public CharacterInstance CriticalHitUnit { get; set; }
        public bool AnotherTurnGranted { get; set; }
        public CharacterInstance UnitTakingAnotherTurn { get; set; }

        public BattleContext()
        {
            CustomData = new Dictionary<string, object>();
            Targets = new List<CharacterInstance>();
            Allies = new List<CharacterInstance>();
            ThirdParty = new List<CharacterInstance>();
            AdjacentUnits = new Adjacency(null);
            ActiveSkills = new List<Skill>();
            ActiveSkillGraphs = new List<SkillGraph>();
            SkillUseCount = new Dictionary<Skill, int>();
        }

        // Get a custom data value, or default if not found
        public T GetCustomData<T>(string key, T defaultValue = default) =>
            CustomData.TryGetValue(key, out object value) && value is T typedValue
                ? typedValue
                : defaultValue;

        // Set a custom data value
        public void SetCustomData(string key, object value) => CustomData[key] = value;

        #region Focused Context Factories

        /// <summary>
        /// Creates a focused context for skill execution.
        /// Use this in skill nodes instead of the full BattleContext.
        /// </summary>
        public SkillExecutionContext AsSkillContext() => new SkillExecutionContext(this);

        /// <summary>
        /// Creates a focused context for combat resolution.
        /// Use this in combat calculation systems.
        /// </summary>
        public CombatContext AsCombatContext() => new CombatContext(this);

        /// <summary>
        /// Creates a focused context for AI decision-making.
        /// Use this in AI systems instead of the full BattleContext.
        /// </summary>
        public AIDecisionContext AsAIContext() => new AIDecisionContext(this);

        #endregion
    }
}
