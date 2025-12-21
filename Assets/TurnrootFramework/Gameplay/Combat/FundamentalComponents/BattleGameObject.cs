using System.Linq;
using Turnroot.Characters;
using Turnroot.Characters.Components;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Environment;
using Turnroot.Gameplay.Combat.FundamentalComponents.Conditions.Specific;
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
    public class BattleGameObject : MonoBehaviour
    {
        public bool HasThirdParty;

        [field: Header("Battle Components")]
        [field: SerializeField]
        public BattleContext Context { get; private set; }

        [SerializeField, SerializeReference]
        private BattleCondition[] _battleConditions;

        public BattleCondition[] BattleConditions => _battleConditions;

        [SerializeField]
        private MapGrid _mapGrid;

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

        #region Unity Lifecycle

        public void Awake()
        {
            ResetTurnCount();
            Context ??= new BattleContext();
            _mapGrid = _mapGrid != null ? _mapGrid : GetComponentInChildren<MapGrid>();

            if (_mapGrid == null)
            {
                Debug.LogError("BattleGameObject requires a MapGrid child");
                Debug.Break();
            }

            if (_battleConditions == null)
            {
                Debug.LogError("BattleGameObject requires BattleConditions to be set");
                Debug.Break();
            }

            // Connect Context to mapGrid
            Context.mapGrid = _mapGrid;
        }

        #endregion

        #region Brain Event Connection

        public void ConnectToBrainEvents()
        {
            if (Brain == null)
            {
                Debug.LogWarning("BattleGameObject has no Brain to connect to");
                return;
            }

            // Guard against duplicate subscriptions
            if (_isConnectedToBrain)
            {
                Debug.LogWarning("BattleGameObject is already connected to Brain events");
                return;
            }

            Debug.Log("BattleGameObject connecting to Brain events");

            // Initialize context with Brain reference for command pattern
            InitializeContextWithBrain();

            // Subscribe to battle events
            Brain.OnTurnEnded += HandleTurnEnded;
            Brain.OnAllyDamaged += HandleAllyDamaged;
            Brain.OnEnemyDamaged += HandleEnemyDamaged;
            Brain.OnUnitDefeated += HandleUnitDefeated;
            Brain.OnUnitMoved += HandleUnitMoved;
            Brain.OnBattleCompleted += HandleExitBattle;

            _isConnectedToBrain = true;
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

            _isConnectedToBrain = false;
        }

        public void ConnectBattleConditionsToGamewideContextBrain()
        {
            if (Brain == null || Brain.gamewideContextBrain == null)
            {
                Debug.LogError(
                    "BattleGameObject cannot connect BattleConditions: Brain or GamewideContextBrain is null"
                );
                Debug.Break();
                return;
            }

            foreach (var condition in _battleConditions)
            {
                condition.gamewideContextBrain = Brain.gamewideContextBrain;
            }
        }

        private void InitializeContextWithBrain()
        {
            if (Context == null)
            {
                Debug.LogError("BattleGameObject: Context is null during initialization!");
                return;
            }

            if (Brain == null)
            {
                Debug.LogError(
                    "BattleGameObject: Brain is null - context will not function correctly!"
                );
                return;
            }

            Context.Brain = Brain;
            Debug.Log("BattleGameObject: Context initialized with Brain reference");
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
        }

        private void HandleExitBattle(BattleExitType exitType)
        {
            DisconnectFromBrainEvents();
        }

        private void OnDestroy()
        {
            DisconnectFromBrainEvents();
        }

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

            // Create or clear third party roster
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

            Debug.Log("BattleGameObject: Initialized three temporary battle rosters");
        }

        /// <summary>
        /// Populate rosters from templates and persistent data.
        /// </summary>
        public void PopulateBattleRostersFromTemplates()
        {
            // TODO: Load player roster from persistent data (GamewideContextBrain)
            // PlayerTeamRoster should be populated from the persistent player roster

            // TODO: Load enemy roster from this battle's enemy template
            // EnemyTeamRoster should be populated from a RosterTemplate assigned to this battle

            // TODO: Load third party roster from this battle's NPC template (if any)
            // ThirdPartyTeamRoster should be populated from a RosterTemplate if HasThirdParty is true

            Debug.Log("BattleGameObject: TODO - Populate rosters from templates");
        }

        /// <summary>
        /// Spawn all roster units onto the grid at their spawn points.
        /// </summary>
        public void SpawnRosterUnitsOntoGrid()
        {
            // TODO: For each roster, find spawn points and place units
            // - Get spawn points from MapGrid
            // - Match units to spawn points by faction
            // - Set unit.MapGridPosition
            // - Call Context.mapGrid.SetOccupied()
            // - Publish Brain.PublishCharacterSpawned()

            Debug.Log("BattleGameObject: TODO - Spawn roster units onto grid");
        }

        /// <summary>
        /// Build the BattleContext from the populated rosters.
        /// </summary>
        public void PopulateBattleContextFromRosters()
        {
            if (Context == null)
            {
                Debug.LogError("BattleGameObject: Context is null!");
                return;
            }

            // Clear existing context data
            Context.Targets.Clear();
            Context.Allies.Clear();
            Context.ThirdParty.Clear();

            // TODO: Populate Context.Targets from EnemyTeamRoster
            // TODO: Populate Context.Allies from PlayerTeamRoster
            // TODO: Populate Context.ThirdParty from ThirdPartyTeamRoster
            // Filter out defeated units when populating

            Debug.Log("BattleGameObject: TODO - Populate battle context from rosters");
        }

        /// <summary>
        /// Clear all three temporary battle rosters.
        /// </summary>
        public void ClearBattleRosters()
        {
            PlayerTeamRoster?.Clear();
            EnemyTeamRoster?.Clear();
            ThirdPartyTeamRoster?.Clear();

            Debug.Log("BattleGameObject: Cleared all temporary battle rosters");
        }

        #endregion
    }
}
