using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Characters.Components;
using Turnroot.Gameplay.Objects;
using Turnroot.GameSettings;

namespace Turnroot.Gameplay.Brain
{
    public partial class CharactersBrain : BrainComponent
    {
        // -----------------------------------------------------------------------
        // Cache — invalidated by brain item events; recomputed lazily on next read
        // -----------------------------------------------------------------------

        private bool? _blacksmithWorkCache;

        /// <summary>
        /// Cached result of <see cref="HasBlacksmithWork"/>. Invalidated automatically
        /// whenever a relevant brain item event fires (deposit, withdraw, buy, repair,
        /// forge, discard, sell, or transfer). Recomputed on the next read.
        /// </summary>
        public bool BlacksmithWorkAvailable => _blacksmithWorkCache ??= HasBlacksmithWork();

        // One invalidation handler per distinct event signature.
        private void InvalidateBlacksmithWork(ObjectItemInstance _) => _blacksmithWorkCache = null;

        private void InvalidateBlacksmithWork(ObjectItemInstance _, int __) =>
            _blacksmithWorkCache = null;

        private void InvalidateBlacksmithWork(ObjectItemInstance _, ObjectItem __) =>
            _blacksmithWorkCache = null;

        private void InvalidateBlacksmithWork(
            ObjectItemInstance _,
            CharacterInventoryInstance __
        ) => _blacksmithWorkCache = null;

        partial void SubscribeBlacksmithItemEvents()
        {
            _brain.OnItemDeposited += InvalidateBlacksmithWork;
            _brain.OnItemBroken += InvalidateBlacksmithWork;
            _brain.OnItemDiscarded += InvalidateBlacksmithWork;
            _brain.OnItemSold += InvalidateBlacksmithWork;
            _brain.OnItemRepaired += InvalidateBlacksmithWork;
            _brain.OnItemForged += InvalidateBlacksmithWork;
            _brain.OnItemBought += InvalidateBlacksmithWork;
            _brain.OnItemWithdrawn += InvalidateBlacksmithWork;
            _brain.OnItemTransferred += InvalidateBlacksmithWork;
        }

        partial void UnsubscribeBlacksmithItemEvents()
        {
            _brain.OnItemDeposited -= InvalidateBlacksmithWork;
            _brain.OnItemBroken -= InvalidateBlacksmithWork;
            _brain.OnItemDiscarded -= InvalidateBlacksmithWork;
            _brain.OnItemSold -= InvalidateBlacksmithWork;
            _brain.OnItemRepaired -= InvalidateBlacksmithWork;
            _brain.OnItemForged -= InvalidateBlacksmithWork;
            _brain.OnItemBought -= InvalidateBlacksmithWork;
            _brain.OnItemWithdrawn -= InvalidateBlacksmithWork;
            _brain.OnItemTransferred -= InvalidateBlacksmithWork;
        }

        // -----------------------------------------------------------------------
        // Scan — the actual work; only called when the cache is cold
        // -----------------------------------------------------------------------

        /// <summary>
        /// Returns <c>true</c> if the player has at least one item that the blacksmith can
        /// service (repair or forge), based on the current <see cref="GameplayGeneralSettings"/>.
        ///
        /// Scans both character inventories (via the roster) and the shared storehouse.
        /// Prefer <see cref="BlacksmithWorkAvailable"/> for cached reads.
        /// </summary>
        public bool HasBlacksmithWork()
        {
            var settings = GameplayGeneralSettings.Instance;
            bool canRepair = settings != null && settings.WeaponsCanBeRepaired;
            bool canForge = settings != null && settings.WeaponsCanBeForged;

            if (!canRepair && !canForge)
            {
                return false;
            }

            // --- Character inventories ---
            var rosterInstance = _gamewideContextBrain?.GetPersistentPlayerTeamRosterInstance();
            if (rosterInstance != null)
            {
                foreach (var character in rosterInstance.Instances ?? new List<CharacterInstance>())
                {
                    if (character?.InventoryInstance == null)
                    {
                        continue;
                    }

                    foreach (
                        var item in character.InventoryInstance.InventoryItems
                            ?? new List<ObjectItemInstance>()
                    )
                    {
                        if (item == null)
                        {
                            continue;
                        }

                        if (canRepair && item.IsRepairableWeaponAccessoryOrShield())
                        {
                            return true;
                        }

                        if (canForge && item.IsForgeableWeaponOrMagic())
                        {
                            return true;
                        }
                    }
                }
            }

            // --- Storehouse ---
            var storehouseItems = _brain.storehouseBrain?.GetStoredItems();
            if (storehouseItems != null)
            {
                foreach (var item in storehouseItems)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    if (canRepair && item.IsRepairableWeaponAccessoryOrShield())
                    {
                        return true;
                    }

                    if (canForge && item.IsForgeableWeaponOrMagic())
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
