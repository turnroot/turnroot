using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Shop
{
    public class Shop : MonoBehaviour
    {
        public ShopItem[] ItemsStocked;
        private Dictionary<ShopItem, int> currentStock = new();
        public string ShopDescription;
        public CharacterData Shopkeeper;

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
            foreach (ShopItem item in ItemsStocked)
            {
                var status = item.Refresh(currentDay);
                currentStock[item] = status.AvailableQuantity;
                if (item.RareItem && status.AvailableQuantity > 0)
                {
                    return $"A rare item is in stock at";
                }
            }
            return "";
        }
    }
}
