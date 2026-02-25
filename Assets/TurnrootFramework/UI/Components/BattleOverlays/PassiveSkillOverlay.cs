using UnityEngine;

namespace Turnroot.UI.Components
{
    public class PassiveSkillOverlay : MonoBehaviour
    {
        public GameObject PassiveSkillEntryPrefab;
        public GameObject PassiveSkillListContainer;
        private SkillListOverlayPrefabReferences[] PassiveSkillEntries;

        public void ClearSkills()
        {
            foreach (Transform child in PassiveSkillListContainer.transform)
            {
                Destroy(child.gameObject);
            }
        }

        public void AddSkill(string skillName, Sprite skillIcon)
        {
            var entryObj = Instantiate(
                PassiveSkillEntryPrefab,
                PassiveSkillListContainer.transform
            );
            if (entryObj.TryGetComponent<SkillListOverlayPrefabReferences>(out var entryRefs))
            {
                entryRefs.SetText(skillName);
                entryRefs.SetIcon(skillIcon);
            }
        }
    }
}
