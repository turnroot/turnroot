using System;
using System.Collections.Generic;
using Turnroot.Characters.CharacterClass;
using Turnroot.Characters.Components.Support;
using Turnroot.Characters.Stats;
using Turnroot.Characters.Subclasses;
using Turnroot.Gameplay.Objects;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace Turnroot.Characters
{
    // CharacterData holds static info: name, base stats, portraits, etc.
    // CharacterInstance holds runtime info: current HP, level, exp, inventory, etc.
    // Multiple characters can share the same CharacterData template but have different instances

    [Serializable]
    public class CharacterInstance : Serialization.IPostDeserialize, Stats.IHasStats
    {
        #region Instance State

        /* ---------------------- Instance specific information --------------------- */
        [SerializeField]
        private Vector2Int _mapGridPosition;

        public Vector2Int MapGridPosition
        {
            get => _mapGridPosition;
            set => _mapGridPosition = value;
        }

        [SerializeField]
        private bool _isDefeatedInCurrentBattle = false;
        public bool IsDefeatedInCurrentBattle
        {
            get => _isDefeatedInCurrentBattle;
            set => _isDefeatedInCurrentBattle = value;
        }

        [SerializeField]
        private string _id;

        [SerializeField]
        private CharacterData _characterTemplate;

        #endregion

        #region Battle Statistics

        // Persistent stats (saved to LTM for unique characters)
        [SerializeField]
        private int _totalKills = 0;

        [SerializeField]
        private int _totalBattles = 0;

        // Transient stats (reset each battle, not serialized)
        [System.NonSerialized]
        private int _turnsAliveThisBattle = 0;

        [System.NonSerialized]
        private int _combatsThisTurn = 0;

        public int TotalKills => _totalKills;
        public int TotalBattles => _totalBattles;
        public int TurnsAliveThisBattle => _turnsAliveThisBattle;
        public int CombatsThisTurn => _combatsThisTurn;

        internal void RecordKill() => _totalKills++;

        public void RecordBattleStart()
        {
            _totalBattles++;
            _turnsAliveThisBattle = 0;
        }

        public void IncrementTurnsAlive() => _turnsAliveThisBattle++;

        public void IncrementCombatCount() => _combatsThisTurn++;

        public void ResetTurnStats() => _combatsThisTurn = 0;

        public void ResetBattleStats()
        {
            _turnsAliveThisBattle = 0;
            _combatsThisTurn = 0;
        }

        #endregion

        #region Stats & Progression

        // Runtime Stats & Progression
        [SerializeField]
        private int _currentLevel = 1;

        [SerializeField]
        private int _currentExp = 0;

        [SerializeField]
        private List<BoundedCharacterStat> _runtimeBoundedStats = new();

        [SerializeField]
        private List<CharacterStat> _runtimeUnboundedStats = new();

        // Runtime Inventory
        [SerializeField]
        private CharacterInventoryInstance _inventoryInstance;

        // Runtime Skills
        [SerializeField]
        private List<SkillInstance> _skillInstances = new();

        // Support Relationships (runtime)
        [SerializeField]
        private List<SupportRelationshipInstance> _supportRelationships = new();

        // Experience/Aptitude Ranks (runtime)
        [SerializeField]
        private List<ExperienceRankInstance> _experienceRanks = new();

        // Current Class (runtime)
        [SerializeField]
        private CharacterClass.CharacterClassDataInstance _currentClass;

        // Classes this character has previously equipped (for tracking one-time bonuses)
        [SerializeField]
        private List<CharacterClass.CharacterClassData> _equippedClassHistory = new();

        #endregion

        #region Properties

        public string Id => _id;
        public CharacterData CharacterTemplate => _characterTemplate;
        public int CurrentLevel => _currentLevel;
        public int CurrentExp => _currentExp;
        public List<BoundedCharacterStat> RuntimeBoundedStats => _runtimeBoundedStats;
        public List<CharacterStat> RuntimeUnboundedStats => _runtimeUnboundedStats;
        public CharacterInventoryInstance InventoryInstance => _inventoryInstance;
        public List<SkillInstance> SkillInstances => _skillInstances;
        public List<ExperienceRankInstance> ExperienceRanks => _experienceRanks;
        public CharacterClass.CharacterClassDataInstance CurrentClass => _currentClass;

        // IHasStats implementation - expose runtime stats
        public List<BoundedCharacterStat> BoundedStats => _runtimeBoundedStats;
        public List<CharacterStat> UnboundedStats => _runtimeUnboundedStats;

        #endregion

        #region Initialization

        /// <summary>
        /// Creates a new CharacterInstance from a template.
        /// Enforces uniqueness for templates marked IsUnique.
        /// </summary>
        // Make constructor non-public to encourage using CharacterInstance.Create which
        // enforces uniqueness for templates with IsUnique.
        internal CharacterInstance(CharacterData template)
        {
            _characterTemplate = template;
            _id = GenerateId(template);
            Initialize();
        }

        /// <summary>
        /// Generates a deterministic ID for unique characters based on their template,
        /// or a random GUID for non-unique characters.
        /// </summary>
        private static string GenerateId(CharacterData template)
        {
            if (template == null)
                return Guid.NewGuid().ToString();

            if (template.IsUnique)
            {
                // Use template asset name as deterministic ID for unique characters
                // This ensures the same unique character always gets the same ID
                return $"unique_{template.name}";
            }

            // Non-unique characters get random IDs
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Factory that enforces template uniqueness. If the template has IsUnique==true
        /// this returns the previously registered instance for that template if available.
        /// Otherwise it constructs, registers (if unique) and returns a new instance.
        /// </summary>
        public static CharacterInstance Create(CharacterData template)
        {
            if (template == null)
                return null;

            if (template.IsUnique)
            {
                var existing = UniqueInstanceRegistry.Get<CharacterInstance>(template);
                if (existing != null)
                    return existing;
            }

            var instance = new CharacterInstance(template);

            if (template.IsUnique)
            {
                UniqueInstanceRegistry.Register(template, instance);
            }

            return instance;
        }

        // Hook to allow custom initialization or repair after deserialization by a custom JSON converter.
        // The converter will call this to ensure non-null lists/structures are present.
        public void OnAfterDeserialize()
        {
            _runtimeBoundedStats ??= new List<BoundedCharacterStat>();
            _runtimeUnboundedStats ??= new List<CharacterStat>();
            _supportRelationships ??= new List<SupportRelationshipInstance>();
            _skillInstances ??= new List<SkillInstance>();
            _inventoryInstance ??= new CharacterInventoryInstance();
            _experienceRanks ??= new List<ExperienceRankInstance>();
            _equippedClassHistory ??= new List<CharacterClass.CharacterClassData>();

            // Re-register unique instances after deserialization
            if (_characterTemplate != null && _characterTemplate.IsUnique)
            {
                UniqueInstanceRegistry.Register(_characterTemplate, this);
            }

            // Re-initialize current class material if present
            if (_currentClass != null)
            {
                _currentClass.OnAfterDeserialize();
            }
        }

        private void Initialize()
        {
            // Copy initial values from template
            _currentLevel = _characterTemplate.Level;
            _currentExp = _characterTemplate.Exp;

            // Deep copy stats from template
            _runtimeBoundedStats = Turnroot.Characters.CharacterHelpers.CloneBoundedStats(
                _characterTemplate.BoundedStats
            );
            _runtimeUnboundedStats = Turnroot.Characters.CharacterHelpers.CloneUnboundedStats(
                _characterTemplate.UnboundedStats
            );

            // Deep copy inventory from template
            _inventoryInstance = new CharacterInventoryInstance();
            if (_characterTemplate.StartingInventory != null)
            {
                foreach (var slot in _characterTemplate.StartingInventory)
                {
                    _inventoryInstance.AddToInventory(new ObjectItemInstance(slot.Item));
                }
            }

            // Deep copy support relationships from template
            _supportRelationships = Turnroot.Characters.CharacterHelpers.CloneSupportRelationships(
                _characterTemplate.SupportRelationships,
                _characterTemplate
            );

            // Initialize skills from template
            _skillInstances = new List<SkillInstance>();
            InitializeSkillsFromTemplates(_characterTemplate.Skills);
            InitializeSkillsFromTemplates(_characterTemplate.SpecialSkills);

            // Initialize experience ranks from template
            _experienceRanks = new List<ExperienceRankInstance>();
            if (_characterTemplate.ExperienceRanks != null)
            {
                foreach (var expRank in _characterTemplate.ExperienceRanks)
                {
                    if (expRank != null && !string.IsNullOrEmpty(expRank.ExperienceTypeId))
                    {
                        _experienceRanks.Add(new ExperienceRankInstance(expRank));
                    }
                }
            }
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

        #endregion

        #region Stat Access

        /// <summary>
        /// Get a bounded stat by type (HP, Shields, etc).
        /// </summary>
        public BoundedCharacterStat GetBoundedStat(BoundedStatType type)
        {
            return StatHelpers.GetBoundedStat(_runtimeBoundedStats, type);
        }

        /// <summary>
        /// Get an unbounded stat by type (Strength, Speed, etc).
        /// </summary>
        public CharacterStat GetUnboundedStat(UnboundedStatType type)
        {
            return StatHelpers.GetUnboundedStat(_runtimeUnboundedStats, type);
        }

        #endregion

        #region Level Up & Growth

        /// <summary>
        /// Level up the character and apply random stat growth rolls.
        /// Internal method - use CharactersBrain.LevelUpCharacter() to publish events.
        /// </summary>
        internal void LevelUp()
        {
            _currentLevel++;
            // HP always increases by 1 on level up
            var hpStat = GetBoundedStat(BoundedStatType.Health);
            hpStat.SetCurrent(hpStat.GetCurrent() + 1f);

            if (_currentClass != null && _currentClass.ClassData != null)
            {
                var growthRates = GetEffectiveGrowthRates();

                var increasedStats = CharacterClass.StatApplicationHelper.ApplyStatGrowths(
                    growthRates,
                    new List<CharacterClass.UnboundedStatModifier>(), // Already combined in GetEffectiveGrowthRates
                    this,
                    _currentClass.ClassData.unboundedStatCaps
                );

                if (increasedStats.Count == UnboundedStats.Count)
                {
                    hpStat.SetCurrent(hpStat.GetCurrent() + 1f);
                }
            }
            else
            {
                // No class equipped - use personal growth rates only
                var personalRates =
                    _characterTemplate?.PersonalGrowthRates
                    ?? new List<CharacterClass.UnboundedStatModifier>();
                if (personalRates.Count > 0)
                {
                    var increasedStats = CharacterClass.StatApplicationHelper.ApplyStatGrowths(
                        personalRates,
                        new List<CharacterClass.UnboundedStatModifier>(),
                        this,
                        new List<CharacterClass.UnboundedStatModifier>() // No caps when classless
                    );

                    if (increasedStats.Count > 0)
                    {
                        Debug.Log(
                            $"{_characterTemplate.DisplayName} leveled up to {_currentLevel}! Stats increased: {string.Join(", ", increasedStats)}"
                        );
                    }
                    else
                    {
                        Debug.Log(
                            $"{_characterTemplate.DisplayName} leveled up to {_currentLevel}!"
                        );
                    }
                }
                else
                {
                    Debug.LogWarning(
                        $"{_characterTemplate.DisplayName} leveled up without a class or personal growth rates - no stat growth applied"
                    );
                }
            }
        }

        /// <summary>
        /// Get effective growth rates combining personal and class growth rates.
        /// Personal growth rates from CharacterData are added to class growth rate modifiers.
        /// </summary>
        private List<CharacterClass.UnboundedStatModifier> GetEffectiveGrowthRates()
        {
            var effectiveRates = new List<CharacterClass.UnboundedStatModifier>();

            // Start with personal growth rates from CharacterData
            if (_characterTemplate?.PersonalGrowthRates != null)
            {
                effectiveRates.AddRange(_characterTemplate.PersonalGrowthRates);
            }

            // Add class growth rate modifiers if we have a class
            if (_currentClass?.ClassData?.growthRateModifiers != null)
            {
                foreach (var classMod in _currentClass.ClassData.growthRateModifiers)
                {
                    int index = effectiveRates.FindIndex(e =>
                        e.unboundedStatType == classMod.unboundedStatType
                    );
                    if (index != -1)
                    {
                        // Combine with existing personal rate
                        var existing = effectiveRates[index];
                        effectiveRates[index] = new CharacterClass.UnboundedStatModifier(
                            classMod.unboundedStatType,
                            existing.value + classMod.value
                        );
                    }
                    else
                    {
                        // Add class modifier
                        effectiveRates.Add(classMod);
                    }
                }
            }

            return effectiveRates;
        }

        #endregion

        #region Support Relationships

        /// <summary>
        /// Get support relationship with a specific character.
        /// </summary>
        public SupportRelationshipInstance GetSupportRelationship(CharacterData character)
        {
            return _supportRelationships.Find(s => s.Character == character);
        }

        /// <summary>
        /// Add a new support relationship from a template.
        /// </summary>
        public void AddSupportRelationship(SupportRelationship template)
        {
            // Validate that the support relationship is not with the same character
            if (template.Character == _characterTemplate)
            {
                Debug.LogWarning(
                    $"Cannot add support relationship with the same character ({template.Character.name})"
                );
                return;
            }

            // Check if relationship already exists
            if (GetSupportRelationship(template.Character) == null)
            {
                _supportRelationships.Add(new SupportRelationshipInstance(template));
            }
        }

        /// <summary>
        /// Increase support level with another character.
        /// </summary>
        internal void IncreaseSupport(CharacterData character, int amount)
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

        /// <summary>
        /// Remove support relationship with a character.
        /// </summary>
        public void RemoveSupportRelationship(CharacterData character)
        {
            _ = _supportRelationships.RemoveAll(s => s.Character == character);
        }

        #endregion

        #region Skills

        /// <summary>
        /// Add a skill from a template.
        /// </summary>
        internal void AddSkill(Skill skillTemplate)
        {
            var skillInstance = new SkillInstance(skillTemplate);
            _skillInstances.Add(skillInstance);
        }

        /// <summary>
        /// Remove a skill instance.
        /// </summary>
        internal void RemoveSkill(SkillInstance skillInstance)
        {
            _skillInstances.Remove(skillInstance);
        }

        #endregion

        #region Experience Ranks

        /// <summary>
        /// Get experience rank by type ID (e.g., "Swords", "Magic").
        /// </summary>
        public ExperienceRankInstance GetExperienceRank(string experienceTypeId)
        {
            return _experienceRanks.Find(e => e.ExperienceTypeId == experienceTypeId);
        }

        /// <summary>
        /// Add experience to a specific experience type.
        /// </summary>
        internal void AddExperience(string experienceTypeId, int amount)
        {
            var rank = GetExperienceRank(experienceTypeId);
            if (rank != null)
            {
                rank.AddExperience(amount);
            }
            else
            {
                // Create new experience rank starting at E
                var newRank = new ExperienceRankInstance(
                    experienceTypeId,
                    Turnroot.CommonAncestors.LeveledLetteredField.E
                );
                newRank.AddExperience(amount);
                _experienceRanks.Add(newRank);
            }
        }

        /// <summary>
        /// Check if character meets an experience rank requirement.
        /// </summary>
        public bool MeetsExperienceRequirement(string experienceTypeId, string minRankLetter)
        {
            var rank = GetExperienceRank(experienceTypeId);
            if (rank == null)
                return false;

            return rank.Rank.CompareTo(minRankLetter) >= 0;
        }

        #endregion

        #region Class Management

        /// <summary>
        /// Change to a new class. Applies all class bonuses, enforces minimums/caps.
        /// Removes bonuses from old class if present.
        /// </summary>
        public bool ChangeClass(
            CharacterClass.CharacterClassData newClassData,
            MeshRenderer meshRenderer = null
        )
        {
            if (newClassData == null)
            {
                Debug.LogWarning("ChangeClass: newClassData is null");
                return false;
            }

            // Validate class requirements if needed
            // TODO: Add validation for experience requirements, level requirements, etc.

            // Remove old class bonuses
            if (_currentClass != null)
            {
                _currentClass.RemoveClassBonuses(this);
                _currentClass.Dispose();
            }

            // Check if this class has been equipped before (compare by reference, not name)
            bool isFirstTime = !_equippedClassHistory.Contains(newClassData);

            // Create new class instance
            _currentClass = new CharacterClass.CharacterClassDataInstance(
                _characterTemplate,
                newClassData,
                meshRenderer
            );

            // Initialize visual representation if mesh renderer provided
            if (meshRenderer != null)
            {
                _currentClass.Initialize();
            }

            // Apply class bonuses
            _currentClass.ApplyClassBonuses(this);

            // Apply one-time class change bonuses if first time
            if (isFirstTime)
            {
                _currentClass.ApplyClassChangeBonuses(this);
                _equippedClassHistory.Add(newClassData);
            }

            // Enforce stat minimums and caps
            _currentClass.EnforceStatMinimums(this);
            _currentClass.ApplyStatCaps(this);

            Debug.Log(
                $"{_characterTemplate.DisplayName} changed to class: {newClassData.className}"
            );
            return true;
        }

        /// <summary>
        /// Check if character meets requirements to change to a specific class.
        /// </summary>
        public bool MeetsClassRequirements(CharacterClass.CharacterClassData classData)
        {
            if (classData == null)
                return false;

            // Check level requirement
            if (_currentLevel < classData.requiredLevelToChange)
            {
                return false;
            }

            // Check class tier progression
            if (!ValidateClassTierProgression(classData))
            {
                return false;
            }

            // Check experience requirements (for requirement-based class system)
            if (classData.experienceRequirements != null)
            {
                foreach (var req in classData.experienceRequirements)
                {
                    if (!MeetsExperienceRequirement(req.experienceTypeId, req.minimumRank.Value))
                    {
                        return false;
                    }
                }
            }

            // Check species restrictions
            // TODO: Add species check when species system is implemented

            // Check pronoun restrictions
            // Note: Pronoun checking requires matching against current pronoun set
            // This is a simplified check - full implementation would need to determine
            // current pronoun type from the Pronouns object
            if (classData.allowedPronounKeys != null && classData.allowedPronounKeys.Count > 0)
            {
                // TODO: Implement proper pronoun matching when Pronouns stores key
                // For now, we allow all (empty allowedPronounKeys = allow all)
            }

            return true;
        }

        /// <summary>
        /// Validate that the target class follows proper tier progression.
        /// </summary>
        private bool ValidateClassTierProgression(CharacterClass.CharacterClassData targetClass)
        {
            // If no current class, any tier is allowed (starting class)
            if (_currentClass == null || _currentClass.ClassData == null)
            {
                return true;
            }

            var currentTier = _currentClass.ClassData.classTier;
            var targetTier = targetClass.classTier;

            // Can only advance one tier at a time (Base -> Advanced not allowed)
            // Tier regression is allowed (Advanced -> Intermediate is valid)
            if (targetTier > currentTier + 1)
            {
                Debug.LogWarning(
                    $"Cannot change from {currentTier} class to {targetTier} class - must progress one tier at a time"
                );
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check if character's current class allows a specific weapon type.
        /// </summary>
        public bool CanEquipWeaponType(Turnroot.Gameplay.Objects.Components.WeaponType weaponType)
        {
            if (_currentClass == null || _currentClass.ClassData == null)
            {
                return true; // No class restrictions
            }

            var allowedTypes = _currentClass.ClassData.allowedWeaponTypes;

            // Empty list means no restrictions (can equip anything)
            if (allowedTypes == null || allowedTypes.Count == 0)
            {
                return true;
            }

            return allowedTypes.Contains(weaponType);
        }

        /// <summary>
        /// Get available promotion paths based on current class.
        /// </summary>
        public List<CharacterClass.CharacterClassData> GetAvailablePromotions()
        {
            var available = new List<CharacterClass.CharacterClassData>();

            if (_currentClass == null || _currentClass.ClassData == null)
                return available;

            var promotionPaths = _currentClass.ClassData.promotionPaths;
            if (promotionPaths == null || promotionPaths.Count == 0)
                return available;

            foreach (var promotionClass in promotionPaths)
            {
                if (promotionClass != null && MeetsClassRequirements(promotionClass))
                {
                    available.Add(promotionClass);
                }
            }

            return available;
        }

        /// <summary>
        /// Check if character has previously equipped a specific class.
        /// </summary>
        public bool HasEquippedClass(CharacterClass.CharacterClassData classData)
        {
            if (classData == null)
                return false;
            return _equippedClassHistory.Contains(classData);
        }

        #endregion
    }

    /// <summary>
    /// Runtime instance of an experience/aptitude rank for a character.
    /// </summary>
    [Serializable]
    public class ExperienceRankInstance
    {
        [SerializeField]
        private string _experienceTypeId;

        [SerializeField]
        private Turnroot.CommonAncestors.LeveledLetteredField _rank;

        [SerializeField]
        private int _experiencePoints = 0;

        public string ExperienceTypeId => _experienceTypeId;
        public Turnroot.CommonAncestors.LeveledLetteredField Rank => _rank;
        public int ExperiencePoints => _experiencePoints;

        /// <summary>
        /// Create a new experience rank instance.
        /// </summary>
        public ExperienceRankInstance(string experienceTypeId, string rankLetter)
        {
            _experienceTypeId = experienceTypeId;
            _rank = new Turnroot.CommonAncestors.LeveledLetteredField(rankLetter);
            _experiencePoints = 0;
        }

        /// <summary>
        /// Create experience rank from a template.
        /// </summary>
        public ExperienceRankInstance(CharacterData.ExperienceRank template)
        {
            _experienceTypeId = template.ExperienceTypeId;
            _rank = new Turnroot.CommonAncestors.LeveledLetteredField(template.Rank.Value);
            _experiencePoints = 0;
        }

        /// <summary>
        /// Add experience points to this rank.
        /// </summary>
        public void AddExperience(int amount)
        {
            _experiencePoints += amount;
            // TODO: Implement rank progression based on experience thresholds
        }

        /// <summary>
        /// Set the rank to a specific letter grade.
        /// </summary>
        public void SetRank(string rankLetter)
        {
            _rank = new Turnroot.CommonAncestors.LeveledLetteredField(rankLetter);
        }
    }
}
