using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Commands;
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
            _brain.OnCriticalHit += HandleCriticalHit;
            _brain.OnWeaponUsesChanged += HandleWeaponUsesChanged;
            _brain.OnItemStolen += HandleItemStolen;
            // Hook into turn end for status effect expiry handling
            _brain.OnTurnEnded += HandleTurnEndStatusEffects;
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            _brain.OnBattleStarted -= HandleStartBattle;
            _brain.OnBattleCompleted -= HandleExitBattle;
            _brain.OnUnitTakesAnotherTurn -= HandleUnitTakesAnotherTurn;
            _brain.OnCriticalHit -= HandleCriticalHit;
            _brain.OnWeaponUsesChanged -= HandleWeaponUsesChanged;
            _brain.OnItemStolen -= HandleItemStolen;
            // Unhook turn end handler
            _brain.OnTurnEnded -= HandleTurnEndStatusEffects;
        }

        #region Event Handlers


        private void HandleUnitTakesAnotherTurn(CharacterInstance unit)
        {
            if (BattleObject?.Context == null)
            {
                Debug.LogWarning(
                    "BattleBrain: Cannot grant another turn - BattleContext not available"
                );
                return;
            }

            BattleObject.Context.Flags.UnitTakingAnotherTurn = unit;
            BattleObject.Context.Flags.AnotherTurnGranted = true;

#if UNITY_EDITOR
            Debug.Log($"BattleBrain: {unit.CharacterTemplate.DisplayName} will take another turn");
#endif
        }

        private void HandleCriticalHit(CharacterInstance unit)
        {
            if (BattleObject?.Context == null)
            {
                Debug.LogWarning(
                    "BattleBrain: Cannot set critical hit - BattleContext not available"
                );
                return;
            }

            BattleObject.Context.Flags.IsCriticalHit = true;
            BattleObject.Context.Flags.CriticalHitUnit = unit;

#if UNITY_EDITOR
            Debug.Log($"BattleBrain: {unit.CharacterTemplate.DisplayName} triggered critical hit");
#endif
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
            {
                return;
            }

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

            Debug.Log(
                $"BattleBrain: {thief.CharacterTemplate.DisplayName} stole {bestItem.Template.name} from {target.CharacterTemplate.DisplayName}!"
            );

            // Publish transfer event
            _brain.inventoryBrain.TransferItem(bestItem, thiefInventory);
        }

        #endregion
    }
}
