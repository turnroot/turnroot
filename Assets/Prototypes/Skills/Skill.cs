using Assets.Prototypes.Skills.Components.Badges;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSkill", menuName = "Turnroot/Skills/New Skill")]
public class Skill : ScriptableObject
{
    public Color AccentColor1;
    public Color AccentColor2;
    public Color AccentColor3;

    public SkillBadge Badge;
}
