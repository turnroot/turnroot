using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.UI.Components
{
    public class PassiveSkillOverlay : MonoBehaviour
    {
        public GameObject PassiveSkillEntryPrefab;
        public GameObject PassiveSkillListContainer;
        private RectTransform ContainerRect;

        public float CollapsedPosY = 472f;
        public float ExpandedPosY = 456f;

        public VerticalLayoutGroup ItemsLayoutGroup;

        public float CollapsedSpacing = -20f;
        public float ExpandedSpacing = -60f;

        private void Awake()
        {
            if (PassiveSkillListContainer != null)
            {
                ContainerRect = PassiveSkillListContainer.GetComponent<RectTransform>();
            }
            else
            {
                "PassiveSkillListContainer reference is not set in PassiveSkillOverlay.".LogError();
            }
        }

        private SkillListOverlayPrefabReferences[] PassiveSkillEntries;

        public void ClearSkills()
        {
            foreach (Transform child in PassiveSkillListContainer.transform)
            {
                Destroy(child.gameObject);
            }
        }

        public void AddSkill(Skills.Skill skill)
        {
            var entryObj = Instantiate(
                PassiveSkillEntryPrefab,
                PassiveSkillListContainer.transform
            );
            if (entryObj.TryGetComponent<SkillListOverlayPrefabReferences>(out var entryRefs))
            {
                entryRefs.SetSkill(skill);
            }
        }

        public void ToggleDetails(bool showDetails = false)
        {
            if (ContainerRect != null)
            {
                ContainerRect.anchoredPosition = new Vector2(
                    ContainerRect.anchoredPosition.x,
                    showDetails ? ExpandedPosY : CollapsedPosY
                );
            }

            if (ItemsLayoutGroup != null)
            {
                ItemsLayoutGroup.spacing = showDetails ? ExpandedSpacing : CollapsedSpacing;
            }

            foreach (Transform child in PassiveSkillListContainer.transform)
            {
                if (child.TryGetComponent<SkillListOverlayPrefabReferences>(out var entryRefs))
                {
                    entryRefs.ToggleDetails();
                }
            }
        }
    }
}
