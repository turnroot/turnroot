using TMPro;
using Turnroot.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Blacksmith
{
    [RequireComponent(typeof(UiChoice))]
    public class BlacksmithItemRefs : MonoBehaviour
    {
        public UiChoice BlacksmithItemChoice => GetComponent<UiChoice>();

        public Image OwnerPortrait;
        public GameObject OwnerPortraitParent;
        public TextMeshProUGUI ItemNameText;
        public TextMeshProUGUI UsesText;
        public TextMeshProUGUI RepairsText;
        public TextMeshProUGUI GoldCostText;
        public TextMeshProUGUI RepairItemNameText;
        public TextMeshProUGUI RepairItemCostText;
    }
}
