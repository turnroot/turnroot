using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Characters.Components;
using Turnroot.Gameplay.Objects;
using Turnroot.GameSettings;

namespace Turnroot.Gameplay.Brain
{
    public partial class CharactersBrain : BrainComponent
    {
        private bool? _repairWorkCache;
        private bool? _forgeWorkCache;

        public bool BlacksmithWorkAvailable => RepairWorkAvailable || ForgeWorkAvailable;
        public bool RepairWorkAvailable => _repairWorkCache ??= HasRepairWork();
        public bool ForgeWorkAvailable => _forgeWorkCache ??= HasForgeWork();

        private void InvalidateAllBlacksmithCaches()
        {
            _repairWorkCache = null;
            _forgeWorkCache = null;
        }
        private void InvalidateBlacksmithWork(ObjectItemInstance _) =>
            InvalidateAllBlacksmithCaches();

        private void InvalidateBlacksmithWork(ObjectItemInstance _, int __) =>
            InvalidateAllBlacksmithCaches();

        private void InvalidateBlacksmithWork(ObjectItemInstance _, ObjectItem __) =>
            InvalidateAllBlacksmithCaches();

        private void InvalidateBlacksmithWork(
            ObjectItemInstance _,
            CharacterInventoryInstance __
        ) => InvalidateAllBlacksmithCaches();

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

        public bool HasRepairWork()
        {
            var settings = GameplayGeneralSettings.Instance;
            return settings != null
                && settings.WeaponsCanBeRepaired
                && ScanAllItems(item => item.IsRepairableWeaponAccessoryOrShield());
        }

        public bool HasForgeWork()
        {
            var settings = GameplayGeneralSettings.Instance;
            return settings != null
                && settings.WeaponsCanBeForged
                && ScanAllItems(item => item.IsForgeableWeaponOrMagic());
        }

        private bool ScanAllItems(System.Func<ObjectItemInstance, bool> predicate)
        {
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
                        if (item != null && predicate(item))
                        {
                            return true;
                        }
                    }
                }
            }

            var storehouseItems = _brain.storehouseBrain?.GetStoredItems();
            if (storehouseItems != null)
            {
                foreach (var item in storehouseItems)
                {
                    if (item != null && predicate(item))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
