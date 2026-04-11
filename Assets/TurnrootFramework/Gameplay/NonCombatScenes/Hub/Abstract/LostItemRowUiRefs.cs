using TMPro;
using Turnroot.Gameplay.Objects;
using Turnroot.UI;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [RequireComponent(typeof(UiChoice))]
    public class LostItemUiRowRefs : MonoBehaviour
    {
        public UiChoice LostItemChoice => GetComponent<UiChoice>();
        public TextMeshProUGUI ItemNameText;
        public TextMeshProUGUI ItemDescriptionText;
        public TextMeshProUGUI QuantityText;

        public void Initialize(ObjectItem item, int quantity)
        {
            ItemNameText.text = item.Name;
            ItemDescriptionText.text = item.FlavorText;
            QuantityText.text = $"x{quantity}";
        }
    }
}
