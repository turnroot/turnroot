using Assets.Prototypes.Characters.Subclasses;
using Assets.Prototypes.Graphics2D.Editor;
using UnityEditor;

namespace Assets.Prototypes.Skills.Components.Badges.Editor
{
    public class SkillBadgeEditorWindow : StackedImageEditorWindow<Skill, SkillBadge>
    {
        protected override string WindowTitle => "Skill Badge Editor";
        protected override string OwnerFieldLabel => "Skill";

        [MenuItem("Window/Turnroot/Skill Badge Editor")]
        public static void ShowWindow()
        {
            GetWindow<SkillBadgeEditorWindow>("Skill Badge Editor");
        }

        public static void OpenSkillBadge(Skill skill, int badgeIndex = 0)
        {
            var window = GetWindow<SkillBadgeEditorWindow>("Skill Badge Editor");
            window._currentOwner = skill;
            window._selectedImageIndex = badgeIndex;
            if (skill != null && skill.Badge != null)
            {
                window._currentImage = skill.Badge;
                window.RefreshPreview();
            }
        }

        protected override SkillBadge[] GetImagesFromOwner(Skill owner)
        {
            return owner != null ? new[] { owner.Badge } : null;
        }
    }
}
