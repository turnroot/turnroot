using System;
using NaughtyAttributes;
using Turnroot.Gameplay.Objects;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Shop
{
    [Serializable]
    public struct ShopItem
    {
        public ObjectItem Item;
        public int AvailableQuantity;
        public int MaxQuantity;
        public bool RestockAtIntervals;
        public int RestockIntervalDays;
        private GameDate lastRestockDate;
        public int RestockQuantityPerDay;
        public int SalePrice;
        public GameDate[] SpecificSaleDays;

        [Range(0f, 1f)]
        public float SaleChanceOnRandomDays;

        public readonly bool IsOnSale(GameDate currentDay)
        {
            if (SpecificSaleDays == null || SpecificSaleDays.Length == 0)
            {
                return false;
            }

            foreach (GameDate saleDay in SpecificSaleDays)
            {
                if (
                    saleDay == currentDay
                    || (
                        SaleChanceOnRandomDays > 0f
                        && UnityEngine.Random.value < SaleChanceOnRandomDays
                    )
                )
                {
                    return true;
                }
            }
            return false;
        }

        public void RestockIfNeeded(GameDate currentDay)
        {
            if (!RestockAtIntervals)
            {
                return;
            }
            else if (
                currentDay.year > lastRestockDate.year
                || currentDay.month > lastRestockDate.month
                || currentDay.day >= lastRestockDate.day + RestockIntervalDays
            )
            {
                AvailableQuantity = Math.Min(
                    AvailableQuantity + RestockQuantityPerDay,
                    MaxQuantity
                );
                lastRestockDate = currentDay;
            }
        }

        public void Initialize(GameDate currentDay) => lastRestockDate = currentDay;

        public bool RefreshAndReturnSaleStatus(GameDate currentDay)
        {
            RestockIfNeeded(currentDay);
            return IsOnSale(currentDay);
        }
    }
}
