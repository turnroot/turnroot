using System;
using System.Collections.Generic;
using Turnroot.Characters.CharacterClass;
using Turnroot.Characters.Components.Support;
using Turnroot.Characters.Stats;
using Turnroot.Characters.StatusEffects;
using Turnroot.Gameplay.Objects;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Characters
{
    // CharacterData holds static info: name, base stats, portraits, etc.
    // CharacterInstance holds runtime info: current HP, level, exp, inventory, etc.
    // Multiple characters can share the same CharacterData template but have different instances

    /// <summary>
    /// Runtime instance of a character. Core state and initialization.
    /// Functionality is split across partial class files in Components/InstanceComponents:
    /// - CharacterBattleStats.cs: Battle statistics tracking
    /// - CharacterProgression.cs: Level up, stat growth, experience ranks
    /// - CharacterStatusEffects.cs: Status effect management
    /// - CharacterSupportSystem.cs: Support relationships
    /// - CharacterClassManager.cs: Class management and requirements
    /// - CharacterSkillManager.cs: Skill management
    /// </summary>
    [Serializable]
    public partial class CharacterInstance : Serialization.IPostDeserialize, IHasStats
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

        /// <summary>
        /// Converts a <see cref="Vector2Int"/> position to a <see cref="MapGridPoint"/> using the provided <see cref="MapGrid"/>.
        /// Returns <c>null</c> if the position is out of bounds, as per the <see cref="MapGrid.GetGridPoint"/> signature.
        /// </summary>
        public MapGridPoint UnitPositionToMapGridPoint(Vector2Int unitPosition, MapGrid mapGrid) =>
            mapGrid.GetGridPoint(unitPosition.x, unitPosition.y);

        /// <summary>
        /// Moves the character to a new position on the map grid.
        /// Returns <see cref="OperationResult.SuccessResult"/> if the move is successful.
        /// Returns <see cref="OperationResult.Failure"/> with a message if the new position is out of bounds.
        /// </summary>
        /// <param name="newPosition">New position on the map grid</param>
        /// <param name="mapGrid">The map grid to validate the position against</param>
        public OperationResult MoveToPosition(Vector2Int newPosition, MapGrid mapGrid)
        {
            var gridPoint = UnitPositionToMapGridPoint(newPosition, mapGrid);
            if (gridPoint == null)
            {
                return OperationResult.Failure("New position is out of bounds");
            }

            _mapGridPosition = newPosition;
            return OperationResult.SuccessResult();
        }

        [SerializeField]
        private bool _isDefeatedInCurrentBattle = false;
        public bool IsDefeatedInCurrentBattle
        {
            get => _isDefeatedInCurrentBattle;
            set => _isDefeatedInCurrentBattle = value;
        }

        [SerializeField]
        private bool _wasSpawnedDuringBattle = false;

        /// <summary>
        /// Indicates the unit was spawned during the current battle (not part of initial roster).
        /// This flag helps snapshot/restore logic determine whether to remove reinforcements on restore.
        /// </summary>
        public bool WasSpawnedDuringBattle
        {
            get => _wasSpawnedDuringBattle;
            set => _wasSpawnedDuringBattle = value;
        }
        public bool IsSelectedForBattle { get; set; } = false;

        [SerializeField]
        private string _id;

        [SerializeField]
        private CharacterData _characterTemplate;

        [SerializeField]
        private SkinnedMeshRenderer _meshRenderer;

        /// <summary>
        /// Renderer used to display this character's model. Should be set when the character is spawned.
        /// </summary>
        public SkinnedMeshRenderer Renderer => _meshRenderer;

        /// <summary>
        /// Set the renderer for this character instance. Used when spawning models in pre-battle or battle.
        /// </summary>
        public void SetRenderer(SkinnedMeshRenderer renderer)
        {
            _meshRenderer = renderer;
        }

        #endregion

        #region Stats & Progression State

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
        private CharacterClassDataInstance _currentClass;

        // Classes this character has previously equipped (for tracking one-time bonuses)
        [SerializeField]
        private List<CharacterClassData> _equippedClassHistory = new();

        // Active status effects on this character
        [SerializeField]
        private List<StatusEffectInstance> _activeStatusEffects = new();
        public CharacterInstance LastAttackedTarget { get; set; }
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
        public CharacterClassDataInstance CurrentClass => _currentClass;

        /// <summary>
        /// Returns the template for the currently equipped class if any, otherwise falls back to the starting class template.
        /// This allows callers to query the class *template* without needing the runtime instance.
        /// </summary>
        public CharacterClassData CurrentClassTemplate =>
            _currentClass?.ClassData ?? _characterTemplate?.StartingClass;
        public IReadOnlyList<StatusEffectInstance> ActiveStatusEffects => _activeStatusEffects;

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
            {
                return Guid.NewGuid().ToString();
            }

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
            {
                return null;
            }

            if (template.IsUnique)
            {
                var existing = UniqueInstanceRegistry.Get<CharacterInstance>(template);
                if (existing != null)
                {
                    return existing;
                }
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
            _equippedClassHistory ??= new List<CharacterClassData>();

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

        private OperationResult Initialize()
        {
            // Copy initial values from template
            if (_characterTemplate == null)
            {
                return OperationResult.Failure("CharacterTemplate is null.");
            }
            _currentLevel = _characterTemplate.Level;
            _currentExp = _characterTemplate.Exp;

            // Deep copy stats from template
            _runtimeBoundedStats = CharacterHelpers.CloneBoundedStats(
                _characterTemplate.BoundedStats
            );
            _runtimeUnboundedStats = CharacterHelpers.CloneUnboundedStats(
                _characterTemplate.UnboundedStats
            );

            // Deep copy inventory from template
            _inventoryInstance = new CharacterInventoryInstance();
            if (_characterTemplate.StartingInventory != null)
            {
                foreach (var slot in _characterTemplate.StartingInventory)
                {
                    var res = _inventoryInstance.AddToInventory(new ObjectItemInstance(slot.Item));
                    if (!res.Success)
                    {
                        return res;
                    }
                }
            }

            // Deep copy support relationships from template
            _supportRelationships = CharacterHelpers.CloneSupportRelationships(
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

            // Initialize current class from template (use ChangeClass to ensure consistent behavior)
            // If a starting class is defined on the template, equip it without applying one-time change bonuses
            if (_characterTemplate.StartingClass != null)
            {
                ChangeClass(_characterTemplate.StartingClass, applyClassChangeBonuses: false);
#if UNITY_EDITOR
                Debug.Log(
                    $"Character {Id} initialized with starting class {_characterTemplate.StartingClass.Identity.ClassName}"
                );
#endif
            }
            else
            {
                // get the default class from GameplayGeneralSettings, use that

                var settings = GameSettingsLoader.LoadFirst<GameplayGeneralSettings>(
                    "GameSettings"
                );
                var defaultClass = settings?.GetDefaultStartingClass();
                // Attempt to apply default starting class, returning any failure from ChangeClass
                var changedRes =
                    defaultClass != null
                        ? ChangeClass(defaultClass, applyClassChangeBonuses: false)
                        : OperationResult.Failure(
                            $"Character {Id} has no starting class and GameplayGeneralSettings.DefaultStartingClass is not set."
                        );

                if (!changedRes.Success)
                {
                    return changedRes;
                }

#if UNITY_EDITOR
                Debug.Log(
                    $"Character {Id} initialized with default starting class {defaultClass.Identity.ClassName}"
                );
#endif
            }

            return OperationResult.SuccessResult();
        }

        /// <summary>
        /// Helper method to add skill instances from a list of skill templates.
        /// </summary>
        private void InitializeSkillsFromTemplates(List<Skill> skillTemplates)
        {
            if (skillTemplates == null)
            {
                return;
            }

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
        public BoundedCharacterStat GetBoundedStat(BoundedStatType type) =>
            StatHelpers.GetBoundedStat(_runtimeBoundedStats, type);

        /// <summary>
        /// Get an unbounded stat by type (Strength, Speed, etc).
        /// </summary>
        public CharacterStat GetUnboundedStat(UnboundedStatType type) =>
            StatHelpers.GetUnboundedStat(_runtimeUnboundedStats, type);

        public float GetHealthPercentage() => StatHelpers.GetHealthPercentage(this.BoundedStats);

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
        private CommonAncestors.LeveledLetteredField _rank;

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
        public void AddExperience(int amount) => _experiencePoints += amount; // TODO: Implement rank progression based on experience thresholds

        /// <summary>
        /// Set the rank to a specific letter grade.
        /// </summary>
        public void SetRank(string rankLetter) =>
            _rank = new CommonAncestors.LeveledLetteredField(rankLetter);
    }
}
