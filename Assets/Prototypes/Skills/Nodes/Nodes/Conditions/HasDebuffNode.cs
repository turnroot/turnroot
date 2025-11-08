using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Status/Has Debuff")]
[NodeLabel("Checks if a unit has a debuff")]
public class HasDebuffNode : SkillNode
{
    [Output]
    public BoolValue UnitHasDebuff;

    [Output]
    public BoolValue EnemyHasDebuff;

    [Output]
    public BoolValue AllyHasDebuff;

    [Tooltip("Specific debuff type to check (leave empty to check for any debuff)")]
    public string debuffType = "";

    public override object GetValue(NodePort port)
    {
        var skillGraph = graph as SkillGraph;
        if (skillGraph == null || !Application.isPlaying)
        {
            // Return false in editor mode
            return new BoolValue { value = false };
        }

        // Get context
        var context = GetContextFromGraph(skillGraph);
        if (context == null)
        {
            return new BoolValue { value = false };
        }

        // Determine which character to check based on port
        var character = port.fieldName switch
        {
            "UnitHasDebuff" => ConditionHelpers.GetCharacterFromContext(
                context,
                ConditionHelpers.CharacterSource.Unit
            ),
            "EnemyHasDebuff" => ConditionHelpers.GetCharacterFromContext(
                context,
                ConditionHelpers.CharacterSource.Enemy
            ),
            "AllyHasDebuff" => ConditionHelpers.GetCharacterFromContext(
                context,
                ConditionHelpers.CharacterSource.Ally
            ),
            _ => null,
        };

        if (character == null)
        {
            return new BoolValue { value = false };
        }

        // TODO: Implement actual debuff check when buff/debuff system is added
        // Check context.CustomData or character.ActiveDebuffs for debuff presence
        // If debuffType is specified, check for that specific debuff, otherwise check for any debuff
        // Future implementation:
        // if (string.IsNullOrEmpty(debuffType))
        //     return new BoolValue { value = character.HasAnyDebuff() };
        // else
        //     return new BoolValue { value = character.HasDebuff(debuffType) };
        return new BoolValue { value = false };
    }
}
