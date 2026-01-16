using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Environment;
using Turnroot.Gameplay.Combat.FundamentalComponents.Conditions.Specific;
using Turnroot.Gameplay.Combat.PreBattle;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat
{
    public enum BattleExitType
    {
        Victory,
        Defeat,
        Retreat,
        Bookmark,
    }

    [RequireComponent(typeof(EnvironmentalConditions))]
    [RequireComponent(typeof(BattleContext))]
    [RequireComponent(typeof(BattlePreparationObject))]
    public class BattleGameObject : MonoBehaviour
    {
        [field: SerializeField, HideInInspector]
        public BattleContext Context { get; private set; }

        [Header("Battle Teams"), HorizontalLine(color: EColor.Indigo)]
        [SerializeField]
        private bool _hasThirdParty;
        public bool HasThirdParty
        {
            get => _hasThirdParty;
            set => _hasThirdParty = value;
        }

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
        public BattleCondition[] BattleConditions => _battleConditions;

        [field: SerializeField]
        public MapGrid MapGrid { get; private set; }

        [Header("Roster Templates"), HorizontalLine(color: EColor.Blue)]
        [SerializeField]
        private GenericRoster _enemyRoster;

        [SerializeField]
        private GenericRoster _thirdPartyRoster;

        [SerializeField, HideInInspector]
        private int _currentTurnCount;

        [field: HideInInspector]
        public Brain.Brain Brain { get; set; }

        public PlayerTeamRosterInstance PlayerTeamRoster { get; private set; }
        public GenericRosterInstance EnemyTeamRoster { get; private set; }
        public GenericRosterInstance ThirdPartyTeamRoster { get; private set; }

        public LayerMask GroundLayerMask;

        private bool _isConnectedToBrain;

        #region Unity Lifecycle

        public void Awake()
        {
            ResetTurnCount();
            Context ??= GetComponent<BattleContext>();
            MapGrid ??= GetComponentInChildren<MapGrid>();

            ValidateRequiredComponents();
        }

        private void ValidateRequiredComponents()
        {
            if (MapGrid == null)
            {
                Debug.LogError("BattleGameObject requires a MapGrid child");
                Debug.Break();
            }

            if (_battleConditions == null)
            {
                Debug.LogError("BattleGameObject requires BattleConditions to be set");
                Debug.Break();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Context ??= GetComponent<BattleContext>();
            if (_battleConditions == null)
            {
                return;
            }

            foreach (var condition in _battleConditions)
            {
                if (condition == null)
                {
                    continue;
                }

                condition.battleContext = Context;

                try
                {
                    condition.ResolveRequiredConditions(_battleConditions);
                    if (condition is ConditionalGroupBattleCondition group)
                    {
                        group.ResolveChildConditions(_battleConditions);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning(
                        $"Failed to resolve condition {condition?.Name}: {ex.Message}"
                    );
                }
            }
        }
#endif

        #endregion

        #region Brain Connection

        public OperationResult ConnectToBrainEvents()
        {
            if (Brain == null)
            {
                return OperationResult.Failure("Brain reference is null");
            }

            if (_isConnectedToBrain)
            {
                return OperationResult.Failure("Already connected to Brain events");
            }

            InitializeContextWithBrain();
            SubscribeToBrainEvents();

            _isConnectedToBrain = true;
            return OperationResult.SuccessResult();
        }

        private void SubscribeToBrainEvents()
        {
            Brain.OnTurnEnded += HandleTurnEnded;
            Brain.OnAllyDamaged += HandleAllyDamaged;
            Brain.OnEnemyDamaged += HandleEnemyDamaged;
            Brain.OnUnitDefeated += HandleUnitDefeated;
            Brain.OnUnitMoved += HandleUnitMoved;
            Brain.OnBattleCompleted += HandleExitBattle;

            Brain.Subscribe<UnitSpawnedEvent>(HandleUnitSpawnedEvent, EventPriority.Normal);
            Brain.Subscribe<UnitDefeatedEvent>(HandleUnitDefeatedEvent, EventPriority.Normal);
        }

        public void DisconnectFromBrainEvents()
        {
            if (Brain == null || !_isConnectedToBrain)
            {
                return;
            }

            Brain.OnTurnEnded -= HandleTurnEnded;
            Brain.OnAllyDamaged -= HandleAllyDamaged;
            Brain.OnEnemyDamaged -= HandleEnemyDamaged;
            Brain.OnUnitDefeated -= HandleUnitDefeated;
            Brain.OnUnitMoved -= HandleUnitMoved;
            Brain.OnBattleCompleted -= HandleExitBattle;

            Brain.Unsubscribe<UnitSpawnedEvent>(HandleUnitSpawnedEvent);
            Brain.Unsubscribe<UnitDefeatedEvent>(HandleUnitDefeatedEvent);

            UnsubscribeFromMapChanges();
            _isConnectedToBrain = false;
        }

        private void UnsubscribeFromMapChanges()
        {
            try
            {
                if (MapGrid != null)
                {
                    MapGrid.OnStateVersionChanged -= HandleMapStateChanged;
                }
            }
            catch { }
        }

        public OperationResult ConnectBattleConditionsToContext()
        {
            try
            {
                foreach (var condition in _battleConditions)
                {
                    condition.battleContext = Context;
                    ResolveConditionReferences(condition);
                }
                return OperationResult.SuccessResult();
            }
            catch (System.Exception ex)
            {
                return OperationResult.Failure($"Failed to connect conditions: {ex.Message}");
            }
        }

        private void ResolveConditionReferences(BattleCondition condition)
        {
            try
            {
                condition.ResolveRequiredConditions(_battleConditions);

                if (condition is ConditionalGroupBattleCondition group)
                {
                    group.ResolveChildConditions(_battleConditions);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning(
                    $"Failed to resolve references for {condition?.Name}: {ex.Message}"
                );
            }
        }

        public OperationResult AddConditionAtRuntime(BattleCondition condition)
        {
            if (condition == null)
            {
                return OperationResult.Failure("Condition is null");
            }

            try
            {
                var list = new List<BattleCondition>(
                    _battleConditions ?? System.Array.Empty<BattleCondition>()
                );
                list.Add(condition);
                _battleConditions = list.ToArray();

                condition.battleContext = Context;
                ResolveConditionReferences(condition);
                condition?.InvalidateCache();

                return OperationResult.SuccessResult();
            }
            catch (System.Exception ex)
            {
                return OperationResult.Failure($"AddConditionAtRuntime failed: {ex.Message}");
            }
        }

        private void InitializeContextWithBrain()
        {
            if (Context == null || Brain == null)
            {
                Debug.LogError("Cannot initialize context: Context or Brain is null");
                return;
            }

            try
            {
                Context.Initialize(Brain, MapGrid);
                SubscribeToMapChanges();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to initialize context: {ex.Message}");
            }
        }

        private void SubscribeToMapChanges()
        {
            try
            {
                MapGrid.OnStateVersionChanged += HandleMapStateChanged;
            }
            catch { }
        }

        #endregion

        #region Event Handlers

        private void HandleTurnEnded()
        {
            IncrementTurnCount();

            foreach (var condition in _battleConditions.OfType<SurviveTurnsBattleCondition>())
            {
                condition.OnTurnEnd();
            }

            foreach (var condition in _battleConditions.OfType<TimeLimitBattleCondition>())
            {
                condition.OnTurnEnd();
            }
        }

        private void HandleAllyDamaged(CharacterInstance unit, int damage)
        {
            foreach (
                var condition in _battleConditions.OfType<LimitTotalAllyDamageBattleCondition>()
            )
            {
                condition.OnAllyDamaged(damage);
            }
        }

        private void HandleEnemyDamaged(CharacterInstance unit, int damage)
        {
            foreach (
                var condition in _battleConditions.OfType<DealMinimumTotalEnemyDamageBattleCondition>()
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
            foreach (var condition in _battleConditions.OfType<DefeatAllEnemiesBattleCondition>())
            {
                condition.CheckCondition();
            }

            foreach (var condition in _battleConditions.OfType<DefeatEnemyBattleCondition>())
            {
                condition.CheckCondition();
            }

            foreach (var condition in _battleConditions.OfType<ProtectNPCsBattleCondition>())
            {
                condition.CheckCondition();
            }

#if TURNROOT_MONSTERS_MODULE
            foreach (var condition in _battleConditions.OfType<DefeatAllMonstersBattleCondition>())
            {
                // TODO: condition.CheckCondition();
            }

            foreach (var condition in _battleConditions.OfType<DefeatMonsterBattleCondition>())
                condition.CheckCondition();
#endif
        }

        private void HandleUnitMoved(CharacterInstance unit, Vector2Int newPos)
        {
            CheckMovementConditions(unit, newPos);
            ClearAICache();
        }

        private void CheckMovementConditions(CharacterInstance unit, Vector2Int pos)
        {
            foreach (var condition in _battleConditions)
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

        private void HandleUnitSpawnedEvent(UnitSpawnedEvent evt)
        {
            InvalidateAllConditionCaches();
            ClearAICache();

            foreach (var condition in _battleConditions.OfType<DefeatEnemyBattleCondition>())
            {
                condition.CheckCondition();
            }

            foreach (var condition in _battleConditions.OfType<DefeatAllEnemiesBattleCondition>())
            {
                condition.CheckCondition();
            }
        }

        private void HandleUnitDefeatedEvent(UnitDefeatedEvent evt) => HandleUnitDefeated(evt.Unit);

        private void InvalidateAllConditionCaches()
        {
            foreach (var condition in _battleConditions)
            {
                try
                {
                    condition?.InvalidateCache();
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning(
                        $"Failed to invalidate cache for {condition?.Name}: {ex.Message}"
                    );
                }
            }
        }

        private void ClearAICache()
        {
            try
            {
                Brain?.battleBrain?.ClearAICache();
            }
            catch { }
        }

        private void HandleMapStateChanged() => ClearAICache();

        private void OnDestroy() => DisconnectFromBrainEvents();

        #endregion

        #region Turn Management

        public void IncrementTurnCount() => _currentTurnCount++;

        public void ResetTurnCount() => _currentTurnCount = 0;

        public int Turns() => _currentTurnCount;

        #endregion

        #region Roster Management

        public void InitializeBattleRosters()
        {
            var res = EnsureRostersExist();
            if (!res.Success)
            {
#if UNITY_EDITOR
                Debug.LogError(
                    $"BattleGameObject.InitializeBattleRosters failed: {res.ErrorMessage}"
                );
#endif
                return;
            }

            res = InitializeRuntimePlacements();
            if (!res.Success)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"BattleGameObject.InitializeBattleRosters: {res.ErrorMessage}");
#endif
                // Not fatal; continue to try apply prebattle placements
            }

            res = ApplyPreBattlePlacements();
            if (!res.Success)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"BattleGameObject.InitializeBattleRosters: {res.ErrorMessage}");
#endif
            }
        }

        private OperationResult EnsureRostersExist()
        {
            try
            {
                if (PlayerTeamRoster == null)
                {
                    var go = new GameObject("BattleRoster - Player Team");
                    go.transform.SetParent(transform);
                    PlayerTeamRoster = go.AddComponent<PlayerTeamRosterInstance>();
                }
                else
                {
                    PlayerTeamRoster.Clear();
                }

                if (EnemyTeamRoster == null)
                {
                    var go = new GameObject("BattleRoster - Enemy Team");
                    go.transform.SetParent(transform);
                    EnemyTeamRoster = go.AddComponent<GenericRosterInstance>();
                }
                else
                {
                    EnemyTeamRoster.Clear();
                }

                if (HasThirdParty)
                {
                    if (ThirdPartyTeamRoster == null)
                    {
                        var go = new GameObject("BattleRoster - Third Party Team");
                        go.transform.SetParent(transform);
                        ThirdPartyTeamRoster = go.AddComponent<GenericRosterInstance>();
                    }
                    else
                    {
                        ThirdPartyTeamRoster.Clear();
                    }
                }

                return OperationResult.SuccessResult();
            }
            catch (System.Exception ex)
            {
                return OperationResult.Failure($"EnsureRostersExist failed: {ex.Message}");
            }
        }

        private OperationResult InitializeRuntimePlacements()
        {
            try
            {
                var persistentPlayer =
                    Brain?.gamewideContextBrain?.GetPersistentPlayerTeamRosterInstance();
                if (persistentPlayer != null)
                {
                    PlayerTeamRoster.ApplyDecodedPlacements(persistentPlayer.GetPlacements());
                }
                else
                {
                    PlayerTeamRoster.InitializeRuntimePlacementsFromTemplate();
                }

                EnemyTeamRoster?.InitializeRuntimePlacementsFromTemplate();
                ThirdPartyTeamRoster?.InitializeRuntimePlacementsFromTemplate();

                return OperationResult.SuccessResult();
            }
            catch (System.Exception ex)
            {
                return OperationResult.Failure($"InitializeRuntimePlacements failed: {ex.Message}");
            }
        }

        private OperationResult ApplyPreBattlePlacements()
        {
            try
            {
                var prep = Brain?.battleBrain?.PreparationObject;
                if (prep?.placements != null && prep.placements.Count > 0)
                {
                    var list = new List<Characters.Roster.UnitPlacement>();
                    foreach (var kvp in prep.placements)
                    {
                        var pos = kvp.Key;
                        var inst = kvp.Value;
                        if (inst == null || inst.CharacterTemplate == null)
                        {
                            continue;
                        }

                        var up = new Characters.Roster.UnitPlacement
                        {
                            CharacterData = inst.CharacterTemplate,
                            SpawnPosition = pos,
                            Order = list.Count,
                        };
                        up.SetStatus(Turnroot.Characters.Roster.UnitStatus.NotSpawned);
                        up.SetActiveRightNow(true);

                        list.Add(up);
                    }

                    if (list.Count > 0)
                    {
                        PlayerTeamRoster.ApplyDecodedPlacements(list.ToArray());
#if UNITY_EDITOR
                        Debug.Log(
                            $"BattleGameObject: Applied PreBattle placements to PlayerTeamRoster ({list.Count})"
                        );
#endif
                    }
                }

                return OperationResult.SuccessResult();
            }
            catch (System.Exception ex)
            {
                return OperationResult.Failure($"ApplyPreBattlePlacements failed: {ex.Message}");
            }
        }

        public OperationResult PopulateBattleRostersFromTemplates()
        {
            var battleBrain = Brain?.battleBrain;
            if (battleBrain == null)
            {
                return OperationResult.Failure("Brain or battleBrain is null");
            }

            var playerInstance = battleBrain.InstantiatePlayerTeamRoster();
            if (playerInstance == null)
            {
                return OperationResult.Failure("Could not instantiate player team roster");
            }

            PlayerTeamRoster.AddInstances(playerInstance.Instances);

            if (_enemyRoster != null)
            {
                var enemyInstance = battleBrain.InstantiateGenericRoster(_enemyRoster);
                EnemyTeamRoster.AddInstances(enemyInstance.Instances);
            }

            if (HasThirdParty && _thirdPartyRoster != null)
            {
                var thirdPartyInstance = battleBrain.InstantiateGenericRoster(_thirdPartyRoster);
                ThirdPartyTeamRoster.AddInstances(thirdPartyInstance.Instances);
            }

            return OperationResult.SuccessResult();
        }

        public OperationResult ClearBattleRosters()
        {
            try
            {
                PlayerTeamRoster?.Clear();
                EnemyTeamRoster?.Clear();
                ThirdPartyTeamRoster?.Clear();
                return OperationResult.SuccessResult();
            }
            catch (System.Exception ex)
            {
                return OperationResult.Failure($"ClearBattleRosters failed: {ex.Message}");
            }
        }

        #endregion
    }
}
