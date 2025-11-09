using System;
using System.Collections.Generic;
using Turnroot.Characters.Components.Support;
using Turnroot.Characters.Stats;
using Turnroot.Characters.Subclasses;
using Turnroot.Gameplay.Objects;
using UnityEngine;

namespace Turnroot.Characters
{
    // CharacterData holds static info: name, base stats, portraits, etc.
    // CharacterInstance holds runtime info: current HP, level, exp, inventory, etc.
    // Multiple characters can share the same CharacterData template but have different instances

    [Serializable]
    public class CharacterInstance
    {
        [SerializeField]
        private readonly string _id;

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

        // Runtime Inventory
        [SerializeField]
        private CharacterInventoryInstance _inventoryInstance;

        // Runtime Skills
        [SerializeField]
        private List<SkillInstance> _skillInstances = new List<SkillInstance>();

        // Support Relationships (runtime)
        [SerializeField]
        private List<SupportRelationshipInstance> _supportRelationships =
            new List<SupportRelationshipInstance>();

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

            // Deep copy inventory from template
            _inventoryInstance = new CharacterInventoryInstance();
            if (_characterTemplate.StartingInventory != null)
            {
                foreach (var slot in _characterTemplate.StartingInventory)
                {
                    for (int i = 0; i < slot.Quantity; i++)
                    {
                        _inventoryInstance.AddToInventory(new ObjectItemInstance(slot.Item));
                    }
                }
            }

            // Deep copy support relationships from template
            _supportRelationships = new List<SupportRelationshipInstance>();
            if (_characterTemplate.SupportRelationships != null)
            {
                foreach (var relTemplate in _characterTemplate.SupportRelationships)
                {
                    _supportRelationships.Add(new SupportRelationshipInstance(relTemplate));
                }
            }

            // Initialize skills from template
            _skillInstances = new List<SkillInstance>();
            InitializeSkillsFromTemplates(_characterTemplate.Skills);
            InitializeSkillsFromTemplates(_characterTemplate.SpecialSkills);
        }

        /// <summary>
        /// Helper method to add skill instances from a list of skill templates.
        /// </summary>
        private void InitializeSkillsFromTemplates(List<Skill> skillTemplates)
        {
            if (skillTemplates == null)
                return;

            foreach (var skillTemplate in skillTemplates)
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
            return StatHelpers.GetBoundedStat(_runtimeBoundedStats, type);
        }

        public CharacterStat GetUnboundedStat(UnboundedStatType type)
        {
            return StatHelpers.GetUnboundedStat(_runtimeUnboundedStats, type);
        }

        public void LevelUp()
        {
            _currentLevel++;
            // TODO: Apply stat growths
        }

        public SupportRelationshipInstance GetSupportRelationship(CharacterData character)
        {
            return _supportRelationships.Find(s => s.Character == character);
        }

        public void AddSupportRelationship(SupportRelationship template)
        {
            // Check if relationship already exists
            if (GetSupportRelationship(template.Character) == null)
            {
                _supportRelationships.Add(new SupportRelationshipInstance(template));
            }
        }

        public void IncreaseSupport(CharacterData character, int amount)
        {
            var relationship = GetSupportRelationship(character);
            if (relationship != null)
            {
                relationship.Increase(amount);
            }
            else
            {
                Debug.LogWarning($"No support relationship found with {character.FullName}");
                AddSupportRelationship(new SupportRelationship { Character = character });
                GetSupportRelationship(character)?.Increase(amount);
            }
        }

        public void RemoveSupportRelationship(CharacterData character)
        {
            _ = _supportRelationships.RemoveAll(s => s.Character == character);
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
