using TMPro;
using Turnroot.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Blacksmith
{
    [RequireComponent(typeof(UiChoice))]
    public class BlacksmithForgeOptionRefs : MonoBehaviour
    {
        public UiChoice ForgeOptionChoice => GetComponent<UiChoice>();

        public Image ForgeIntoIcon;
        public TextMeshProUGUI ForgeIntoNameText;
        public TextMeshProUGUI DescriptionText;
        public TextMeshProUGUI UsesText;
        public TextMeshProUGUI GoldCostText;
        public Image MaterialIcon;
        public TextMeshProUGUI MaterialNameText;
        public TextMeshProUGUI MaterialAmountText;
    }
}
