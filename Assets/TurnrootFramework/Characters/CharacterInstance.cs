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
using UnityEngine.Splines;

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
        private Vector2Int _mapGridPosition = new(-9999, -9999);

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

        [NonSerialized]
        private GameObject _currentWeaponPrefab;

        [NonSerialized]
        private GameObject _currentShieldPrefab;

        [NonSerialized]
        private bool _isMounted;

        [NonSerialized]
        private GameObject _currentMountModel;
        #endregion

        #region Properties
        public Vector2Int MapGridPosition
        {
            get => _mapGridPosition;
            set { _mapGridPosition = value; }
        }
        public GameObject CurrentWeaponPrefab
        {
            get => _currentWeaponPrefab;
            set => _currentWeaponPrefab = value;
        }

        public GameObject CurrentShieldPrefab
        {
            get => _currentShieldPrefab;
            set => _currentShieldPrefab = value;
        }

        public bool IsMounted
        {
            get => _isMounted;
            set => _isMounted = value;
        }

        public GameObject CurrentMountModel
        {
            get => _currentMountModel;
            set => _currentMountModel = value;
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

        public Spline CurrentMovementSpline { get; set; }
        public float WalkingSpeed { get; set; } = 3.5f;
        #endregion


        #region Map Position Management
        public MapGridPoint UnitPositionToMapGridPoint(Vector2Int unitPosition, MapGrid mapGrid) =>
            mapGrid.GetGridPoint(unitPosition.x, unitPosition.y);

        public OperationResult MoveToPosition(Vector2Int newPosition, MapGrid mapGrid)
        {
            var gridPoint = UnitPositionToMapGridPoint(newPosition, mapGrid);
            if (!ValidationHelper.ValidateNotNull(gridPoint, nameof(gridPoint), "MoveToPosition"))
            {
                return OperationResult.Failure("New position is out of bounds");
            }

            _mapGridPosition = newPosition;
            return OperationResult.Successful();
        }
        #endregion


        #region Battle Copy for Roster Decoupling

        /// <summary>
        /// Private parameterless constructor for CreateBattleCopy() only.
        /// </summary>
        private CharacterInstance() { }

        /// <summary>
        /// Creates a battle-specific copy of this instance for use in battle rosters.
        /// Shares persistent data (stats, skills, template) but has isolated battle state (position, flags).
        /// This prevents serialization of the persistent roster from corrupting active battle state.
        /// </summary>
        public CharacterInstance CreateBattleCopy()
        {
            var copy = new CharacterInstance();

            copy._id = this._id;
            copy._characterTemplate = this._characterTemplate;
            copy.settings = this.settings;

            copy._currentLevel = this._currentLevel;
            copy._currentExp = this._currentExp;
            copy._runtimeBoundedStats = this._runtimeBoundedStats;
            copy._runtimeUnboundedStats = this._runtimeUnboundedStats;
            copy._inventoryInstance = this._inventoryInstance;
            copy._skillInstances = this._skillInstances;
            copy._supportRelationships = this._supportRelationships;
            copy._experienceRanks = this._experienceRanks;
            copy._currentClass = this._currentClass;
            copy._equippedClassHistory = this._equippedClassHistory;

            copy._meshRenderer = this._meshRenderer;
            copy._useBattleModel = this._useBattleModel;
            copy._currentWeaponPrefab = this._currentWeaponPrefab;
            copy._currentShieldPrefab = this._currentShieldPrefab;
            copy._isMounted = this._isMounted;
            copy._currentMountModel = this._currentMountModel;

            // Battle-specific state (RESET for new battle)
            copy._mapGridPosition = new Vector2Int(-9999, -9999);
            copy._isDefeatedInCurrentBattle = false;
            copy._wasSpawnedDuringBattle = false;
            copy.IsSelectedForBattle = this.IsSelectedForBattle;
            copy._activeStatusEffects = new List<StatusEffectInstance>();
            copy.LastAttackedTarget = null;
            copy.CurrentMovementSpline = null;
            copy.WalkingSpeed = this.WalkingSpeed;

            return copy;
        }
        #endregion
    }
}
