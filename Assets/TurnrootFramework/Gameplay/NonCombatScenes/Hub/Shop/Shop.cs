using System;
using System.Collections.Generic;
using Turnroot.Conversations;
using Turnroot.Gameplay.Brain;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Shop
{
    using Turnroot.Gameplay.NonCombatScenes.Hub.Abstract;

    [RequireComponent(typeof(ShopUi))]
    public class Shop : HubVendor
    {
        public ShopUi Ui => GetComponent<ShopUi>();
        public ShopItem[] ItemsStocked;
        private Dictionary<ShopItem, int> currentStock = new();

        public string ShopName;
        public string ShopDescription;

        public string SoldOutDialogueText;

        public OneShotDialogue[] ShopKeeperSellsDialogues;
        public OneShotDialogue[] ShopKeeperBuysDialogues;

        private OneShot[] SellDialogueConversations;
        private OneShot[] BuyDialogueConversations;

        protected override void Awake()
        {
            base.Awake();

            var speakerName = Shopkeeper != null ? Shopkeeper.DisplayName : "???";
            SellDialogueConversations =
                audioBrain?.ConvertToOneShots(ShopKeeperSellsDialogues, speakerName)
                ?? Array.Empty<OneShot>();
            BuyDialogueConversations =
                audioBrain?.ConvertToOneShots(ShopKeeperBuysDialogues, speakerName)
                ?? Array.Empty<OneShot>();
        }

        public void NotifyShopVisited()
        {
            brain ??= FindFirstObjectByType<Brain.Brain>();
            audioBrain ??= brain?.audioBrain;
            brain?.PublishShopVisited(this);

            // Guard: nothing to sell or show if there are no items
            if (ItemsStocked == null || ItemsStocked.Length == 0)
            {
                $"Shop '{name}': No items stocked, skipping display.".LogInfo();
                return;
            }

            // Ensure shop stock is refreshed from LongTermMemory on every visit,
            // even on the same calendar day.
            if (brain?.ltm != null)
            {
                var date = brain.ltm.GetGameDate();
                _ = RefreshShopForNewDay(date);
            }

            NotifyVendorVisited(
                () => TryGetComponent<ShopUi>(out var ui) ? ui : null,
                shopUi => shopUi.RefreshShopDisplay(),
                "Shop"
            );
        }

        public void NotifyShopExited()
        {
            brain ??= FindFirstObjectByType<Brain.Brain>();
            brain?.PublishShopExited(this);

            NotifyVendorExited(
                () => TryGetComponent<ShopUi>(out var ui) ? ui : null,
                shopUi => shopUi.ShopUiFade.Hide(),
                "Shop"
            );
        }

        public override void HandleConfirmInput(string action) => Ui?.HandlePurchaseConfirmationInput();

        public override void HandleBackInput(string action)
        {
            if (action is not "Back" and not InputActionConstants.Cancel)
            {
                return;
            }

            NotifyShopExited();
        }

        public void NotifyShopkeeperBuys(ShopItem[] itemsBought)
        {
            NotifyTransaction(
                itemsBought ?? Array.Empty<ShopItem>(),
                i => brain?.PublishShopkeeperBuys(this, i),
                BuyDialogueConversations,
                $"Shop '{name}': No buy dialogue to play"
            );
        }

        public void NotifyShopkeeperSells(ShopItem itemSold)
        {
            NotifyTransaction(
                itemSold,
                i => brain?.PublishShopkeeperSells(this, i),
                SellDialogueConversations,
                $"Shop '{name}': No sell dialogue to play"
            );
        }

        [Tooltip(
            "If this shop is not unlocked yet, set to false and it will not be available until this is true"
        )]
        public bool ShopReadyForBusiness = true;

        public bool ShopOpen(GameDate currentDay)
        {
            int dayIndex = currentDay.day % DaysOpenCycle.Length;
            return DaysOpenCycle[dayIndex] && ShopReadyForBusiness;
        }

        [Tooltip(
            "The shop will loop through the DaysOpenCycle array over the length of the array. The default is a week, but you can use any length and variety of cycle"
        )]
        public bool[] DaysOpenCycle = new bool[7] { true, true, true, true, true, true, true };
        public bool WillBuy = true;

        public string RefreshShopForNewDay(GameDate currentDay)
        {
            if (ItemsStocked == null || ItemsStocked.Length == 0)
            {
                return "";
            }

            var brainRef = brain ?? FindFirstObjectByType<Brain.Brain>();
            bool hasSavedShopStock = HubDayStateStore.HasShopStock(name);
            bool isDailyUpdateAlreadyProcessed = HubDayStateStore.HasProcessedDailyUpdates;

            // If this shop has no existing persistent stock data, initialize normal items to max
            if (!hasSavedShopStock && brainRef != null)
            {
                for (int i = 0; i < ItemsStocked.Length; i++)
                {
                    var item = ItemsStocked[i];
                    if (item.RareItem)
                    {
                        continue;
                    }

                    item.CurrentStatus.AvailableQuantity = item.MaxQuantity;
                    ItemsStocked[i] = item; // Ensure struct value is updated in the array.
                    currentStock[item] = item.MaxQuantity;

                    string itemName = item.Item != null ? item.Item.name : string.Empty;
                    HubDayStateStore.SetShopItemQuantity(
                        brainRef,
                        name,
                        itemName,
                        item.MaxQuantity
                    );
                }
            }
            else if (brainRef != null)
            {
                for (int i = 0; i < ItemsStocked.Length; i++)
                {
                    var item = ItemsStocked[i];
                    string itemName = item.Item != null ? item.Item.name : string.Empty;
                    int persistedQuantity = HubDayStateStore.GetShopItemQuantity(
                        name,
                        itemName,
                        item.MaxQuantity
                    );

                    item.CurrentStatus.AvailableQuantity = persistedQuantity;
                    ItemsStocked[i] = item;
                    currentStock[item] = persistedQuantity;
                }
            }

            var totalStock = 0;
            foreach (ShopItem item in ItemsStocked)
            {
                // always evaluate sale status every display/refresh
                item.IsOnSale(currentDay);

                if (!isDailyUpdateAlreadyProcessed)
                {
                    item.RestockIfNeeded(currentDay);
                }

                var status = item.CurrentStatus;
                currentStock[item] = status.AvailableQuantity;
                totalStock += status.AvailableQuantity;

                // Persist the updated quantity into HubDayStateStore for this day.
                if (brainRef != null)
                {
                    string itemName = item.Item != null ? item.Item.name : string.Empty;
                    HubDayStateStore.SetShopItemQuantity(
                        brainRef,
                        name,
                        itemName,
                        status.AvailableQuantity
                    );
                }

                // Only show rare item notification when this call is the daily update path (not on repeated same-day slip-ins)
                if (!isDailyUpdateAlreadyProcessed && item.RareItem && status.AvailableQuantity > 0)
                {
                    return $"A rare item is in stock at";
                }
            }

            if (totalStock == 0)
            {
                if (SoldOutDialogueText != null)
                {
                    for (int i = 0; i < WelcomeDialogues.Length; i++)
                    {
                        var d = WelcomeDialogues[i];
                        d.Dialogue = SoldOutDialogueText;
                        WelcomeDialogues[i] = d;
                    }
                    WelcomeDialogueConversations =
                        audioBrain?.ConvertToOneShots(
                            WelcomeDialogues,
                            Shopkeeper != null ? Shopkeeper.DisplayName : "???"
                        ) ?? Array.Empty<OneShot>();
                }
            }
            return "";
        }
    }
}
