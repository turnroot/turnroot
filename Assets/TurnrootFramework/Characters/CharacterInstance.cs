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
        [NonSerialized]
        private Vector2Int _mapGridPosition;

        [NonSerialized]
        private bool _isDefeatedInCurrentBattle = false;

        [NonSerialized]
        private bool _wasSpawnedDuringBattle = false;

        [NonSerialized]
        private string _id;

        [NonSerialized]
        private CharacterData _characterTemplate;

        [NonSerialized]
        private SkinnedMeshRenderer _meshRenderer;

        [NonSerialized]
        private bool _useBattleModel = true;

        [NonSerialized]
        private int _currentLevel = 1;

        [NonSerialized]
        private int _currentExp = 0;

        [NonSerialized]
        private List<BoundedCharacterStat> _runtimeBoundedStats = new();

        [NonSerialized]
        private List<CharacterStat> _runtimeUnboundedStats = new();

        [NonSerialized]
        private CharacterInventoryInstance _inventoryInstance;

        [NonSerialized]
        private List<SkillInstance> _skillInstances = new();

        [NonSerialized]
        private List<SupportRelationshipInstance> _supportRelationships = new();

        [NonSerialized]
        private List<ExperienceRankInstance> _experienceRanks = new();

        [NonSerialized]
        private CharacterClassDataInstance _currentClass;

        [NonSerialized]
        public List<CharacterClassData> _equippedClassHistory = new();

        [NonSerialized]
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
