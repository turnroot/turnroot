using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components.Battle;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Environment;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.NPCs;
using Turnroot.Gameplay.Combat.PreBattle;
using Turnroot.Gameplay.Combat.Precompute;
using Turnroot.Gameplay.Maps;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat
{
    /// <summary>
    /// Defines the various ways a battle can be concluded.
    /// </summary>
    public enum BattleExitType
    {
        Victory,
        Defeat,
        Retreat,
        Bookmark,
    }

    /// <summary>
    /// Main battle object managing combat state, teams, rosters, battle conditions, and the map grid.
    /// </summary>
    [RequireComponent(typeof(EnvironmentalConditions))]
    [RequireComponent(typeof(BattleContext))]
    [RequireComponent(typeof(EnemySupervisor))]
    [RequireComponent(typeof(BattlePreparationObject))]
    [RequireComponent(typeof(TileHighlighter))]
    [RequireComponent(typeof(TerrainTypeOverlay))]
    [RequireComponent(typeof(BattlePrecomputeLoader))]
    public partial class BattleGameObject : MonoBehaviour
    {
        public EnemySupervisor EnemySupervisor => GetComponent<EnemySupervisor>();
        [field: SerializeField, HideInInspector]
        public BattleContext Context { get; private set; }

        [field: Header("Battle Teams"), HorizontalLine(color: EColor.Indigo)]
        [field: SerializeField]
        public bool HasThirdParty { get; set; }

        [ShowIf(nameof(HasThirdParty))]
        public bool ThirdPartyFightsAllies;

        [ShowIf(nameof(HasThirdParty))]
        public bool ThirdPartyFightsEnemies;

        [Range(1, 16)]
        public int MaxPlayerTeamUnits;

        [SerializeField]
        private List<CharacterData> _requiredPlayerUnits = new();

        public List<CharacterData> RequiredPlayerUnits
        {
            get => _requiredPlayerUnits;
            set => _requiredPlayerUnits = value;
        }

        public EnvironmentalConditions EnvironmentalConditions =>
            GetComponent<EnvironmentalConditions>();

        [SerializeField, SerializeReference]
        private BattleCondition[] _battleConditions;

        public BattleCondition[] BattleConditions
        {
            get => _battleConditions;
            private set => _battleConditions = value;
        }

        [field: SerializeField, HideInInspector]
        public MapGrid MapGrid { get; private set; }

        private TileHighlighter _tileHighlighter;
        public TileHighlighter TileHighlighter
        {
            get
            {
                if (_tileHighlighter == null)
                {
                    _tileHighlighter = GetComponent<TileHighlighter>();
                }
                return _tileHighlighter;
            }
        }

        private TerrainTypeOverlay _terrainTypeOverlay;
        public TerrainTypeOverlay TerrainTypeOverlay
        {
            get
            {
                if (_terrainTypeOverlay == null)
                {
                    _terrainTypeOverlay = GetComponent<TerrainTypeOverlay>();
                }
                return _terrainTypeOverlay;
            }
        }

        [Header("Roster Templates"), HorizontalLine(color: EColor.Blue)]
        [SerializeField]
        private GenericRoster _thirdPartyRoster;

        [SerializeField, HideInInspector]
        private int _currentTurnCount;
        public int CurrentTurnCount => _currentTurnCount;

        [field: HideInInspector]
        public Brain.Brain Brain { get; set; }

        public PlayerTeamRosterInstance PlayerTeamRoster { get; private set; }
        public GenericRosterInstance ThirdPartyTeamRoster { get; private set; }

        public LayerMask GroundLayerMask;

        public void PublishMoveAnimationCompleted(CharacterInstance unit) =>
            Brain.PublishMoveAnimationCompleted(unit);

        private bool _isConnectedToBrain;

        #region Unity Lifecycle

        public void Awake()
        {
            ResetTurnCount();
            Context ??= GetComponent<BattleContext>();
            MapGrid ??= GetComponentInChildren<MapGrid>();
            _tileHighlighter ??= GetComponent<TileHighlighter>();

            ValidateRequiredComponents();
        }

        public OperationResult ValidateRequiredComponents()
        {
            return MapGrid == null ? OperationResult.Failure("Requires MapGrid to be set")
                : BattleConditions == null
                    ? OperationResult.Failure("Requires BattleConditions to be set")
                : OperationResult.Successful();
        }

#if UNITY_EDITOR
        private void Reset()
        {
            MapGrid = GetComponentInChildren<MapGrid>();
            if (MapGrid != null)
            {
                return;
            }

            var mapGridObject = new GameObject("MapGrid");
            UnityEditor.Undo.RegisterCreatedObjectUndo(mapGridObject, "Create MapGrid");
            mapGridObject.transform.SetParent(transform, false);
            MapGrid = mapGridObject.AddComponent<MapGrid>();
        }

        private void OnValidate()
        {
            Context ??= GetComponent<BattleContext>();
            MapGrid ??= GetComponentInChildren<MapGrid>();
            if (BattleConditions == null)
            {
                return;
            }

            foreach (var condition in BattleConditions)
            {
                if (condition == null)
                {
                    continue;
                }

                condition.battleContext = Context;

                condition.ResolveRequiredConditions(BattleConditions);

                if (condition is ConditionalGroupBattleCondition group)
                {
                    group.ResolveChildConditions(BattleConditions);
                }
            }
        }
#endif

        #endregion

        #region Event Handlers

        private void HandleTurnEnded()
        {
            IncrementTurnCount();

            foreach (var condition in BattleConditions)
            {
                if (condition is SurviveTurnsBattleCondition surviveTurns)
                {
                    surviveTurns.OnTurnEnd();
                }
                else if (condition is TimeLimitBattleCondition timeLimit)
                {
                    timeLimit.OnTurnEnd();
                }
            }
        }

        private void HandleAllyDamaged(CharacterInstance unit, int damage)
        {
            foreach (
                var condition in BattleConditions.OfType<LimitTotalAllyDamageBattleCondition>()
            )
            {
                condition.OnAllyDamaged(damage);
            }
        }

        private void HandleEnemyDamaged(CharacterInstance unit, int damage)
        {
            foreach (
                var condition in BattleConditions.OfType<DealMinimumTotalEnemyDamageBattleCondition>()
            )
            {
                condition.OnEnemyDamaged(damage);
            }
        }

        private void HandleUnitDefeated(CharacterInstance unit)
        {
            InvalidateAllConditionCaches();
            ClearAICache();

            CheckDefeatConditions();
        }

        private void CheckDefeatConditions()
        {
            foreach (var condition in BattleConditions)
            {
                if (condition is DefeatAllEnemiesBattleCondition defeatAll)
                {
                    defeatAll.CheckCondition();
                }
                else if (condition is DefeatEnemyBattleCondition defeatEnemy)
                {
                    defeatEnemy.CheckCondition();
                }
                else if (condition is ProtectNPCsBattleCondition protectNPCs)
                {
                    protectNPCs.CheckCondition();
                }
            }
        }

        private void HandleUnitMoved(CharacterInstance unit, Vector2Int newPos)
        {
            CheckMovementConditions(unit, newPos);
            ClearAICache();
        }

        private void CheckMovementConditions(CharacterInstance unit, Vector2Int pos)
        {
            foreach (var condition in BattleConditions)
            {
                if (condition is SurviveUntilAllyReachesTileBattleCondition reachTile)
                {
                    reachTile.CheckCondition();
                }

                if (condition is ReachTilesBattleCondition reachTiles)
                {
                    bool isPlayerUnit = PlayerTeamRoster?.Instances?.Contains(unit) ?? false;
                    if (isPlayerUnit)
                    {
                        reachTiles.OnUnitReachedTile(pos);
                    }
                }
            }
        }

        private void HandleExitBattle(BattleExitType exitType) => DisconnectFromBrainEvents();

        private void HandleUnitSpawned(CharacterInstance unit, Vector2Int position)
        {
            InvalidateAllConditionCaches();
            ClearAICache();

            foreach (var condition in BattleConditions)
            {
                if (condition is DefeatEnemyBattleCondition defeatEnemy)
                {
                    defeatEnemy.CheckCondition();
                }
                else if (condition is DefeatAllEnemiesBattleCondition defeatAll)
                {
                    defeatAll.CheckCondition();
                }
            }
        }

        private OperationResult InvalidateAllConditionCaches()
        {
            foreach (var condition in BattleConditions)
            {
                try
                {
                    condition?.InvalidateCache();
                }
                catch (System.Exception ex)
                {
                    return OperationResult.Failure(
                        $"Failed to invalidate cache for condition {condition?.GetType().Name}: {ex.Message}"
                    );
                }
            }
            return OperationResult.Successful();
        }

        private void ClearAICache() => Brain.battleBrain.ClearAICache();

        private void HandleMapStateChanged() => ClearAICache();

        private void OnDestroy() => DisconnectFromBrainEvents();

        #endregion

        #region Turn Management

        public void IncrementTurnCount() => _currentTurnCount++;

        public void ResetTurnCount() => _currentTurnCount = 0;

        public int Turns() => _currentTurnCount;

        #endregion
    }
}
