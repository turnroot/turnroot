using Assets.Prototypes.Gameplay.Combat.FundamentalComponents.Battles;
using Assets.Prototypes.Skills.Components.Badges;
using Assets.Prototypes.Skills.Nodes;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

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
            "Assets.Prototypes.Skills.Components.Badges.Editor.SkillBadgeEditorWindow, Assembly-CSharp-Editor"
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

    [Foldout("Behavior")]
    public UnityEvent ReadyToFire;

    [Foldout("Behavior")]
    public UnityEvent SkillTriggered;

    [Foldout("Behavior")]
    public UnityEvent ActionCompleted;

    [Foldout("Behavior")]
    public UnityEvent SkillEquipped;

    [Foldout("Behavior")]
    public UnityEvent SkillUnequipped;

    /// <summary>
    /// Execute this skill's behavior graph with the given context.
    /// </summary>
    public void ExecuteSkill(BattleContext context)
    {
        if (BehaviorGraph == null)
        {
            UnityEngine.Debug.LogWarning($"Skill {SkillName} has no BehaviorGraph assigned.");
            return;
        }

        context.CurrentSkill = this;
        context.CurrentSkillGraph = BehaviorGraph;
        SkillTriggered?.Invoke();
        BehaviorGraph.Execute(context);
    }
}
