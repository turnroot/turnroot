using System;
using System.Collections.Generic;
using Assets.Prototypes.Characters.Stats;
using UnityEngine;

namespace Assets.Prototypes.Characters
{
    // CharacterData holds static info: name, base stats, portraits, etc.
    // CharacterInstance holds runtime info: current HP, level, exp, inventory, etc.
    // Multiple characters can share the same CharacterData template but have different instances

    [Serializable]
    public class CharacterInstance
    {
        [SerializeField]
        private string _id;

        [SerializeField]
        private CharacterData _characterTemplate;

        // Runtime Stats & Progression
        [SerializeField]
        private int _currentLevel = 1;

        [SerializeField]
        private int _currentExp = 0;

        [SerializeField]
        private List<BoundedCharacterStat> _runtimeBoundedStats = new List<BoundedCharacterStat>();

        [SerializeField]
        private List<CharacterStat> _runtimeUnboundedStats = new List<CharacterStat>();

        [SerializeField]
        private Dictionary<string, int> _classExperiences = new Dictionary<string, int>();

        // Runtime Inventory
        [SerializeField]
        private CharacterInventoryInstance _inventoryInstance;

        // Runtime Skills
        [SerializeField]
        private List<SkillInstance> _skillInstances = new List<SkillInstance>();

        public string Id => _id;
        public CharacterData CharacterTemplate => _characterTemplate;
        public int CurrentLevel => _currentLevel;
        public int CurrentExp => _currentExp;
        public List<BoundedCharacterStat> RuntimeBoundedStats => _runtimeBoundedStats;
        public List<CharacterStat> RuntimeUnboundedStats => _runtimeUnboundedStats;
        public CharacterInventoryInstance InventoryInstance => _inventoryInstance;
        public List<SkillInstance> SkillInstances => _skillInstances;

        public CharacterInstance(CharacterData template)
        {
            _id = Guid.NewGuid().ToString();
            // TODO: REPLACE THIS WITH A REAL ID
            _characterTemplate = template;
            Initialize();
        }

        private void Initialize()
        {
            // Copy initial values from template
            _currentLevel = _characterTemplate.Level;
            _currentExp = _characterTemplate.Exp;

            // Deep copy stats from template
            _runtimeBoundedStats = new List<BoundedCharacterStat>();
            foreach (var stat in _characterTemplate.BoundedStats)
            {
                _runtimeBoundedStats.Add(new BoundedCharacterStat(stat));
            }

            _runtimeUnboundedStats = new List<CharacterStat>();
            foreach (var stat in _characterTemplate.UnboundedStats)
            {
                _runtimeUnboundedStats.Add(new CharacterStat(stat));
            }

            // Copy class experiences - use the Dictionary property to access data
            _classExperiences = new Dictionary<string, int>(
                _characterTemplate.ClassExps.Dictionary
            );

            // Initialize inventory - always create a new instance
            // (no template needed, just start with default capacity)
            _inventoryInstance = new CharacterInventoryInstance(capacity: 6);

            // Initialize skills from template
            _skillInstances = new List<SkillInstance>();
            foreach (var skillTemplate in _characterTemplate.Skills)
            {
                if (skillTemplate != null)
                {
                    _skillInstances.Add(new SkillInstance(skillTemplate));
                }
            }
            foreach (var skillTemplate in _characterTemplate.SpecialSkills)
            {
                if (skillTemplate != null)
                {
                    _skillInstances.Add(new SkillInstance(skillTemplate));
                }
            }
        }

        // Stat Access Methods
        public BoundedCharacterStat GetBoundedStat(BoundedStatType type)
        {
            return _runtimeBoundedStats.Find(s => s.StatType == type);
        }

        public CharacterStat GetUnboundedStat(UnboundedStatType type)
        {
            return _runtimeUnboundedStats.Find(s => s.StatType == type);
        }

        public void LevelUp()
        {
            _currentLevel++;
            // TODO: Apply stat growths
        }

        // Skill Management
        public void AddSkill(Skill skillTemplate)
        {
            var skillInstance = new SkillInstance(skillTemplate);
            _skillInstances.Add(skillInstance);
        }

        public void RemoveSkill(SkillInstance skillInstance)
        {
            _skillInstances.Remove(skillInstance);
        }
    }
}
