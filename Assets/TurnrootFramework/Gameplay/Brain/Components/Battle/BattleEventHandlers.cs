using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Gameplay.Combat.PreBattle;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class BattleBrain : BrainComponent
    {
        protected override EventPriority GetSubscriptionPriority() => EventPriority.Highest;

        protected override void SubscribeToBrainEvents()
        {
            _brain.OnPreBattlePrepare += HandlePreBattlePrepare;
            _brain.OnPreBattleCompleted += HandleStartBattle;
            _brain.OnBattleCompleted += HandleExitBattle;
            _brain.OnUnitTakesAnotherTurn += HandleUnitTakesAnotherTurn;
            _brain.OnUnitFinishedMovingAfterAction += HandleUnitFinishedMovingAfterAction;
            _brain.OnCriticalHit += HandleCriticalHit;
            _brain.OnWeaponUsesChanged += HandleWeaponUsesChanged;
            _brain.OnItemStolen += HandleItemStolen;
            _brain.OnTurnEnded += HandleTurnEndStatusEffects;
            _brain.OnTurnBegin += HandleTurnBeginEvent;
            _brain.OnUnitDefeated += HandleUnitDefeated;
            _brain.OnUnitMoved += HandleUnitMoved;
            _brain.OnLongTermMemoryInitialized += InitializeLTMDependentData;
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            _brain.OnPreBattlePrepare -= HandlePreBattlePrepare;
            _brain.OnPreBattleCompleted -= HandleStartBattle;
            _brain.OnBattleCompleted -= HandleExitBattle;
            _brain.OnUnitTakesAnotherTurn -= HandleUnitTakesAnotherTurn;
            _brain.OnUnitFinishedMovingAfterAction -= HandleUnitFinishedMovingAfterAction;
            _brain.OnCriticalHit -= HandleCriticalHit;
            _brain.OnWeaponUsesChanged -= HandleWeaponUsesChanged;
            _brain.OnItemStolen -= HandleItemStolen;
            _brain.OnTurnEnded -= HandleTurnEndStatusEffects;
            _brain.OnTurnBegin -= HandleTurnBeginEvent;
            _brain.OnUnitDefeated -= HandleUnitDefeated;
            _brain.OnUnitMoved -= HandleUnitMoved;
            _brain.OnLongTermMemoryInitialized -= InitializeLTMDependentData;
        }

        #region Event Handlers

        private void HandlePreBattlePrepare() => HandlePreBattlePrepareLogic();

        private void HandleUnitTakesAnotherTurn(CharacterInstance unit) =>
            HandleUnitTakesAnotherTurnLogic(unit);

        private void HandleUnitFinishedMovingAfterAction(CharacterInstance unit) =>
            HandleUnitFinishedMovingAfterActionLogic(unit);

        private void HandleCriticalHit(CharacterInstance unit) => HandleCriticalHitLogic(unit);

        private void HandleWeaponUsesChanged(CharacterInstance unit, int usesChange) =>
            HandleWeaponUsesChangedLogic(unit, usesChange);

        private void HandleItemStolen(CharacterInstance thief, CharacterInstance target) =>
            HandleItemStolenLogic(thief, target);

        private OperationResult HandlePreBattlePrepareLogic()
        {
            // Find a preparation object in the scene and cache it for pre-battle UI access
            var prep = FindFirstObjectByType<BattlePreparationObject>();
            if (prep == null)
            {
                var all = Resources.FindObjectsOfTypeAll<BattlePreparationObject>();
                foreach (var p in all)
                {
                    if (p != null && p.gameObject != null && p.gameObject.scene.isLoaded)
                    {
                        prep = p;
                        break;
                    }
                }
            }
            if (prep == null)
            {
                return OperationResult.Failure("No BattlePreparationObject found in scene");
            }

            PreparationObject = prep;
            PreparationObject.Initialize(_brain);
            return OperationResult.Successful();
        }

        private void HandleUnitMoved(CharacterInstance unit, Vector2Int targetPosition)
        {
            // Invalidate per-unit tile cache when a unit moves
            BattleObject.Context.InvalidateUnitTileCache(unit);
        }

        private void HandleUnitDefeated(CharacterInstance unit)
        {
            BattleObject.Context.InvalidateUnitTileCache(unit);
        }

        private OperationResult HandleUnitTakesAnotherTurnLogic(CharacterInstance unit)
        {
            if (BattleObject?.Context == null)
            {
                return OperationResult.Failure("BattleContext not available");
            }

            BattleObject.Context.Flags.ActiveUnitFlags.Unit = unit;
            BattleObject.Context.Flags.ActiveUnitFlags.AnotherTurnGranted = true;

            $"BattleBrain: {unit.CharacterTemplate.DisplayName} will take another turn".LogInfo();

            return OperationResult.Successful();
        }

        private OperationResult HandleUnitFinishedMovingAfterActionLogic(CharacterInstance unit)
        {
            if (BattleObject?.Context == null)
            {
                return OperationResult.Failure(
                    "BattleBrain: Cannot set finish moving after action: BattleContext not available"
                );
            }

            BattleObject.Context.Flags.ActiveUnitFlags.Unit = unit;
            BattleObject.Context.Flags.ActiveUnitFlags.CanFinishMovingAfterAction = true;

            $"BattleBrain: {unit.CharacterTemplate.DisplayName} can finish moving after action".LogInfo();
            return OperationResult.Successful();
        }

        private void HandleTurnBeginEvent() =>
            // Invalidate all tile caches at start of each turn to ensure AI and UI recompute
            BattleObject.Context.InvalidateAllTileCaches();

        private OperationResult HandleCriticalHitLogic(CharacterInstance unit)
        {
            if (BattleObject?.Context == null)
            {
                return OperationResult.Failure(
                    "BattleBrain: Cannot set critical hit - BattleContext not available"
                );
            }

            BattleObject.Context.Flags.ActiveUnitFlags.WillCriticalHit = true;
            BattleObject.Context.Flags.ActiveUnitFlags.Unit = unit;

            $"BattleBrain: {unit.CharacterTemplate.DisplayName} will perform a critical hit".LogInfo();
            return OperationResult.Successful();
        }

        private OperationResult HandleWeaponUsesChangedLogic(CharacterInstance unit, int usesChange)
        {
            var inventory = unit.InventoryInstance;
            if (inventory == null)
            {
                return OperationResult.Failure(
                    "BattleBrain: Cannot change weapon uses - inventory not available"
                );
            }

            int weaponIndex = inventory.GetEquippedWeaponIndex();
            if (weaponIndex == -1)
            {
                $"BattleBrain: {unit.CharacterTemplate.DisplayName} has no equipped weapon".LogInfo();
                return OperationResult.Failure(
                    "BattleBrain: Cannot change weapon uses - no equipped weapon"
                );
            }

            var equippedWeapon = inventory.Items()[weaponIndex];
            if (equippedWeapon == null)
            {
                return OperationResult.Failure(
                    "BattleBrain: Cannot change weapon uses - equipped weapon is null"
                );
            }

            if (usesChange > 0)
            {
                equippedWeapon.Repair(usesChange);

                $"BattleBrain: Restored {usesChange} uses to {unit.CharacterTemplate.DisplayName}'s weapon".LogInfo();
            }
            else if (usesChange < 0)
            {
                for (int i = 0; i < Mathf.Abs(usesChange); i++)
                {
                    equippedWeapon.Use();
                }

                $"BattleBrain: Reduced {Mathf.Abs(usesChange)} uses from {unit.CharacterTemplate.DisplayName}'s weapon".LogInfo();
            }

            return OperationResult.Successful();
        }

        private OperationResult HandleItemStolenLogic(
            CharacterInstance thief,
            CharacterInstance target
        )
        {
            $"BattleBrain: {thief.CharacterTemplate.DisplayName} attempts to steal from {target.CharacterTemplate.DisplayName}".LogInfo();

            // Get target's inventory
            var targetInventory = target.InventoryInstance;
            if (
                targetInventory == null
                || targetInventory.InventoryItems == null
                || targetInventory.InventoryItems.Count == 0
            )
            {
                return OperationResult.Failure("BattleBrain: Target has no items to steal");
            }

            // Get thief's inventory
            var thiefInventory = thief.InventoryInstance;
            if (thiefInventory == null || thiefInventory.IsFull)
            {
                return OperationResult.Failure(
                    "BattleBrain: Thief's inventory is full or unavailable"
                );
            }

            // Find most valuable stealable item
            Objects.ObjectItemInstance bestItem = null;
            int bestValue = -1;

            foreach (var item in targetInventory.InventoryItems)
            {
                if (item == null || item.Template == null)
                {
                    continue;
                }

                if (item.Template.IsUnequippable)
                {
                    continue;
                }

                if (targetInventory.IsItemEquipped(item))
                {
                    continue;
                }

                int itemValue = item.Template.BasePrice;
                if (itemValue > bestValue)
                {
                    bestValue = itemValue;
                    bestItem = item;
                }
            }

            if (bestItem == null)
            {
                return OperationResult.Failure("BattleBrain: No stealable items found on target");
            }

            // Perform the steal
            var resRemove = targetInventory.RemoveFromInventory(bestItem);
            if (!resRemove.Success)
            {
                return resRemove;
            }

            var resAdd = thiefInventory.AddToInventory(bestItem);
            if (!resAdd.Success)
            {
                targetInventory.AddToInventory(bestItem);
                return OperationResult.Failure(
                    "BattleBrain: Failed to add stolen item to thief, restored to target"
                );
            }

            $"BattleBrain: {thief.CharacterTemplate.DisplayName} stole {bestItem.Template.name} from {target.CharacterTemplate.DisplayName}!".LogInfo();

            // Publish transfer event
            _brain.inventoryBrain.TransferItem(bestItem, thiefInventory);

            return OperationResult.Successful();
        }

        #endregion
    }
}
