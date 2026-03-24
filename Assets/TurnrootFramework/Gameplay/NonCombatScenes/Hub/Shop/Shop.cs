using System;
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Conversations;
using Turnroot.Gameplay.Brain;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Shop
{
    [RequireComponent(typeof(ShopUi))]
    public class Shop : MonoBehaviour
    {
        public ShopUi Ui => GetComponent<ShopUi>();
        public ShopItem[] ItemsStocked;
        private Dictionary<ShopItem, int> currentStock = new();

        public string ShopName;
        public string ShopDescription;
        public CharacterData Shopkeeper;

        public OneShotDialogue[] WelcomeDialogues;

        private OneShotDialogue[] cachedWelcomeDialogues;
        public OneShotDialogue[] ShopKeeperSellsDialogues;
        public OneShotDialogue[] ShopKeeperBuysDialogues;
        public OneShotDialogue[] FarewellDialogues;

        public string SoldOutDialogueText;

        private OneShot[] WelcomeDialogueConversations;
        private OneShot[] SellDialogueConversations;
        private OneShot[] BuyDialogueConversations;
        private OneShot[] FarewellDialogueConversations;

        [HideInInspector]
        public Brain.Brain brain;

        private AudioBrain audioBrain;

        public void Awake()
        {
            cachedWelcomeDialogues = WelcomeDialogues;

            brain ??= FindFirstObjectByType<Brain.Brain>();
            audioBrain = brain?.audioBrain;
            var speakerName = Shopkeeper != null ? Shopkeeper.DisplayName : "???";

            WelcomeDialogueConversations =
                audioBrain?.ConvertToOneShots(WelcomeDialogues, speakerName)
                ?? Array.Empty<OneShot>();
            SellDialogueConversations =
                audioBrain?.ConvertToOneShots(ShopKeeperSellsDialogues, speakerName)
                ?? Array.Empty<OneShot>();
            BuyDialogueConversations =
                audioBrain?.ConvertToOneShots(ShopKeeperBuysDialogues, speakerName)
                ?? Array.Empty<OneShot>();
            FarewellDialogueConversations =
                audioBrain?.ConvertToOneShots(FarewellDialogues, speakerName)
                ?? Array.Empty<OneShot>();
        }

        public void OnDestroy() => WelcomeDialogues = cachedWelcomeDialogues;

        private OneShot[] ConvertToOneShots(OneShotDialogue[] dialogues) =>
            audioBrain?.ConvertToOneShots(
                dialogues,
                Shopkeeper != null ? Shopkeeper.DisplayName : "???"
            ) ?? Array.Empty<OneShot>();

        public void NotifyShopVisited()
        {
            brain ??= FindFirstObjectByType<Brain.Brain>();
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

            var ShopUi = TryGetComponent<ShopUi>(out var ui) ? ui : null;
            if (ShopUi == null)
            {
                $"Shop '{name}': No ShopUi component found for dialogue playback.".LogWarning();
            }
            else
            {
                ShopUi.RefreshShopDisplay();
            }

            var welcomeOneShot = GetRandomWelcomeOneShot();
            if (!string.IsNullOrWhiteSpace(welcomeOneShot.Dialogue))
            {
                var player = GetOrCreateOneShotPlayer();
                if (player == null)
                {
                    "Shop: Could not create OneShotPlayer for dialogue playback.".LogWarning();
                }
                player?.PlayOneShot(welcomeOneShot);
            }
            else
            {
                $"Shop '{name}': No welcome dialogue to play".LogInfo();
            }
        }

        public void NotifyShopExited()
        {
            brain ??= FindFirstObjectByType<Brain.Brain>();
            brain?.PublishShopExited(this);

            var farewellOneShot = GetRandomFarewellOneShot();
            var shopUi = TryGetComponent<ShopUi>(out var ui) ? ui : null;

            if (!string.IsNullOrWhiteSpace(farewellOneShot.Dialogue))
            {
                var player = GetOrCreateOneShotPlayer();
                if (player == null)
                {
                    $"Shop '{name}': player is null, hiding shop UI immediately.".LogWarning();
                    shopUi?.ShopUiFade.Hide();
                    return;
                }

                player.PlayOneShot(farewellOneShot);
            }
            else
            {
                $"Shop '{name}': No farewell dialogue to play".LogInfo();
                // No farewell dialogue — SpecificUiHandler.CompleteShopExit will hide the UI.
            }
        }

        public void NotifyShopkeeperBuys(ShopItem[] itemsBought)
        {
            brain ??= FindFirstObjectByType<Brain.Brain>();
            brain?.PublishShopkeeperBuys(this, itemsBought ?? Array.Empty<ShopItem>());

            var buyOneShot = GetRandomBuyOneShot();
            if (!string.IsNullOrWhiteSpace(buyOneShot.Dialogue))
            {
                GetOrCreateOneShotPlayer()?.PlayOneShot(buyOneShot);
            }
            else
            {
                $"Shop '{name}': No buy dialogue to play".LogInfo();
            }
        }

        public void NotifyShopkeeperSells(ShopItem itemSold)
        {
            brain ??= FindFirstObjectByType<Brain.Brain>();
            brain?.PublishShopkeeperSells(this, itemSold);

            var sellOneShot = GetRandomSellOneShot();
            if (!string.IsNullOrWhiteSpace(sellOneShot.Dialogue))
            {
                GetOrCreateOneShotPlayer()?.PlayOneShot(sellOneShot);
            }
            else
            {
                $"Shop '{name}': No sell dialogue to play".LogInfo();
            }
        }

        public OneShot GetRandomWelcomeOneShot() =>
            audioBrain?.GetRandomWelcomeOneShot(WelcomeDialogueConversations) ?? default;

        public OneShot GetRandomSellOneShot() =>
            audioBrain?.GetRandomOneShot(SellDialogueConversations) ?? default;

        public OneShot GetRandomBuyOneShot() =>
            audioBrain?.GetRandomOneShot(BuyDialogueConversations) ?? default;

        public OneShot GetRandomFarewellOneShot() =>
            audioBrain?.GetRandomOneShot(FarewellDialogueConversations) ?? default;

        private OneShotPlayer GetOrCreateOneShotPlayer()
        {
            return brain?.audioBrain?.GetOrCreateOneShotPlayer();
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
                return "";

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
            $"Shop '{name}': Total stock after refresh is {totalStock}.".LogInfo();
            if (totalStock == 0)
            {
                if (SoldOutDialogueText != null)
                {
                    var i = -1;
                    foreach (var dialogue in WelcomeDialogues)
                    {
                        i++;
                        var d = WelcomeDialogues[i];
                        d.Dialogue = SoldOutDialogueText;
                        WelcomeDialogues[i] = d;
                    }
                    WelcomeDialogueConversations = ConvertToOneShots(WelcomeDialogues);
                }
            }
            return "";
        }
    }
}
