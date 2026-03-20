using TMPro;
using Turnroot.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Shop
{
    [RequireComponent(typeof(UiChoice))]
    public class ShopItemUiRefs : MonoBehaviour
    {
        public UiChoice ShopItemChoice => GetComponent<UiChoice>();
        public Image SaleBadge;
        public TextMeshProUGUI PriceText;
        public Color DefaultPriceColor;
        public Color TooExpensivePriceColor;
        public Color OnSalePriceColor;
        public TextMeshProUGUI ItemNameText;
        public TextMeshProUGUI ItemCategoryText;
        public TextMeshProUGUI QuantityText;
        public Image ItemIcon; // don't disable this, set alpha to 0 so it doesn't mess up the hl
        public Image LetterIcon; // don't disable this, set alpha to 0 so it doesn't mess up the hl
    }
}
