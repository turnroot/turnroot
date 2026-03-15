using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Shop
{
    public class ShopsManager : MonoBehaviour
    {
        public Shop[] AllShops;

        public void RefreshShopsForNewDay(GameDate currentDay)
        {
            foreach (var shop in AllShops)
            {
                shop.RefreshShopForNewDay(currentDay);
            }
        }
    }
}
