using System;
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Conversations;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Shop
{
    [System.Serializable]
    public struct ShopDialogue
    {
        public string Dialogue;
        public Sprite Portrait;
        public AudioClip Audio;
    }

    public class Shop : MonoBehaviour
    {
        public ShopItem[] ItemsStocked;
        private Dictionary<ShopItem, int> currentStock = new();

        public string ShopName;
        public string ShopDescription;
        public CharacterData Shopkeeper;

        public ShopDialogue[] WelcomeDialogues;
        public ShopDialogue[] ShopKeeperSellsDialogues;
        public ShopDialogue[] ShopKeeperBuysDialogues;
        public ShopDialogue[] FarewellDialogues;

        private OneShot[] WelcomeDialogueConversations;
        private OneShot[] SellDialogueConversations;
        private OneShot[] BuyDialogueConversations;
        private OneShot[] FarewellDialogueConversations;

        public Brain.Brain brain;

        private Brain.Brain GetBrain()
        {
            if (brain == null)
            {
                brain = FindFirstObjectByType<Brain.Brain>();
            }
            return brain;
        }

        public void Awake()
        {
            WelcomeDialogueConversations = ConvertToOneShots(WelcomeDialogues);
            SellDialogueConversations = ConvertToOneShots(ShopKeeperSellsDialogues);
            BuyDialogueConversations = ConvertToOneShots(ShopKeeperBuysDialogues);
            FarewellDialogueConversations = ConvertToOneShots(FarewellDialogues);
        }

        private OneShot[] ConvertToOneShots(ShopDialogue[] dialogues)
        {
            if (dialogues == null)
            {
                return Array.Empty<OneShot>();
            }

            OneShot[] conversations = new OneShot[dialogues.Length];
            for (int i = 0; i < dialogues.Length; i++)
            {
                conversations[i] = new OneShot
                {
                    Dialogue = dialogues[i].Dialogue,
                    Portrait = dialogues[i].Portrait,
                    Audio = dialogues[i].Audio,
                    SpeakerName = Shopkeeper != null ? Shopkeeper.DisplayName : "???",
                };
            }
            return conversations;
        }

        private void EnsureOneShotConversationsInitialized()
        {
            WelcomeDialogueConversations ??= ConvertToOneShots(WelcomeDialogues);
            SellDialogueConversations ??= ConvertToOneShots(ShopKeeperSellsDialogues);
            BuyDialogueConversations ??= ConvertToOneShots(ShopKeeperBuysDialogues);
            FarewellDialogueConversations ??= ConvertToOneShots(FarewellDialogues);
        }

        public void NotifyShopVisited()
        {
            Debug.Log($"Shop '{name}': NotifyShopVisited called.");
            GetBrain()?.PublishShopVisited(this);
            // Ensure shop stock is refreshed from LongTermMemory on every visit,
            // even on the same calendar day.
            var brain = GetBrain();
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
            Debug.Log($"Shop '{name}': NotifyShopExited called.");
            GetBrain()?.PublishShopExited(this);

            var farewellOneShot = GetRandomFarewellOneShot();
            var shopUi = TryGetComponent<ShopUi>(out var ui) ? ui : null;
            var conversationController =
                ConversationController.Instance ?? FindFirstObjectByType<ConversationController>();

            if (!string.IsNullOrWhiteSpace(farewellOneShot.Dialogue))
            {
                Debug.Log($"Shop '{name}': farewell dialogue exists, shopUi={(shopUi != null)}");
                // Let SpecificUiHandler handle OnAnyConversationFinished for shop exit cleanup.

                var player = GetOrCreateOneShotPlayer();
                if (player == null)
                {
                    "Shop: Could not create OneShotPlayer for dialogue playback.".LogWarning();
                    Debug.LogWarning($"Shop '{name}': player is null, hiding shop UI immediately.");
                    shopUi?.ShopUiFade.Hide();
                    return;
                }

                Debug.Log($"Shop '{name}': playing farewell one-shot dialogue.");
                player.PlayOneShot(farewellOneShot);
            }
            else
            {
                $"Shop '{name}': No farewell dialogue to play".LogInfo();
                Debug.Log($"Shop '{name}': no farewell dialogue, hiding shop UI now.");
                shopUi?.ShopUiFade.Hide();
            }
        }

        public void NotifyShopkeeperBuys(ShopItem[] itemsBought)
        {
            GetBrain()?.PublishShopkeeperBuys(this, itemsBought ?? Array.Empty<ShopItem>());

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

        public void NotifyShopkeeperSells(ShopItem[] itemsSold)
        {
            GetBrain()?.PublishShopkeeperSells(this, itemsSold ?? Array.Empty<ShopItem>());

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

        public OneShot GetRandomWelcomeOneShot() => GetRandomOneShot(WelcomeDialogueConversations);

        public OneShot GetRandomSellOneShot() => GetRandomOneShot(SellDialogueConversations);

        public OneShot GetRandomBuyOneShot() => GetRandomOneShot(BuyDialogueConversations);

        public OneShot GetRandomFarewellOneShot() =>
            GetRandomOneShot(FarewellDialogueConversations);

        private OneShot GetRandomOneShot(OneShot[] candidates)
        {
            EnsureOneShotConversationsInitialized();
            if (candidates == null || candidates.Length == 0)
            {
                return default;
            }
            return candidates[UnityEngine.Random.Range(0, candidates.Length)];
        }

        private OneShotPlayer GetOrCreateOneShotPlayer()
        {
            if (!TryGetComponent<OneShotPlayer>(out var player))
            {
                player = gameObject.AddComponent<OneShotPlayer>();
            }

            if (TryGetComponent<AudioSource>(out var audioSource))
            {
                player.SetAudioSource(audioSource);
            }

            return player;
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
            var brain = GetBrain();
            bool hasSavedShopStock = HubDayStateStore.HasShopStock(name);
            bool isDailyUpdateAlreadyProcessed = HubDayStateStore.HasProcessedDailyUpdates;

            // If this shop has no existing persistent stock data, initialize normal items to max
            if (!hasSavedShopStock && brain != null)
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
                    HubDayStateStore.SetShopItemQuantity(brain, name, itemName, item.MaxQuantity);
                }
            }
            else if (brain != null)
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

                // Persist the updated quantity into HubDayStateStore for this day.
                if (brain != null)
                {
                    string itemName = item.Item != null ? item.Item.name : string.Empty;
                    HubDayStateStore.SetShopItemQuantity(
                        brain,
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
            return "";
        }
    }
}
