using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Events;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class BattleBrain : BrainComponent
    {
        protected override EventPriority GetSubscriptionPriority() => EventPriority.Highest;

        protected override void SubscribeToBrainEvents()
        {
            _brain.OnBattleStarted += HandleStartBattle;
            _brain.OnBattleCompleted += HandleExitBattle;
            _brain.OnUnitTakesAnotherTurn += HandleUnitTakesAnotherTurn;
            _brain.OnUnitFinishedMovingAfterAction += HandleUnitFinishedMovingAfterAction;
            _brain.OnCriticalHit += HandleCriticalHit;
            _brain.OnWeaponUsesChanged += HandleWeaponUsesChanged;
            _brain.OnItemStolen += HandleItemStolen;
            _brain.OnTurnEnded += HandleTurnEndStatusEffects;
            _brain.OnUnitDefeated += HandleUnitDefeated;
            _brain.OnUnitMoved += HandleUnitMoved;
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            _brain.OnBattleStarted -= HandleStartBattle;
            _brain.OnBattleCompleted -= HandleExitBattle;
            _brain.OnUnitTakesAnotherTurn -= HandleUnitTakesAnotherTurn;
            _brain.OnUnitFinishedMovingAfterAction -= HandleUnitFinishedMovingAfterAction;
            _brain.OnCriticalHit -= HandleCriticalHit;
            _brain.OnWeaponUsesChanged -= HandleWeaponUsesChanged;
            _brain.OnItemStolen -= HandleItemStolen;
            _brain.OnTurnEnded -= HandleTurnEndStatusEffects;
            _brain.OnUnitDefeated -= HandleUnitDefeated;
            _brain.OnUnitMoved -= HandleUnitMoved;
        }

        #region Event Handlers

        private void HandleUnitMoved(CharacterInstance unit, Vector2Int targetPosition) =>
            // Rebuild cached unit positions in BattleContext whenever a unit moves
            BattleObject.Context.InvalidateUnitPositionCache();

        private void HandleUnitDefeated(CharacterInstance unit) => BattleObject.Context.InvalidateUnitPositionCache();

        private void HandleUnitTakesAnotherTurn(CharacterInstance unit)
        {
            if (BattleObject?.Context == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    "BattleBrain: Cannot grant another turn - BattleContext not available"
                );
#endif
                return;
            }

            BattleObject.Context.Flags.ActiveUnitFlags.Unit = unit;
            BattleObject.Context.Flags.ActiveUnitFlags.AnotherTurnGranted = true;

#if UNITY_EDITOR
            Debug.Log($"BattleBrain: {unit.CharacterTemplate.DisplayName} will take another turn");
#endif
        }

        private void HandleUnitFinishedMovingAfterAction(CharacterInstance unit)
        {
            if (BattleObject?.Context == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    "BattleBrain: Cannot set finish moving after action - BattleContext not available"
                );
#endif
                return;
            }

            BattleObject.Context.Flags.ActiveUnitFlags.Unit = unit;
            BattleObject.Context.Flags.ActiveUnitFlags.CanFinishMovingAfterAction = true;
#if UNITY_EDITOR
            Debug.Log(
                $"BattleBrain: {unit.CharacterTemplate.DisplayName} can finish moving after action"
            );
#endif
        }

        private void HandleCriticalHit(CharacterInstance unit)
        {
            if (BattleObject?.Context == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    "BattleBrain: Cannot set critical hit - BattleContext not available"
                );
#endif
                return;
            }

            BattleObject.Context.Flags.ActiveUnitFlags.WillCriticalHit = true;
            BattleObject.Context.Flags.ActiveUnitFlags.Unit = unit;

#if UNITY_EDITOR
            Debug.Log($"BattleBrain: {unit.CharacterTemplate.DisplayName} triggered critical hit");
#endif
        }

        private void HandleWeaponUsesChanged(CharacterInstance unit, int usesChange)
        {
            var inventory = unit.InventoryInstance;
            if (inventory == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    $"BattleBrain: {unit.CharacterTemplate.DisplayName} has no inventory"
                );
#endif
                return;
            }

            int weaponIndex = inventory.GetEquippedWeaponIndex();
            if (weaponIndex == -1)
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    $"BattleBrain: {unit.CharacterTemplate.DisplayName} has no equipped weapon"
                );
#endif
                return;
            }

            var equippedWeapon = inventory.Items()[weaponIndex];
            if (equippedWeapon == null)
            {
                return;
            }

            if (usesChange > 0)
            {
                equippedWeapon.Repair(usesChange);
#if UNITY_EDITOR
                Debug.Log(
                    $"BattleBrain: Restored {usesChange} uses to {unit.CharacterTemplate.DisplayName}'s weapon"
                );
#endif
            }
            else if (usesChange < 0)
            {
                for (int i = 0; i < Mathf.Abs(usesChange); i++)
                {
                    equippedWeapon.Use();
                }
#if UNITY_EDITOR
                Debug.Log(
                    $"BattleBrain: Reduced {Mathf.Abs(usesChange)} uses from {unit.CharacterTemplate.DisplayName}'s weapon"
                );
#endif
            }
        }

        private void HandleItemStolen(CharacterInstance thief, CharacterInstance target)
        {
#if UNITY_EDITOR
            Debug.Log(
                $"BattleBrain: {thief.CharacterTemplate.DisplayName} attempts to steal from {target.CharacterTemplate.DisplayName}"
            );
#endif

            // Get target's inventory
            var targetInventory = target.InventoryInstance;
            if (
                targetInventory == null
                || targetInventory.InventoryItems == null
                || targetInventory.InventoryItems.Count == 0
            )
            {
#if UNITY_EDITOR
                Debug.Log("BattleBrain: Target has no items to steal");
#endif
                return;
            }

            // Get thief's inventory
            var thiefInventory = thief.InventoryInstance;
            if (thiefInventory == null || thiefInventory.IsFull)
            {
#if UNITY_EDITOR
                Debug.Log("BattleBrain: Thief's inventory is full or unavailable");
#endif
                return;
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
#if UNITY_EDITOR
                Debug.Log("BattleBrain: No stealable items found on target");
#endif
                return;
            }

            // Perform the steal
            targetInventory.RemoveFromInventory(bestItem);
            thiefInventory.AddToInventory(bestItem);

#if UNITY_EDITOR
            Debug.Log(
                $"BattleBrain: {thief.CharacterTemplate.DisplayName} stole {bestItem.Template.name} from {target.CharacterTemplate.DisplayName}!"
            );
#endif

            // Publish transfer event
            _brain.inventoryBrain.TransferItem(bestItem, thiefInventory);
        }

        #endregion
    }
}
