using Turnroot.Characters;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Shop
{
    public class Shop : MonoBehaviour
    {
        public ShopItem[] ItemsStocked;
        public string ShopDescription;
        public CharacterData Shopkeeper;
        public bool ShopkeeperWillGamble = false;
        public bool ShopOpen = true;
        public bool WillBuy = true;
    }
}
