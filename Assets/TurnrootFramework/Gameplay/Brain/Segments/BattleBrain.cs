using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Gameplay.Combat;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// The battle brain manages one battle at a time.
    /// Responsible for initializing battles and managing turn order.
    /// </summary>
    public class BattleBrain : BrainComponent
    {
        private BattleGameObject _battleGameObject;
        private TurnRotisserie _turnRotisserie;

        public BattleGameObject BattleObject => _battleGameObject;

        // Roster accessors through BattleGameObject
        public PlayerTeamRosterInstance PlayerTeamRoster => _battleGameObject?.PlayerTeamRoster;
        public GenericRosterInstance EnemyTeamRoster => _battleGameObject?.EnemyTeamRoster;
        public GenericRosterInstance ThirdPartyTeamRoster =>
            _battleGameObject?.ThirdPartyTeamRoster;

        protected override void Awake()
        {
            base.Awake();

            _turnRotisserie = GetComponent<TurnRotisserie>();
            if (_turnRotisserie == null)
            {
                _turnRotisserie = gameObject.AddComponent<TurnRotisserie>();
            }
            _turnRotisserie.Brain = _brain;

            Debug.Log("BattleBrain: TurnRotisserie ready");
        }

        protected override EventPriority GetSubscriptionPriority() => EventPriority.Highest;

        protected override void SubscribeToBrainEvents()
        {
            _brain.OnBattleStarted += HandleStartBattle;
            _brain.OnBattleCompleted += HandleExitBattle;
            _brain.OnUnitTakesAnotherTurn += HandleUnitTakesAnotherTurn;
            _brain.OnCriticalHit += HandleCriticalHit;
            _brain.OnWeaponUsesChanged += HandleWeaponUsesChanged;
            _brain.OnItemStolen += HandleItemStolen;
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            _brain.OnBattleStarted -= HandleStartBattle;
            _brain.OnBattleCompleted -= HandleExitBattle;
            _brain.OnUnitTakesAnotherTurn -= HandleUnitTakesAnotherTurn;
            _brain.OnCriticalHit -= HandleCriticalHit;
            _brain.OnWeaponUsesChanged -= HandleWeaponUsesChanged;
            _brain.OnItemStolen -= HandleItemStolen;
        }

        public void ProgressTurnOrder()
        {
            if (!_turnRotisserie.Progress())
            {
                Debug.LogError("BattleBrain: Failed to progress turn order!");
                Debug.Break();
            }
        }

        #region Battle Initialization

        public void HandleStartBattle()
        {
            Debug.Log("BattleBrain: Handling StartBattle event");

            _battleGameObject = FindBattleGameObjectInScene();

            if (_battleGameObject == null)
            {
                Debug.LogError("BattleBrain: No BattleGameObject found in any loaded scene!");
                return;
            }

            // Connect systems
            _battleGameObject.Brain = _brain;
            _battleGameObject.ConnectToBrainEvents();
            _battleGameObject.ConnectBattleConditionsToGamewideContextBrain();

            Debug.Log($"BattleBrain: Connected to BattleGameObject");

            // Configure turn order
            _turnRotisserie.HasThirdParty = _battleGameObject.HasThirdParty;

            // Initialize battle using roster system
            InitializeBattleFromRosters();

            // Initialize advanced systems (commands, snapshots)
            InitializeBattleAdvancedSystems();

            Debug.Log("BattleBrain: Battle initialization complete");
        }

        private BattleGameObject FindBattleGameObjectInScene()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;

                foreach (GameObject rootObject in scene.GetRootGameObjects())
                {
                    var battleObj = rootObject.GetComponentInChildren<BattleGameObject>();
                    if (battleObj != null)
                    {
                        Debug.Log($"BattleBrain: Found BattleGameObject in scene '{scene.name}'");
                        return battleObj;
                    }
                }
            }

            Debug.LogWarning("BattleBrain: No BattleGameObject found in loaded scenes");
            return null;
        }

        private void InitializeBattleFromRosters()
        {
            // TODO: 1. Create empty runtime roster instances
            // _battleGameObject.InitializeBattleRosters();

            // TODO: 2. Populate rosters from templates and persistent data
            // _battleGameObject.PopulateBattleRostersFromGamewideContext(_brain.gamewideContextBrain);

            // TODO: 3. Spawn units onto grid from rosters
            // _battleGameObject.SpawnRosterUnitsOntoGrid();

            // TODO: 4. Build battle context from spawned units
            // _battleGameObject.PopulateBattleContextFromRosters();

            // TODO: 5. Initialize AI system
            // InitializeAISystem();

            Debug.Log("BattleBrain: TODO - Implement roster-based initialization");
        }

        private void InitializeAISystem()
        {
            // TODO: Create AI helper for battle context
            // TODO: Subscribe to AI evaluation events from TurnRotisserie
            // var aiHelper = new BattleContextAIHelper(_battleGameObject.Context);
            // _brain.OnAIEvaluationRequested += (unit) => { ... };

            Debug.Log("BattleBrain: TODO - Initialize AI system");
        }

        private void InitializeBattleAdvancedSystems()
        {
            // Clear any previous battle's command history
            _brain.Commands?.Clear();

            // Take initial snapshot of battle state
            _brain.TakeSnapshot();

            Debug.Log("BattleBrain: Advanced systems initialized");
        }

        #endregion

        #region Battle Cleanup

        private void HandleExitBattle(BattleExitType exitType)
        {
            Debug.Log($"BattleBrain: Handling ExitBattle event with type: {exitType}");

            CleanupBattleAdvancedSystems(exitType);

            // TODO: Clean up rosters
            // _battleGameObject?.ClearBattleRosters();

            Debug.Log("BattleBrain: Battle cleanup complete");
        }

        private void CleanupBattleAdvancedSystems(BattleExitType exitType)
        {
            // Clear command history (unless bookmarking)
            if (exitType != BattleExitType.Bookmark)
            {
                _brain.Commands?.Clear();
            }

            // Clear snapshots
            _brain.Snapshots?.Clear();

            Debug.Log($"BattleBrain: Advanced systems cleaned up (exitType: {exitType})");
        }

        #endregion

        #region Event Handlers

        private void HandleUnitTakesAnotherTurn(CharacterInstance unit)
        {
            if (_battleGameObject?.Context == null)
            {
                Debug.LogWarning(
                    "BattleBrain: Cannot grant another turn - BattleContext not available"
                );
                return;
            }

            _battleGameObject.Context.UnitTakingAnotherTurn = unit;
            _battleGameObject.Context.AnotherTurnGranted = true;

            Debug.Log($"BattleBrain: {unit.CharacterTemplate.DisplayName} will take another turn");
        }

        private void HandleCriticalHit(CharacterInstance unit)
        {
            if (_battleGameObject?.Context == null)
            {
                Debug.LogWarning(
                    "BattleBrain: Cannot set critical hit - BattleContext not available"
                );
                return;
            }

            _battleGameObject.Context.IsCriticalHit = true;
            _battleGameObject.Context.CriticalHitUnit = unit;

            Debug.Log($"BattleBrain: {unit.CharacterTemplate.DisplayName} triggered critical hit");
        }

        private void HandleWeaponUsesChanged(CharacterInstance unit, int usesChange)
        {
            var inventory = unit.InventoryInstance;
            if (inventory == null)
            {
                Debug.LogWarning(
                    $"BattleBrain: {unit.CharacterTemplate.DisplayName} has no inventory"
                );
                return;
            }

            int weaponIndex = inventory.GetEquippedWeaponIndex();
            if (weaponIndex == -1)
            {
                Debug.LogWarning(
                    $"BattleBrain: {unit.CharacterTemplate.DisplayName} has no equipped weapon"
                );
                return;
            }

            var equippedWeapon = inventory.Items()[weaponIndex];
            if (equippedWeapon == null)
                return;

            if (usesChange > 0)
            {
                equippedWeapon.Repair(usesChange);
                Debug.Log(
                    $"BattleBrain: Restored {usesChange} uses to {unit.CharacterTemplate.DisplayName}'s weapon"
                );
            }
            else if (usesChange < 0)
            {
                for (int i = 0; i < Mathf.Abs(usesChange); i++)
                {
                    equippedWeapon.Use();
                }
                Debug.Log(
                    $"BattleBrain: Reduced {Mathf.Abs(usesChange)} uses from {unit.CharacterTemplate.DisplayName}'s weapon"
                );
            }
        }

        private void HandleItemStolen(CharacterInstance thief, CharacterInstance target)
        {
            Debug.Log(
                $"BattleBrain: {thief.CharacterTemplate.DisplayName} attempts to steal from {target.CharacterTemplate.DisplayName}"
            );

            // Get target's inventory
            var targetInventory = target.InventoryInstance;
            if (
                targetInventory == null
                || targetInventory.InventoryItems == null
                || targetInventory.InventoryItems.Count == 0
            )
            {
                Debug.Log("BattleBrain: Target has no items to steal");
                return;
            }

            // Get thief's inventory
            var thiefInventory = thief.InventoryInstance;
            if (thiefInventory == null || thiefInventory.IsFull)
            {
                Debug.Log("BattleBrain: Thief's inventory is full or unavailable");
                return;
            }

            // Find most valuable stealable item
            Objects.ObjectItemInstance bestItem = null;
            int bestValue = -1;

            foreach (var item in targetInventory.InventoryItems)
            {
                if (item == null || item.Template == null)
                    continue;
                if (item.Template.IsUnequippable)
                    continue;
                if (targetInventory.IsItemEquipped(item))
                    continue;

                int itemValue = item.Template.BasePrice;
                if (itemValue > bestValue)
                {
                    bestValue = itemValue;
                    bestItem = item;
                }
            }

            if (bestItem == null)
            {
                Debug.Log("BattleBrain: No stealable items found on target");
                return;
            }

            // Perform the steal
            targetInventory.RemoveFromInventory(bestItem);
            thiefInventory.AddToInventory(bestItem);

            Debug.Log(
                $"BattleBrain: {thief.CharacterTemplate.DisplayName} stole {bestItem.Template.name} from {target.CharacterTemplate.DisplayName}!"
            );

            // Publish transfer event
            _brain?.inventoryBrain?.TransferItem(bestItem, thiefInventory);
        }

        #endregion
    }
}
