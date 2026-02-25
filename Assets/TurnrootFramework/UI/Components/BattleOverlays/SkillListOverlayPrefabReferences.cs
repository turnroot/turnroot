using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.UI.Components
{
    public class SkillListOverlayPrefabReferences : MonoBehaviour
    {
        public TextMeshProUGUI SkillListItemText;
        public Image SkillListItemIcon;

        public void SetText(string text)
        {
            if (SkillListItemText != null)
            {
                SkillListItemText.text = text;
            }
        }

        public void SetIcon(Sprite icon)
        {
            if (SkillListItemIcon != null)
            {
                SkillListItemIcon.sprite = icon;
            }
        }
    }
}
