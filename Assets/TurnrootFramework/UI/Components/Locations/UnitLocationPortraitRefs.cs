using UnityEngine;
namespace Turnroot.Components.UI
{
    public class UnitLocationPortraitRefs : MonoBehaviour
    {
        public TMPro.TextMeshProUGUI NameText;
        public UnityEngine.UI.Image PortraitImage;

        public void Set(string name, Sprite portrait)
        {
            if (NameText != null)
            {
                NameText.text = name;
            }

            if (PortraitImage != null)
            {
                PortraitImage.sprite = portrait;
            }
        }
    }
}