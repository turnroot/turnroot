using NaughtyAttributes;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Skills.Components.Badges;
using Turnroot.Skills.Nodes;
using UnityEngine;

namespace Turnroot.Skills
{
    [CreateAssetMenu(fileName = "NewSkill", menuName = "Turnroot/Skills/Skill")]
    public class Skill : ScriptableObject
    {
        [Foldout("Appearance"), HorizontalLine(color: EColor.Violet)]
        public Color AccentColor1;

        [Foldout("Appearance")]
        public Color AccentColor2;

        [Foldout("Appearance")]
        public Color AccentColor3;

        [Foldout("Appearance"), HideInInspector]
        public SkillBadge Badge;

        [Button("Create Badge")]
        public void CreateNewBadge()
        {
            SkillBadge newBadge = new();
            newBadge.SetOwner(this);
            newBadge.UpdateTintColorsFromOwner();
            Badge = newBadge;
#if UNITY_EDITOR
            // Open the badge editor window using reflection to avoid Editor assembly dependency
            var editorWindowType = System.Type.GetType(
                "Turnroot.Skills.Components.Badges.Editor.SkillBadgeEditorWindow, Assembly-CSharp-Editor"
            );
            if (editorWindowType != null)
            {
                var method = editorWindowType.GetMethod(
                    "OpenSkillBadge",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
                );
                method?.Invoke(null, new object[] { this, 0 });
            }
#endif
        }

        [Foldout("Info"), HorizontalLine(color: EColor.Indigo)]
        public string SkillName;

        [TextArea, Foldout("Info")]
        public string Description;

        [Foldout("Behavior"), HorizontalLine(color: EColor.Blue)]
        public SkillGraph BehaviorGraph;

        /// <summary>
        /// Execute this skill's behavior graph with the given context.
        /// This is a template method - use SkillInstance.ExecuteSkill for runtime execution.
        /// </summary>
        public void ExecuteSkill(BattleContext context)
        {
            if (BehaviorGraph == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Skill {SkillName} has no BehaviorGraph assigned.");
#endif
                return;
            }

            context.Skill.CurrentSkill = this;
            context.Skill.CurrentSkillGraph = BehaviorGraph;
            BehaviorGraph.Execute(context);
        }
    }
}
