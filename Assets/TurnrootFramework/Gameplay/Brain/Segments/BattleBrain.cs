using Turnroot.Characters;
using Turnroot.Characters.Components;
using Turnroot.Gameplay.Combat;
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

        protected override void SubscribeToBrainEvents()
        {
            _brain.OnStartBattle += HandleStartBattle;
            _brain.OnExitBattle += HandleExitBattle;
            _brain.OnUnitTakesAnotherTurn += HandleUnitTakesAnotherTurn;
            _brain.OnCriticalHit += HandleCriticalHit;
            _brain.OnWeaponUsesChanged += HandleWeaponUsesChanged;
            _brain.OnItemStolen += HandleItemStolen;
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            _brain.OnStartBattle -= HandleStartBattle;
            _brain.OnExitBattle -= HandleExitBattle;
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

        private void HandleStartBattle()
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
                            break;
                        }
                    }
                }
            }
        }

        private void HandleExitBattle(BattleExitType exitType)
        {
            Debug.Log($"BattleBrain: Handling ExitBattle event with exit type: {exitType}.");
            _battleGameObject?.ClearBattleRosters();
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
            // TODO: Implement stealing logic - select random item from target and transfer to thief
            // This would need to interact with InventoryBrain to handle the actual transfer
            Debug.Log(
                $"BattleBrain: {thief.CharacterTemplate.DisplayName} attempts to steal from {target.CharacterTemplate.DisplayName}"
            );

            // For now, just log the attempt. Full implementation would:
            // 1. Check target's inventory for stealable items
            // 2. Roll for steal success based on stats
            // 3. Transfer item if successful via InventoryBrain
        }
    }
}
