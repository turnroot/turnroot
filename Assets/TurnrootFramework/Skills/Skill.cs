using System.Linq;
using NaughtyAttributes;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Skills.Components.Badges;
using Turnroot.Skills.Nodes;
using Turnroot.Skills.Nodes.Flow;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills
{
    /// <summary>
    /// Defines a skill template with appearance, behavior graph, and execution logic.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkill", menuName = "Turnroot/Skills/Skill")]
    public class Skill : ScriptableObject
    {
        [BoxGroup("Appearance"), HorizontalLine(color: EColor.Violet)]
        public Color AccentColor1;

        [BoxGroup("Appearance")]
        public Color AccentColor2;

        [BoxGroup("Appearance")]
        public Color AccentColor3;

        /// <summary>
        /// Returns true when the behavior graph contains a
        /// <see cref="BattleStartsNode"/>, which is used for triggering skills at
        /// the beginning of a battle.
        /// </summary>
        public bool HasBattleStartNode() =>
            BehaviorGraph != null && BehaviorGraph.nodes.OfType<BattleStartsNode>().Any();

        [BoxGroup("Appearance"), HideInInspector]
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

        [BoxGroup("Info"), HorizontalLine(color: EColor.Indigo)]
        public string SkillName;

        [TextArea, BoxGroup("Info")]
        public string Description;

        [BoxGroup("Behavior"), HorizontalLine(color: EColor.Blue)]
        public SkillGraph BehaviorGraph;

        /// <summary>
        /// Execute this skill's behavior graph with the given context.
        /// This is a template method - use SkillInstance.ExecuteSkill for runtime execution.
        /// </summary>
        public void ExecuteSkill(BattleContext context)
        {
            $"Executing skill {SkillName}".LogInfo();
            if (BehaviorGraph == null)
            {
                $"Skill {SkillName} has no BehaviorGraph assigned.".LogWarning();
                return;
            }

            context.Skill.CurrentSkill = this;
            context.Skill.CurrentSkillGraph = BehaviorGraph;
            BehaviorGraph.Execute(context);
        }
    }
}
