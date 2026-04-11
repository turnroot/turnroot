using System;
using Turnroot.Gameplay.Objects;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Shop
{
    [Serializable]
    /// <summary>
    /// Represents an item that can be bought/sold in a shop, along with all relevant information about its availability, restocking, and sales. Use <code>ShopItem.Refresh()</code> as the main way to update the item's status each day, which will handle restocking and sales logic together.
    /// </summary>
    public struct ShopItem
    {
        [HideInInspector]
        public ShopItemUiRefs UiRefs;
        public ObjectItem Item;
        public Status CurrentStatus;
        public int MaxQuantity;
        public bool RestockAtIntervals;
        public int RestockIntervalDays;
        private GameDate lastRestockDate;
        public int RestockQuantityPerDay;
        public bool CanGoOnSale;
        public int SalePrice;
        public GameDate[] SpecificSaleDays;

        [Range(0f, 1f)]
        public float SaleChanceOnRandomDays;

        public bool RareItem;
        public float ChanceToAppearInShop;

        public void IsOnSale(GameDate currentDay)
        {
            if (SpecificSaleDays == null || SpecificSaleDays.Length == 0 || !CanGoOnSale)
            {
                CurrentStatus.IsOnSale = false;
                return;
            }

            foreach (GameDate saleDay in SpecificSaleDays)
            {
                if (
                    saleDay == currentDay
                    || (
                        CanGoOnSale
                        && SaleChanceOnRandomDays > 0f
                        && HubDayRandom.Value < SaleChanceOnRandomDays
                    )
                )
                {
                    CurrentStatus.IsOnSale = true;
                    return;
                }
            }
            CurrentStatus.IsOnSale = false;
        }

        /// <summary>
        /// Restocks the item if it's a rare item that can randomly appear, or if it's an item that restocks at intervals and the appropriate amount of time has passed since the last restock.
        /// </summary>
        /// <param name="currentDay"></param>
        /// <returns>
        /// The current available quantity
        /// </returns>
        public int RestockIfNeeded(GameDate currentDay)
        {
            if (RareItem)
            {
                if (HubDayRandom.Value < ChanceToAppearInShop)
                {
                    CurrentStatus.AvailableQuantity = Math.Min(
                        CurrentStatus.AvailableQuantity + 1,
                        MaxQuantity
                    );
                    return CurrentStatus.AvailableQuantity;
                }
            }

            if (CurrentStatus.AvailableQuantity >= MaxQuantity)
            {
                return CurrentStatus.AvailableQuantity;
            }
            if (!RestockAtIntervals)
            {
                return CurrentStatus.AvailableQuantity;
            }
            else if (ToDayApprox(currentDay) >= ToDayApprox(lastRestockDate) + RestockIntervalDays)
            {
                CurrentStatus.AvailableQuantity = Math.Min(
                    CurrentStatus.AvailableQuantity + RestockQuantityPerDay,
                    MaxQuantity
                );
                lastRestockDate = currentDay;
            }
            return CurrentStatus.AvailableQuantity;
        }

        public void Initialize(GameDate currentDay) => lastRestockDate = currentDay;

        private static int ToDayApprox(GameDate d) => d.year * 365 + d.month * 30 + d.day;

        public struct Status
        {
            public int AvailableQuantity;
            public bool IsOnSale;
        }

        public Status Refresh(GameDate currentDay)
        {
            IsOnSale(currentDay);
            RestockIfNeeded(currentDay);
            return CurrentStatus;
        }
    }
}
