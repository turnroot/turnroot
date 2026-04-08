using TMPro;
using Turnroot.Gameplay.Objects;
using Turnroot.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [RequireComponent(typeof(UiChoice))]
    public class GiftItemRowUiRefs : MonoBehaviour
    {
        public UiChoice GiftItemChoice => GetComponent<UiChoice>();
        public TextMeshProUGUI ItemNameText;
        public TextMeshProUGUI ItemDescriptionText;
        public TextMeshProUGUI QuantityText;

        public Image Rank1;
        public Image Rank2;
        public Image Rank3;

        public void Initialize(ObjectItem item, int quantity)
        {
            ItemNameText.text = item.Name;
            ItemDescriptionText.text = item.FlavorText;
            QuantityText.text = $"x{quantity}";
            SetRank(item.GiftRank);
        }

        public void SetRank(int rank)
        {
            if (rank == 1)
            {
                Rank1.color = Color.white;
                Rank2.color = new Color(0f, 0f, 0f, 0f);
                Rank3.color = new Color(0f, 0f, 0f, 0f);
            }
            else if (rank == 2)
            {
                Rank1.color = Color.white;
                Rank2.color = Color.white;
                Rank3.color = new Color(0f, 0f, 0f, 0f);
            }
            else if (rank >= 3)
            {
                Rank1.color = Color.white;
                Rank2.color = Color.white;
                Rank3.color = Color.white;
            }
        }
    }
}
