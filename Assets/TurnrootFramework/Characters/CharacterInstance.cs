using System;
using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters.CharacterClass;
using Turnroot.Characters.Components;
using Turnroot.Characters.Components.Support;
using Turnroot.Characters.Stats;
using Turnroot.Characters.StatusEffects;
using Turnroot.Gameplay.Maps;
using Turnroot.Gameplay.Objects;
using Turnroot.GameSettings;
using Turnroot.Skills;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Characters
{
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
        #region Serialized Fields
        [SerializeField]
        private Vector2Int _mapGridPosition;

        [SerializeField]
        private bool _isDefeatedInCurrentBattle = false;

        [SerializeField]
        private bool _wasSpawnedDuringBattle = false;

        [SerializeField]
        private string _id;

        [SerializeField]
        private CharacterData _characterTemplate;

        [SerializeField]
        private SkinnedMeshRenderer _meshRenderer;

        [SerializeField]
        private bool _useBattleModel = true;

        [SerializeField]
        private int _currentLevel = 1;

        [SerializeField]
        private int _currentExp = 0;

        [SerializeField]
        private List<BoundedCharacterStat> _runtimeBoundedStats = new();

        [SerializeField]
        private List<CharacterStat> _runtimeUnboundedStats = new();

        [SerializeField]
        private CharacterInventoryInstance _inventoryInstance;

        [SerializeField]
        private List<SkillInstance> _skillInstances = new();

        [SerializeField]
        private List<SupportRelationshipInstance> _supportRelationships = new();

        [SerializeField]
        private List<ExperienceRankInstance> _experienceRanks = new();

        [SerializeField]
        private CharacterClassDataInstance _currentClass;

        [SerializeField]
        private List<CharacterClassData> _equippedClassHistory = new();

        [SerializeField]
        private List<StatusEffectInstance> _activeStatusEffects = new();
        #endregion

        #region Properties
        public Vector2Int MapGridPosition
        {
            get => _mapGridPosition;
            set => _mapGridPosition = value;
        }
        public bool IsDefeatedInCurrentBattle
        {
            get => _isDefeatedInCurrentBattle;
            set => _isDefeatedInCurrentBattle = value;
        }
        public bool WasSpawnedDuringBattle
        {
            get => _wasSpawnedDuringBattle;
            set => _wasSpawnedDuringBattle = value;
        }
        public bool IsSelectedForBattle { get; set; } = false;
        public bool UseBattleModel => _useBattleModel;
        public SkinnedMeshRenderer Renderer => _meshRenderer;
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
        public CharacterClassData CurrentClassTemplate =>
            _currentClass?.ClassData ?? _characterTemplate?.StartingClass;
        public IReadOnlyList<StatusEffectInstance> ActiveStatusEffects => _activeStatusEffects;
        public List<BoundedCharacterStat> BoundedStats => _runtimeBoundedStats;
        public List<CharacterStat> UnboundedStats => _runtimeUnboundedStats;
        public CharacterInstance LastAttackedTarget { get; set; }
        #endregion

        #region Initialization
        internal CharacterInstance(CharacterData template, bool useBattleModel = true)
        {
            _characterTemplate = template;
            _id = GenerateId(template);
            _useBattleModel = useBattleModel;
            Initialize();
        }

        private static string GenerateId(CharacterData template)
        {
            if (template == null)
            {
                return Guid.NewGuid().ToString();
            }

            return template.IsUnique ? $"unique_{template.name}" : Guid.NewGuid().ToString();
        }

        public static CharacterInstance Create(CharacterData template, bool useBattleModel = true)
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

            var instance = new CharacterInstance(template, useBattleModel);
            if (template.IsUnique)
            {
                UniqueInstanceRegistry.Register(template, instance);
            }

            return instance;
        }

        private OperationResult Initialize()
        {
            if (_characterTemplate == null)
            {
                return OperationResult.Failure("CharacterTemplate is null.");
            }

            _currentLevel = _characterTemplate.Level;
            _currentExp = _characterTemplate.Exp;

            _runtimeBoundedStats = CharacterHelpers.CloneBoundedStats(
                _characterTemplate.BoundedStats
            );
            _runtimeUnboundedStats = CharacterHelpers.CloneUnboundedStats(
                _characterTemplate.UnboundedStats
            );

            var validation = ValidateRuntimeStatsComplete();
            if (!validation.Success)
            {
                return validation;
            }

#if UNITY_EDITOR
            ValidateStatsComplete();
#endif

            InitializeInventory();
            InitializeSupportRelationships();
            InitializeSkills();
            InitializeExperienceRanks();

            return InitializeClass();
        }

        private void InitializeInventory()
        {
            _inventoryInstance = new CharacterInventoryInstance();
            if (_characterTemplate.StartingInventory != null)
            {
                foreach (var slot in _characterTemplate.StartingInventory)
                {
                    _inventoryInstance.AddToInventory(new ObjectItemInstance(slot.Item));
                }
            }
        }

        private void InitializeSupportRelationships()
        {
            _supportRelationships = CharacterHelpers.CloneSupportRelationships(
                _characterTemplate.SupportRelationships,
                _characterTemplate
            );
        }

        private void InitializeSkills()
        {
            _skillInstances = new List<SkillInstance>();
            AddSkillsFromTemplates(_characterTemplate.Skills);
            AddSkillsFromTemplates(_characterTemplate.SpecialSkills);
        }

        private void AddSkillsFromTemplates(List<Skill> skillTemplates)
        {
            if (skillTemplates == null)
            {
                return;
            }

            foreach (var skill in skillTemplates.Where(s => s != null))
            {
                _skillInstances.Add(new SkillInstance(skill));
            }
        }

        private void InitializeExperienceRanks()
        {
            _experienceRanks = new List<ExperienceRankInstance>();
            if (_characterTemplate.ExperienceRanks != null)
            {
                foreach (
                    var expRank in _characterTemplate.ExperienceRanks.Where(e =>
                        e != null && !string.IsNullOrEmpty(e.ExperienceTypeId)
                    )
                )
                {
                    _experienceRanks.Add(new ExperienceRankInstance(expRank));
                }
            }
        }

        private OperationResult InitializeClass()
        {
            var classToApply = _characterTemplate.StartingClass ?? GetDefaultStartingClass();
            if (classToApply == null)
            {
                return OperationResult.Failure(
                    $"Character {Id} has no starting class and GameplayGeneralSettings.DefaultStartingClass is not set."
                );
            }

            var result = ChangeClass(classToApply, applyClassChangeBonuses: false);
            if (result.Success)
            {
                TurnrootLogger.Log(
                    $"Character {Id} initialized with starting class {classToApply.Identity.ClassName}",
                    TurnrootLogger.LogLevel.Info
                );
            }
            return result;
        }

        private CharacterClassData GetDefaultStartingClass()
        {
            var settings = GameSettingsLoader.LoadFirst<GameplayGeneralSettings>("GameSettings");
            return settings?.GetDefaultStartingClass();
        }
        #endregion

        #region Deserialization
        public void OnAfterDeserialize()
        {
            EnsureListsInitialized();
            RegisterUniqueInstance();
            HandleCurrentClass();
            RepairMissingStats();
        }

        private void EnsureListsInitialized()
        {
            _runtimeBoundedStats ??= new List<BoundedCharacterStat>();
            _runtimeUnboundedStats ??= new List<CharacterStat>();
            _supportRelationships ??= new List<SupportRelationshipInstance>();
            _skillInstances ??= new List<SkillInstance>();
            _inventoryInstance ??= new CharacterInventoryInstance();
            _experienceRanks ??= new List<ExperienceRankInstance>();
            _equippedClassHistory ??= new List<CharacterClassData>();
        }

        private void RegisterUniqueInstance()
        {
            if (_characterTemplate != null && _characterTemplate.IsUnique)
            {
                UniqueInstanceRegistry.Register(_characterTemplate, this);
            }
        }

        private void HandleCurrentClass()
        {
            if (_currentClass != null)
            {
                _currentClass.OnAfterDeserialize();
            }
            else if (_characterTemplate != null && _characterTemplate.StartingClass == null)
            {
                var defaultClass = GetDefaultStartingClass();
                if (defaultClass != null)
                {
                    var res = ChangeClass(defaultClass, applyClassChangeBonuses: false);
                    var logLevel = res.Success
                        ? TurnrootLogger.LogLevel.Info
                        : TurnrootLogger.LogLevel.Warning;
                    var message = res.Success
                        ? $"Character {Id} assigned default starting class {defaultClass.Identity.ClassName} after recall."
                        : $"CharacterInstance.OnAfterDeserialize: Failed to apply default starting class for {Id}: {res.ErrorMessage}";
                    TurnrootLogger.Log(message, logLevel);
                }
            }
        }
        #endregion

        #region Stat Validation
        private OperationResult ValidateRuntimeStatsComplete()
        {
            var res = ValidateStatsFor<BoundedStatType, BoundedCharacterStat>(
                _runtimeBoundedStats,
                s => s.StatType,
                "bounded"
            );
            return res.Success
                ? ValidateStatsFor<UnboundedStatType, CharacterStat>(
                    _runtimeUnboundedStats,
                    s => s.StatType,
                    "unbounded"
                )
                : res;
        }

        private OperationResult ValidateStatsFor<TEnum, TStat>(
            IEnumerable<TStat> runtimeStats,
            Func<TStat, TEnum> getStatType,
            string statKind
        )
            where TEnum : Enum
        {
            var required = Enum.GetValues(typeof(TEnum)).Cast<TEnum>().ToHashSet();
            var existing = new HashSet<TEnum>();

            foreach (var stat in runtimeStats)
            {
                if (stat == null)
                {
                    return OperationResult.Failure(
                        $"CharacterInstance.ValidateRuntimeStatsComplete: null {statKind} stat found for {Id}"
                    );
                }

                var type = getStatType(stat);
                if (!existing.Add(type))
                {
                    return OperationResult.Failure(
                        $"CharacterInstance.ValidateRuntimeStatsComplete: duplicate {statKind} stat {type} for {Id}"
                    );
                }
            }

            var missing = required.Except(existing).ToList();
            if (missing.Any())
            {
                return OperationResult.Failure(
                    $"CharacterInstance.ValidateRuntimeStatsComplete: missing {statKind} stats {string.Join(", ", missing)} for {Id}"
                );
            }

            return OperationResult.Successful();
        }

        private void RepairMissingStats()
        {
            bool anyRepaired = false;

            RepairStatsFor(
                Enum.GetValues(typeof(BoundedStatType)).Cast<BoundedStatType>(),
                _runtimeBoundedStats,
                (list, type) => StatHelpers.GetBoundedStat(list, type),
                (list, type) => StatHelpers.GetOrCreateBoundedStat(list, type),
                ref anyRepaired
            );

            RepairStatsFor(
                Enum.GetValues(typeof(UnboundedStatType)).Cast<UnboundedStatType>(),
                _runtimeUnboundedStats,
                (list, type) => StatHelpers.GetUnboundedStat(list, type),
                (list, type) => StatHelpers.GetOrCreateUnboundedStat(list, type),
                ref anyRepaired
            );

            if (anyRepaired)
            {
                TurnrootLogger.Log(
                    $"CharacterInstance.RepairMissingStats: Repaired stats for {Id} - this indicates save data from an older version",
                    TurnrootLogger.LogLevel.Warning
                );
            }
        }

        private static void RepairStatsFor<TEnum, TStat>(
            IEnumerable<TEnum> enumValues,
            List<TStat> list,
            Func<List<TStat>, TEnum, TStat> getFunc,
            Action<List<TStat>, TEnum> getOrCreateFunc,
            ref bool anyRepaired
        )
            where TEnum : Enum
        {
            foreach (var type in enumValues)
            {
                if (getFunc(list, type) == null)
                {
                    getOrCreateFunc(list, type);
                    anyRepaired = true;
                }
            }
        }

#if UNITY_EDITOR
        private void ValidateStatsComplete()
        {
            bool hasErrors = Enum.GetValues(typeof(BoundedStatType))
                .Cast<BoundedStatType>()
                .Any(type => StatHelpers.GetBoundedStat(_runtimeBoundedStats, type) == null);

            hasErrors |= Enum.GetValues(typeof(UnboundedStatType))
                .Cast<UnboundedStatType>()
                .Any(type => StatHelpers.GetUnboundedStat(_runtimeUnboundedStats, type) == null);

            if (hasErrors)
            {
                TurnrootLogger.Log(
                    $"Character {Id} has missing stats - this will cause runtime errors! Use the DefaultCharacterStats asset or manually add missing stats to the template.",
                    TurnrootLogger.LogLevel.Error
                );
            }
        }
#endif
        #endregion

        #region Map Position Management
        public MapGridPoint UnitPositionToMapGridPoint(Vector2Int unitPosition, MapGrid mapGrid) =>
            mapGrid.GetGridPoint(unitPosition.x, unitPosition.y);

        public OperationResult MoveToPosition(Vector2Int newPosition, MapGrid mapGrid)
        {
            var gridPoint = UnitPositionToMapGridPoint(newPosition, mapGrid);
            if (gridPoint == null)
            {
                return OperationResult.Failure("New position is out of bounds");
            }

            _mapGridPosition = newPosition;
            return OperationResult.Successful();
        }
        #endregion

        #region Stat Access
        public void SetRenderer(SkinnedMeshRenderer renderer) => _meshRenderer = renderer;

        public BoundedCharacterStat GetBoundedStat(BoundedStatType type) =>
            StatHelpers.GetBoundedStat(_runtimeBoundedStats, type);

        public CharacterStat GetUnboundedStat(UnboundedStatType type) =>
            StatHelpers.GetUnboundedStat(_runtimeUnboundedStats, type);

        public float GetHealthPercentage() => StatHelpers.GetHealthPercentage(this.BoundedStats);
        #endregion
    }
}
