using TMPro;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.UI.Components
{
    [RequireComponent(typeof(RectTransform))]
    public class SkillListOverlayPrefabReferences : MonoBehaviour
    {
        public TextMeshProUGUI SkillListItemText;
        public Image SkillListItemIcon;
        private Skills.Skill _skill;

        public RectTransform SkillContainerRectTransform => GetComponent<RectTransform>();

        public float CollapsedHeight = 55f;

        public float ExpandedHeight = 160f;

        public bool IsExpanded { get; private set; } = false;

        public GameObject DetailsContainer;

        public TextMeshProUGUI DetailsText;

        public void ToggleDetails()
        {
            IsExpanded = !IsExpanded;
            DetailsContainer?.SetActive(IsExpanded);
            var rt = SkillContainerRectTransform;
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(
                    rt.sizeDelta.x,
                    IsExpanded ? ExpandedHeight : CollapsedHeight
                );
            }
            if (DetailsText != null)
            {
                DetailsText.text =
                    _skill != null ? (_skill.Description ?? string.Empty) : string.Empty;
            }
        }

        public void SetSkill(Skills.Skill skill)
        {
            if (skill == null)
            {
                "Warning: Attempted to set null skill in SkillListOverlayPrefabReferences.".LogWarning();
                return;
            }
            _skill = skill;
            if (SkillListItemText != null)
            {
                SkillListItemText.text = skill.SkillName ?? string.Empty;
            }
            if (SkillListItemIcon != null)
            {
                SkillListItemIcon.sprite = skill.Badge?.RuntimeSprite;
            }
        }
    }
}
