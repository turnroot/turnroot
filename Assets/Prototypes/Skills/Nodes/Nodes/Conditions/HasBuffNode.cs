using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Status/Has Buff")]
[NodeLabel("Checks if a unit has a buff")]
public class HasBuffNode : SkillNode
{
    [Output]
    public BoolValue UnitHasBuff;

    [Output]
    public BoolValue EnemyHasBuff;

    [Output]
    public BoolValue AllyHasBuff;

    [Tooltip("Specific buff type to check (leave empty to check for any buff)")]
    public string buffType = "";

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
            "UnitHasBuff" => ConditionHelpers.GetCharacterFromContext(
                context,
                ConditionHelpers.CharacterSource.Unit
            ),
            "EnemyHasBuff" => ConditionHelpers.GetCharacterFromContext(
                context,
                ConditionHelpers.CharacterSource.Enemy
            ),
            "AllyHasBuff" => ConditionHelpers.GetCharacterFromContext(
                context,
                ConditionHelpers.CharacterSource.Ally
            ),
            _ => null,
        };

        if (character == null)
        {
            return new BoolValue { value = false };
        }

        // TODO: Implement actual buff check when buff/debuff system is added
        // Check context.CustomData or character.ActiveBuffs for buff presence
        // If buffType is specified, check for that specific buff, otherwise check for any buff
        // Future implementation:
        // if (string.IsNullOrEmpty(buffType))
        //     return new BoolValue { value = character.HasAnyBuff() };
        // else
        //     return new BoolValue { value = character.HasBuff(buffType) };
        return new BoolValue { value = false };
    }
}
