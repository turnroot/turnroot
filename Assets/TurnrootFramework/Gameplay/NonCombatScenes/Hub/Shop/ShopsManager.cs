using System.Collections.Generic;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Shop
{
    public class ShopsManager : MonoBehaviour
    {
        public Shop[] AllShops;

        public string[] RefreshShopsForNewDay(GameDate currentDay)
        {
            var results = new List<string>();
            if (AllShops == null)
                return results.ToArray();
            foreach (var shop in AllShops)
            {
                var result = shop.RefreshShopForNewDay(currentDay);
                if (!string.IsNullOrEmpty(result))
                {
                    var shopName = shop.name;
                    result = $"{result} <b>{shopName}</b>";
                    results.Add(result);
                }
            }
            return results.ToArray();
        }
    }
}
