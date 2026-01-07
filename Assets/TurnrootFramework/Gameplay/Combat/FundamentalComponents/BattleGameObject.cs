using System.Linq;
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
        [field: Header("Battle Components")]
        [field: SerializeField, HideInInspector]
        public BattleContext Context { get; private set; }

        public bool HasThirdParty;
        public bool ThirdPartyFightsAllies;
        public bool ThirdPartyFightsEnemies;

        [SerializeField, SerializeReference]
        private BattleCondition[] _battleConditions;

        public BattleCondition[] BattleConditions => _battleConditions;

        [SerializeField]
        private MapGrid _mapGrid;

        [Header("Roster Templates")]
        [SerializeField]
        private GenericRoster _enemyRoster;

        [SerializeField]
        private GenericRoster _thirdPartyRoster;

        [SerializeField, NaughtyAttributes.ReadOnly]
        private int _currentTurnCount;

        [field: HideInInspector]
        public Brain.Brain Brain { get; set; }

        // Track connection state to prevent duplicate subscriptions
        private bool _isConnectedToBrain;

        // Battle rosters - typed for their specific roles
        public PlayerTeamRosterInstance PlayerTeamRoster { get; private set; }
        public GenericRosterInstance EnemyTeamRoster { get; private set; }
        public GenericRosterInstance ThirdPartyTeamRoster { get; private set; }

        public LayerMask GroundLayerMask;

        #region Unity Lifecycle

        public void Awake()
        {
            ResetTurnCount();
            Context ??= GetComponent<BattleContext>();
            _mapGrid = _mapGrid != null ? _mapGrid : GetComponentInChildren<MapGrid>();

            if (_mapGrid == null)
            {
#if UNITY_EDITOR
                Debug.LogError("BattleGameObject requires a MapGrid child");
#endif
                Debug.Break();
            }

            if (_battleConditions == null)
            {
#if UNITY_EDITOR
                Debug.LogError("BattleGameObject requires BattleConditions to be set");
#endif
                Debug.Break();
            }

            // MapGrid will be set during context initialization when the Brain is connected.
            // Context.mapGrid will be initialized in InitializeContextWithBrain().
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure the Context reference is set in the editor so condition assets can reference it.
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

                // Auto-assign the context so conditions edited in the inspector can use it
                condition.battleContext = Context;

                // Attempt to resolve references so configured conditions immediately have valid links
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
                        $"BattleGameObject OnValidate: Failed to resolve condition {condition?.Name}: {ex.Message}"
                    );
                }
            }
        }
#endif

        #endregion

        #region Brain Event Connection

        public OperationResult ConnectToBrainEvents()
        {
            if (Brain == null)
            {
                return OperationResult.Failure(
                    "BattleGameObject ConnectToBrainEvents failed: Brain reference is null."
                );
            }

            // Guard against duplicate subscriptions
            if (_isConnectedToBrain)
            {
                return OperationResult.Failure(
                    "BattleGameObject ConnectToBrainEvents failed: Already connected to Brain events."
                );
            }

#if UNITY_EDITOR
            Debug.Log("BattleGameObject connecting to Brain events");
#endif

            // Initialize context with Brain reference for command pattern
            InitializeContextWithBrain();

            // Subscribe to battle events
            Brain.OnTurnEnded += HandleTurnEnded;
            Brain.OnAllyDamaged += HandleAllyDamaged;
            Brain.OnEnemyDamaged += HandleEnemyDamaged;
            Brain.OnUnitDefeated += HandleUnitDefeated;
            Brain.OnUnitMoved += HandleUnitMoved;
            Brain.OnBattleCompleted += HandleExitBattle;

            // Subscribe to advanced priority events for spawn/defeat to invalidate condition caches
            Brain.Subscribe<UnitSpawnedEvent>(HandleUnitSpawnedEvent, EventPriority.Normal);
            Brain.Subscribe<UnitDefeatedEvent>(HandleUnitDefeatedEvent, EventPriority.Normal);

            // Also subscribe to Brain's advanced PriorityEventBus for basic unit events in case commands publish them
            // (handlers above will take care of cache invalidation and checks)

            _isConnectedToBrain = true;
            return OperationResult.SuccessResult();
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

            // Unsubscribe from advanced events
            Brain.Unsubscribe<UnitSpawnedEvent>(HandleUnitSpawnedEvent);
            Brain.Unsubscribe<UnitDefeatedEvent>(HandleUnitDefeatedEvent);

            // Unsubscribe from map state changes if any
            try
            {
                if (_mapGrid != null)
                {
                    _mapGrid.OnStateVersionChanged -= HandleMapStateChanged;
                }
            }
            catch (System.Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Failed to unsubscribe from map state changes: {ex.Message}");
#endif
            }

            _isConnectedToBrain = false;
        }

        public OperationResult ConnectBattleConditionsToContext()
        {
            try
            {
                foreach (var condition in _battleConditions)
                {
                    condition.battleContext = Context;
                }

                // Resolve any required-condition references (by name) so conditions can query requirements
                foreach (var condition in _battleConditions)
                {
                    try
                    {
                        condition.ResolveRequiredConditions(_battleConditions);

                        // Resolve ConditionalGroup children if applicable
                        if (condition is ConditionalGroupBattleCondition group)
                        {
                            try
                            {
                                group.ResolveChildConditions(_battleConditions);
                            }
                            catch (System.Exception ex2)
                            {
                                Debug.LogWarning(
                                    $"Failed to resolve group children for {condition?.Name}: {ex2.Message}"
                                );
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning(
                            $"Failed to resolve required conditions for {condition?.Name}: {ex.Message}"
                        );
                    }
                }
                return OperationResult.SuccessResult();
            }
            catch (System.Exception ex)
            {
                return OperationResult.Failure(
                    $"BattleGameObject ConnectBattleConditionsToContext failed: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Adds a new BattleCondition instance to this BattleGameObject at runtime.
        /// Resolves its context and any named references and returns an OperationResult.
        /// </summary>
        public OperationResult AddConditionAtRuntime(BattleCondition condition)
        {
            if (condition == null)
            {
                return OperationResult.Failure("Condition is null");
            }

            try
            {
                var list = new System.Collections.Generic.List<BattleCondition>(
                    _battleConditions ?? System.Array.Empty<BattleCondition>()
                );
                list.Add(condition);
                _battleConditions = list.ToArray();

                // Connect new condition to context and resolve its references
                condition.battleContext = Context;
                condition.ResolveRequiredConditions(_battleConditions);
                if (condition is ConditionalGroupBattleCondition group)
                {
                    group.ResolveChildConditions(_battleConditions);
                }

                // Invalidate caches in case the new condition depends on unit lists
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
            if (Context == null)
            {
#if UNITY_EDITOR
                Debug.LogError("BattleGameObject: Context is null during initialization!");
#endif
                return;
            }

            if (Brain == null)
            {
                Debug.LogError(
                    "BattleGameObject: Brain is null - context will not function correctly!"
                );
                return;
            }

            // Use explicit initialization so the Context has non-null Brain guaranteed
            try
            {
                Context.Initialize(Brain, _mapGrid);
#if UNITY_EDITOR
                Debug.Log("BattleGameObject: Context initialized via Initialize(brain, mapGrid)");
#endif
                // Subscribe to map state changes to invalidate AI caches when terrain/occupancy changes
                try
                {
                    _mapGrid.OnStateVersionChanged += HandleMapStateChanged;
                }
                catch (System.Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"Failed to subscribe to map state changes: {ex.Message}");
#endif
                }
            }
            catch (System.Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogError($"BattleGameObject: Failed to initialize context: {ex.Message}");
#endif
            }
        }

        #endregion

        #region Event Handlers

        private void HandleTurnEnded()
        {
            IncrementTurnCount();

            // Propagate to conditions that need turn end notifications
            foreach (var surviveTurns in _battleConditions.OfType<SurviveTurnsBattleCondition>())
            {
                surviveTurns.OnTurnEnd();
            }
            foreach (var timeLimit in _battleConditions.OfType<TimeLimitBattleCondition>())
            {
                timeLimit.OnTurnEnd();
            }
        }

        private void HandleAllyDamaged(CharacterInstance unit, int damageAmount)
        {
            // Propagate to conditions that track ally damage
            foreach (
                var allyDamageCondition in _battleConditions.OfType<LimitTotalAllyDamageBattleCondition>()
            )
            {
                allyDamageCondition.OnAllyDamaged(damageAmount);
            }
        }

        private void HandleEnemyDamaged(CharacterInstance unit, int damageAmount)
        {
            // Propagate to conditions that track enemy damage
            foreach (
                var enemyDamageCondition in _battleConditions.OfType<DealMinimumTotalEnemyDamageBattleCondition>()
            )
            {
                enemyDamageCondition.OnEnemyDamaged(damageAmount);
            }
        }

        private void HandleUnitDefeated(CharacterInstance unit)
        {
            // Invalidate caches for all conditions (unit lists changed)
            InvalidateAllConditionCaches();

            // Also clear AI helper caches to avoid stale pathfinding tiles and reachability results
            try
            {
                Brain?.battleBrain?.ClearAICache();
            }
            catch (System.Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Failed to clear AI cache on unit defeated: {ex.Message}");
#endif
            }

            // Check defeat-related conditions
            foreach (var defeatAll in _battleConditions.OfType<DefeatAllEnemiesBattleCondition>())
            {
                defeatAll.CheckCondition();
            }
            foreach (var defeatSpecific in _battleConditions.OfType<DefeatEnemyBattleCondition>())
            {
                defeatSpecific.CheckCondition();
            }
            foreach (var protectNPCs in _battleConditions.OfType<ProtectNPCsBattleCondition>())
            {
                protectNPCs.CheckCondition();
            }
#if TURNROOT_MONSTERS_MODULE
            foreach (
                var defeatAllMonsters in _battleConditions.OfType<DefeatAllMonstersBattleCondition>()
            )
            {
                //TODO: defeatAllMonsters.CheckCondition();
            }
            foreach (var defeatMonster in _battleConditions.OfType<DefeatMonsterBattleCondition>())
            {
                defeatMonster.CheckCondition();
            }
#endif
        }

        private void HandleUnitMoved(CharacterInstance unit, Vector2Int newPosition)
        {
            // Check movement-related conditions
            foreach (var condition in _battleConditions)
            {
                if (condition is SurviveUntilAllyReachesTileBattleCondition reachTile)
                {
                    reachTile.CheckCondition();
                }

                // Track reached tiles for ReachTilesBattleCondition
                if (condition is ReachTilesBattleCondition reachTilesCondition)
                {
                    // Check if this unit is on the player team
                    bool isPlayerUnit = PlayerTeamRoster?.Instances?.Contains(unit) ?? false;
                    if (isPlayerUnit)
                    {
                        reachTilesCondition.OnUnitReachedTile(newPosition);
                    }
                }
            }

            // Clear AI caches when a unit moves as occupancy changed
            try
            {
                Brain?.battleBrain?.ClearAICache();
            }
            catch (System.Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Failed to clear AI cache on unit moved: {ex.Message}");
#endif
            }
        }

        private void HandleExitBattle(BattleExitType exitType) => DisconnectFromBrainEvents();

        private void HandleUnitSpawnedEvent(UnitSpawnedEvent evt)
        {
            // Unit spawned into battle - invalidate caches that rely on unit lists
            InvalidateAllConditionCaches();
            // Also clear AI helper caches to avoid stale pathfinding tiles and reachability results
            try
            {
                Brain?.battleBrain?.ClearAICache();
            }
            catch (System.Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Failed to clear AI cache on unit spawned: {ex.Message}");
#endif
            }

            // Optionally check conditions that may be affected by new unit presence
            foreach (var condition in _battleConditions)
            {
                // Some conditions may react to increased ally/enemy counts; call CheckCondition when available
                if (condition is DefeatEnemyBattleCondition defeatSpecific)
                {
                    defeatSpecific.CheckCondition();
                }
                if (condition is DefeatAllEnemiesBattleCondition defeatAll)
                {
                    defeatAll.CheckCondition();
                }
            }
        }

        private void HandleUnitDefeatedEvent(UnitDefeatedEvent evt) =>
            // Delegate to existing handler for defeated units to reuse logic
            HandleUnitDefeated(evt.Unit);

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
                        $"Failed to invalidate cache for condition {condition?.Name ?? condition?.GetType().Name}: {ex.Message}"
                    );
                }
            }
        }

        private void HandleMapStateChanged()
        {
            try
            {
                Brain?.battleBrain?.ClearAICache();
            }
            catch (System.Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Failed to clear AI cache on map change: {ex.Message}");
#endif
            }
        }

        private void OnDestroy() => DisconnectFromBrainEvents();

        #endregion

        #region Turn Count Management

        public void IncrementTurnCount() => _currentTurnCount++;

        public void ResetTurnCount() => _currentTurnCount = 0;

        public int Turns() => _currentTurnCount;

        #endregion

        #region Battle Roster Management

        /// <summary>
        /// Initialize the three temporary battle rosters.
        /// Creates empty instances ready to be populated.
        /// </summary>
        public void InitializeBattleRosters()
        {
            // Create or clear player roster
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

            // Create or clear enemy roster
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

            // Create or clear third party roster (if there is one)
            if (ThirdPartyTeamRoster == null && HasThirdParty)
            {
                var go = new GameObject("BattleRoster - Third Party Team");
                go.transform.SetParent(transform);
                ThirdPartyTeamRoster = go.AddComponent<GenericRosterInstance>();
            }
            else if (HasThirdParty)
            {
                ThirdPartyTeamRoster.Clear();
            }

#if UNITY_EDITOR
            Debug.Log("BattleGameObject: Initialized three temporary battle rosters");
#endif
        }

        /// <summary>
        /// Populate rosters from templates and persistent data.
        /// </summary>
        public OperationResult PopulateBattleRostersFromTemplates()
        {
            var battleBrain = Brain?.battleBrain;
            if (battleBrain != null)
            {
                var playerTeamRosterInstance = battleBrain.InstantiatePlayerTeamRoster();
                if (playerTeamRosterInstance != null)
                {
                    PlayerTeamRoster.AddInstances(playerTeamRosterInstance.Instances);
                }
                else
                {
                    return OperationResult.Failure(
                        "PopulateBattleRostersFromTemplates failed: Could not instantiate player team roster from GamewideContextBrain."
                    );
                }
            }
            else
            {
                return OperationResult.Failure(
                    "PopulateBattleRostersFromTemplates failed: Brain or GamewideContextBrain is null."
                );
            }

            // Load enemy roster from this battle's enemy template
            if (_enemyRoster != null)
            {
                var enemyInstance = battleBrain.InstantiateGenericRoster(_enemyRoster);
                EnemyTeamRoster.AddInstances(enemyInstance.Instances);
            }

            // Load third party roster from this battle's NPC template (if any)
            if (HasThirdParty && _thirdPartyRoster != null)
            {
                var thirdPartyInstance = battleBrain.InstantiateGenericRoster(_thirdPartyRoster);
                ThirdPartyTeamRoster.AddInstances(thirdPartyInstance.Instances);
            }

            return OperationResult.SuccessResult();
        }

        /// <summary>
        /// Clear all three temporary battle rosters.
        /// </summary>
        public OperationResult ClearBattleRosters()
        {
            try
            {
                PlayerTeamRoster.Clear();
                EnemyTeamRoster.Clear();
                ThirdPartyTeamRoster.Clear();

#if UNITY_EDITOR
                Debug.Log("BattleGameObject: Cleared all temporary battle rosters");
#endif
                return OperationResult.SuccessResult();
            }
            catch (System.Exception ex)
            {
                return OperationResult.Failure(
                    $"BattleGameObject ClearBattleRosters failed: {ex.Message}"
                );
            }
        }

        #endregion
    }
}
