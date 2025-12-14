using System.Linq;
using Turnroot.Characters;
using Turnroot.Characters.Components;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Environment;
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

        [SerializeField]
        private MapGrid _mapGrid;

        [SerializeField, NaughtyAttributes.ReadOnly]
        private int _currentTurnCount;

        [field: HideInInspector]
        public Brain.Brain Brain { get; set; }

        // Track connection state to prevent duplicate subscriptions
        private bool _isConnectedToBrain;

        // Battle rosters - temporary for this battle only

        public RosterInstance PlayerTeamRoster { get; private set; }
        public RosterInstance EnemyTeamRoster { get; private set; }
        public RosterInstance ThirdPartyTeamRoster { get; private set; }

        public void ConnectToBrainEvents()
        {
            if (Brain == null)
            {
                Debug.LogWarning("BattleGameObject has no Brain to connect to.");
                return;
            }

            // Guard against duplicate subscriptions
            if (_isConnectedToBrain)
            {
                Debug.LogWarning(
                    "BattleGameObject is already connected to Brain events. Skipping duplicate subscription."
                );
                return;
            }

            Debug.Log("BattleGameObject connecting to Brain events.");

            // Initialize context with Brain reference for command pattern
            InitializeContextWithBrain();

            // Subscribe to turn end event
            Brain.OnTurnEnded += HandleTurnEnded;

            // Subscribe to damage events
            Brain.OnAllyDamaged += HandleAllyDamaged;
            Brain.OnEnemyDamaged += HandleEnemyDamaged;

            // Subscribe to defeat and movement events
            Brain.OnUnitDefeated += HandleUnitDefeated;
            Brain.OnUnitMoved += HandleUnitMoved;

            // Subscribe to battle lifecycle events
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
                    // Check if this unit is on the player team (typically only player units can fulfill reach conditions)
                    bool isPlayerUnit = PlayerTeamRoster?.Instances?.Contains(unit) ?? false;
                    if (isPlayerUnit)
                    {
                        reachTilesCondition.OnUnitReachedTile(newPosition);
                    }
                }
            }
        }

        private void HandleExitBattle(BattleExitType exitType) => DisconnectFromBrainEvents();

        private void OnDestroy() => DisconnectFromBrainEvents();

        public void ConnectBattleConditionsToGamewideContextBrain()
        {
            if (Brain == null || Brain.gamewideContextBrain == null)
            {
                Debug.LogError(
                    "BattleGameObject cannot connect BattleConditions: Brain or GamewideContextBrain is null."
                );
                Debug.Break();
                return;
            }

            foreach (var condition in _battleConditions)
            {
                condition.gamewideContextBrain = Brain.gamewideContextBrain;
            }
        }

        public void Awake()
        {
            ResetTurnCount();
            Context ??= new BattleContext();
            _mapGrid = _mapGrid != null ? _mapGrid : GetComponentInChildren<MapGrid>();
            if (_mapGrid == null)
            {
                Debug.LogError("BattleGameObject requires a MapGrid child.");
                Debug.Break();
            }
            if (_battleConditions == null)
            {
                Debug.LogError("BattleGameObject requires BattleConditions to be set.");
                Debug.Break();
            }

            // Connect Context to mapGrid
            Context.mapGrid = _mapGrid;
        }

        /// <summary>
        /// Called after Brain is set to finish context initialization.
        /// Context requires Brain for all command-based operations.
        /// </summary>
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
            Debug.Log("BattleGameObject: Context initialized with Brain reference.");
        }

        public void IncrementTurnCount() => _currentTurnCount++;

        public void ResetTurnCount() => _currentTurnCount = 0;

        public int Turns() => _currentTurnCount;

        #region Battle Roster Management

        /// <summary>
        /// Initialize the three temporary battle rosters.
        /// </summary>
        public void InitializeBattleRosters()
        {
            // Create or clear player roster
            if (PlayerTeamRoster == null)
            {
                var go = new GameObject("BattleRoster - Player Team");
                go.transform.SetParent(transform);
                PlayerTeamRoster = go.AddComponent<RosterInstance>();
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
                EnemyTeamRoster = go.AddComponent<RosterInstance>();
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
                ThirdPartyTeamRoster = go.AddComponent<RosterInstance>();
            }
            else
            {
                ThirdPartyTeamRoster.Clear();
            }

            Debug.Log("BattleGameObject: Initialized three temporary battle rosters.");
        }

        /// <summary>
        /// Populate battle rosters with characters sorted by faction.
        /// </summary>
        public void PopulateBattleRostersFromGamewideContext(Brain.GamewideContextBrain gwcb)
        {
            if (gwcb == null)
            {
                Debug.LogWarning(
                    "BattleGameObject: Cannot populate battle rosters - GamewideContextBrain is null."
                );
                return;
            }

            var allCharacters = gwcb.GetAllActiveInstances();
            int playerCount = 0;
            int enemyCount = 0;
            int thirdPartyCount = 0;

            foreach (var character in allCharacters)
            {
                if (character?.CharacterTemplate?.Which == null)
                {
                    continue;
                }

                string faction = character.CharacterTemplate.Which.Value;

                if (faction is CharacterWhich.ALLY or CharacterWhich.AVATAR)
                {
                    PlayerTeamRoster.AddInstance(character);
                    playerCount++;
                }
                else if (faction == CharacterWhich.ENEMY)
                {
                    EnemyTeamRoster.AddInstance(character);
                    enemyCount++;
                }
                else if (faction == CharacterWhich.NPC)
                {
                    ThirdPartyTeamRoster.AddInstance(character);
                    thirdPartyCount++;
                }
            }

            Debug.Log(
                $"BattleGameObject: Populated battle rosters - Player: {playerCount}, Enemy: {enemyCount}, Third Party: {thirdPartyCount}"
            );
        }

        /// <summary>
        /// Add a character instance to the appropriate battle roster based on faction.
        /// </summary>
        public void AddCharacterToBattleRoster(CharacterInstance character)
        {
            if (character?.CharacterTemplate?.Which == null)
            {
                Debug.LogWarning(
                    "BattleGameObject: Cannot add character to battle roster - invalid character or faction."
                );
                return;
            }

            string faction = character.CharacterTemplate.Which.Value;

            if (faction is CharacterWhich.ALLY or CharacterWhich.AVATAR)
            {
                PlayerTeamRoster?.AddInstance(character);
            }
            else if (faction == CharacterWhich.ENEMY)
            {
                EnemyTeamRoster?.AddInstance(character);
            }
            else if (faction == CharacterWhich.NPC)
            {
                ThirdPartyTeamRoster?.AddInstance(character);
            }
        }

        /// <summary>
        /// Clear all three temporary battle rosters.
        /// </summary>
        public void ClearBattleRosters()
        {
            PlayerTeamRoster?.Clear();
            EnemyTeamRoster?.Clear();
            ThirdPartyTeamRoster?.Clear();

            Debug.Log("BattleGameObject: Cleared all temporary battle rosters.");
        }

        #endregion
    }
}
