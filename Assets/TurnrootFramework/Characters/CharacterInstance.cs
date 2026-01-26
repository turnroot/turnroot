using System;
using System.Collections.Generic;
using Turnroot.Characters.CharacterClass;
using Turnroot.Characters.Components;
using Turnroot.Characters.Components.Support;
using Turnroot.Characters.Stats;
using Turnroot.Characters.StatusEffects;
using Turnroot.Gameplay.Maps;
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
    }
}
