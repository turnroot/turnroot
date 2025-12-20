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
    /// It is responsible for initializing the battle and managing turn order.
    /// </summary>
    public class BattleBrain : BrainComponent
    {
        private BattleGameObject _battleGameObject;

        public BattleGameObject BattleObject => _battleGameObject;
        private TurnRotisserie _turnRotisserie;

        // Accessor for current battle's rosters through BattleGameObject
        public RosterInstance PlayerTeamRoster => _battleGameObject?.PlayerTeamRoster;
        public RosterInstance EnemyTeamRoster => _battleGameObject?.EnemyTeamRoster;
        public RosterInstance ThirdPartyTeamRoster => _battleGameObject?.ThirdPartyTeamRoster;

        protected override void Awake()
        {
            base.Awake(); // Calls parent Awake which gets Brain and subscribes

            _turnRotisserie = GetComponent<TurnRotisserie>();
            if (_turnRotisserie == null)
            {
                _turnRotisserie = gameObject.AddComponent<TurnRotisserie>();
            }
            _turnRotisserie.Brain = _brain;
            Debug.Log("BattleBrain TurnRotisserie is ready.");
        }

        /// <summary>
        /// BattleBrain uses Highest priority because it manages critical battle state.
        /// This ensures roster updates complete before UI tries to display them.
        /// </summary>
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

        public void HandleStartBattle()
        {
            Debug.Log("BattleBrain: Handling StartBattle event.");

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    GameObject[] rootObjects = scene.GetRootGameObjects();
                    foreach (GameObject rootObject in rootObjects)
                    {
                        _battleGameObject = rootObject.GetComponentInChildren<BattleGameObject>();
                        if (_battleGameObject != null)
                        {
                            _battleGameObject.Brain = _brain;
                            _battleGameObject.ConnectToBrainEvents();
                            _battleGameObject.ConnectBattleConditionsToGamewideContextBrain();
                            Debug.Log(
                                $"BattleBrain: Found BattleGameObject in scene '{scene.name}'."
                            );
                            _turnRotisserie.HasThirdParty = _battleGameObject.HasThirdParty;

                            _battleGameObject.InitializeBattleRosters();
                            _battleGameObject.PopulateBattleRostersFromGamewideContext(
                                _brain.gamewideContextBrain
                            );

                            // Initialize advanced systems for the battle
                            InitializeBattleAdvancedSystems();

                            Brain.playerInputBrain.ScenePlayerController.EnemyAIHelper =
                                new BattleContextAIHelper(Brain.battleBrain.BattleObject.Context);
                            Debug.Log(
                                "BattleBrain: Initialized BattleContextAIHelper for enemy unit."
                            );
                            Brain.playerInputBrain.ScenePlayerController.EnemyAIHelper.InitializeAIControlledUnit(
                                Brain
                                    .playerInputBrain
                                    .ScenePlayerController
                                    .EnemyTestUnitView
                                    .CharacterDataInstance
                            ); // TODO: This is wildly wrong but will work for testing, fix later
                            Debug.Log("BattleBrain: AI Controlled Unit initialized.");
                            Brain
                                .playerInputBrain
                                .ScenePlayerController
                                .TestUnitView
                                .CharacterDataInstance
                                .MapGridPosition = Brain
                                .playerInputBrain
                                .ScenePlayerController
                                .TestUnitView
                                .CurrentGridCoordinates;
                            var enemyCoords = Brain
                                .playerInputBrain
                                .ScenePlayerController
                                .EnemyTestUnitView
                                .CurrentGridCoordinates;
                            var enemyGridPoint = _battleGameObject.Context.mapGrid.GetGridPoint(
                                enemyCoords.x,
                                enemyCoords.y
                            );
                            var GridPoint = _battleGameObject.Context.mapGrid.GetGridPoint(
                                Brain
                                    .playerInputBrain
                                    .ScenePlayerController
                                    .TestUnitView
                                    .CurrentGridCoordinates
                                    .x,
                                Brain
                                    .playerInputBrain
                                    .ScenePlayerController
                                    .TestUnitView
                                    .CurrentGridCoordinates
                                    .y
                            );
                            _battleGameObject.Context.mapGrid.SetOccupied(
                                enemyGridPoint,
                                Brain
                                    .playerInputBrain
                                    .ScenePlayerController
                                    .EnemyTestUnitView
                                    .CharacterDataInstance
                            );
                            _battleGameObject.Context.mapGrid.SetOccupied(
                                GridPoint,
                                Brain
                                    .playerInputBrain
                                    .ScenePlayerController
                                    .TestUnitView
                                    .CharacterDataInstance
                            );
                            _battleGameObject.Context.Targets.Add(
                                Brain
                                    .playerInputBrain
                                    .ScenePlayerController
                                    .TestUnitView
                                    .CharacterDataInstance
                            );
                            Debug.Log("BattleBrain: Player unit added to battle context targets.");
                            _battleGameObject.Context.UnitInstance.MapGridPosition = Brain
                                .playerInputBrain
                                .ScenePlayerController
                                .EnemyTestUnitView
                                .CurrentGridCoordinates;
                            Debug.Log(
                                "BattleBrain: Enemy unit position set to "
                                    + _battleGameObject.Context.UnitInstance.MapGridPosition
                                    + " in battle context."
                            );
                            Debug.Log(
                                "Enemy class: "
                                    + Brain
                                        .playerInputBrain
                                        .ScenePlayerController
                                        .EnemyTestUnitView
                                        .CharacterDataInstance
                                        .CurrentClass
                                        .ClassData
                                        .Identity
                                        .ClassName
                            );
                            _battleGameObject.Context.mapGrid.GetAllOccupiedPoints();
                            break;
                        }
                    }
                    if (_battleGameObject == null)
                    {
                        Debug.LogWarning(
                            $"BattleBrain: No BattleGameObject found in scene '{scene.name}'."
                        );
                    }
                }
            }
        }

        /// <summary>
        /// Initializes advanced systems (commands, snapshots) for the battle.
        /// </summary>
        private void InitializeBattleAdvancedSystems()
        {
            // Clear any previous battle's command history
            _brain.Commands?.Clear();

            // Take initial snapshot of battle state
            _brain.TakeSnapshot();

            Debug.Log("BattleBrain: Advanced systems initialized for battle.");
        }

        private void HandleExitBattle(BattleExitType exitType)
        {
            Debug.Log($"BattleBrain: Handling ExitBattle event with exit type: {exitType}.");

            // Clean up advanced systems
            CleanupBattleAdvancedSystems(exitType);

            _battleGameObject?.ClearBattleRosters();
        }

        /// <summary>
        /// Cleans up advanced systems after battle ends.
        /// </summary>
        private void CleanupBattleAdvancedSystems(BattleExitType exitType)
        {
            // Clear command history (unless we want to keep for replay)
            if (exitType != BattleExitType.Bookmark)
            {
                _brain.Commands?.Clear();
            }

            // Clear snapshots
            _brain.Snapshots?.Clear();

            Debug.Log($"BattleBrain: Advanced systems cleaned up (exitType: {exitType}).");
        }

        private void HandleUnitTakesAnotherTurn(CharacterInstance unit)
        {
            // Grant the unit another action by setting flags in battle context
            if (_battleGameObject?.Context != null)
            {
                _battleGameObject.Context.UnitTakingAnotherTurn = unit;
                _battleGameObject.Context.AnotherTurnGranted = true;
                Debug.Log(
                    $"BattleBrain: {unit.CharacterTemplate.DisplayName} will take another turn"
                );
            }
            else
            {
                Debug.LogWarning(
                    $"BattleBrain: Cannot grant another turn - BattleContext not available"
                );
            }
        }

        private void HandleCriticalHit(CharacterInstance unit)
        {
            // Store critical hit flag in battle context for current combat calculation
            if (_battleGameObject?.Context != null)
            {
                _battleGameObject.Context.IsCriticalHit = true;
                _battleGameObject.Context.CriticalHitUnit = unit;
                Debug.Log(
                    $"BattleBrain: {unit.CharacterTemplate.DisplayName} triggered critical hit"
                );
            }
        }

        private void HandleWeaponUsesChanged(CharacterInstance unit, int usesChange)
        {
            // Get the character's equipped weapon through inventory
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
                    $"BattleBrain: {unit.CharacterTemplate.DisplayName} has no equipped weapon to modify"
                );
                return;
            }

            var equippedWeapon = inventory.Items()[weaponIndex];
            if (equippedWeapon != null)
            {
                // Positive values restore uses, negative values reduce uses
                if (usesChange > 0)
                {
                    equippedWeapon.Repair(usesChange);
                    Debug.Log(
                        $"BattleBrain: Restored {usesChange} uses to {unit.CharacterTemplate.DisplayName}'s weapon"
                    );
                }
                else if (usesChange < 0)
                {
                    // Use the weapon multiple times to reduce durability
                    for (int i = 0; i < Mathf.Abs(usesChange); i++)
                    {
                        equippedWeapon.Use();
                    }
                    Debug.Log(
                        $"BattleBrain: Reduced {Mathf.Abs(usesChange)} uses from {unit.CharacterTemplate.DisplayName}'s weapon"
                    );
                }
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
                Debug.Log("BattleBrain: Target has no items to steal.");
                return;
            }

            // Get thief's inventory
            var thiefInventory = thief.InventoryInstance;
            if (thiefInventory == null || thiefInventory.IsFull)
            {
                Debug.Log("BattleBrain: Thief's inventory is full or unavailable.");
                return;
            }

            // Find the most valuable stealable item
            // Items must be: transferable (not unequippable) and not currently equipped
            Objects.ObjectItemInstance bestItem = null;
            int bestValue = -1;

            foreach (var item in targetInventory.InventoryItems)
            {
                if (item == null || item.Template == null)
                {
                    continue;
                }

                // Check if item can be transferred (not unequippable)
                if (item.Template.IsUnequippable)
                {
                    continue;
                }

                // Check if item is currently equipped
                if (targetInventory.IsItemEquipped(item))
                {
                    continue;
                }

                // Prefer higher value items (use BasePrice as value indicator)
                int itemValue = item.Template.BasePrice;
                if (itemValue > bestValue)
                {
                    bestValue = itemValue;
                    bestItem = item;
                }
            }

            if (bestItem == null)
            {
                Debug.Log("BattleBrain: No stealable items found on target.");
                return;
            }

            // Perform the steal - remove from target, add to thief
            targetInventory.RemoveFromInventory(bestItem);
            thiefInventory.AddToInventory(bestItem);

            Debug.Log(
                $"BattleBrain: {thief.CharacterTemplate.DisplayName} stole {bestItem.Template.name} from {target.CharacterTemplate.DisplayName}!"
            );

            // Publish transfer event through InventoryBrain
            _brain?.inventoryBrain?.TransferItem(bestItem, thiefInventory);
        }
    }
}
