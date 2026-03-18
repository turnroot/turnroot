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

        private Brain.Brain brain;

        private Brain.Brain GetBrain()
        {
            if (brain == null)
            {
                brain = UnityEngine.Object.FindFirstObjectByType<Brain.Brain>();
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
            GetBrain()?.PublishShopVisited(this);

            var welcomeOneShot = GetRandomWelcomeOneShot();
            if (!string.IsNullOrWhiteSpace(welcomeOneShot.Dialogue))
            {
                $"Shop '{name}': Playing welcome dialogue".LogInfo();
                var player = GetOrCreateOneShotPlayer();
                if (player == null)
                {
                    UnityEngine.Debug.LogWarning(
                        "Shop: Could not create OneShotPlayer for dialogue playback."
                    );
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
            GetBrain()?.PublishShopExited(this);

            var farewellOneShot = GetRandomFarewellOneShot();
            if (!string.IsNullOrWhiteSpace(farewellOneShot.Dialogue))
            {
                $"Shop '{name}': Playing farewell dialogue".LogInfo();
                var player = GetOrCreateOneShotPlayer();
                if (player == null)
                {
                    UnityEngine.Debug.LogWarning(
                        "Shop: Could not create OneShotPlayer for dialogue playback."
                    );
                }
                player?.PlayOneShot(farewellOneShot);
            }
            else
            {
                $"Shop '{name}': No farewell dialogue to play".LogInfo();
            }
        }

        public void NotifyShopkeeperBuys(ShopItem[] itemsBought)
        {
            GetBrain()?.PublishShopkeeperBuys(this, itemsBought ?? Array.Empty<ShopItem>());

            var buyOneShot = GetRandomBuyOneShot();
            if (!string.IsNullOrWhiteSpace(buyOneShot.Dialogue))
            {
                $"Shop '{name}': Playing buy dialogue".LogInfo();
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
                $"Shop '{name}': Playing sell dialogue".LogInfo();
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
            // Attempt to load persisted quantities for this hub day before restocking logic
            var brain = GetBrain();
            if (brain != null)
            {
                foreach (ShopItem item in ItemsStocked)
                {
                    string itemName = item.Item != null ? item.Item.name : string.Empty;
                    int persistedQuantity = HubDayStateStore.GetShopItemQuantity(
                        name,
                        itemName,
                        -1
                    );

                    if (persistedQuantity >= 0)
                    {
                        currentStock[item] = persistedQuantity;
                    }
                }
            }

            foreach (ShopItem item in ItemsStocked)
            {
                var status = item.Refresh(currentDay);
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

                if (item.RareItem && status.AvailableQuantity > 0)
                {
                    return $"A rare item is in stock at";
                }
            }
            return "";
        }
    }
}
