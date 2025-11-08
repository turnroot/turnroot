using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Enemy/Enemy Class")]
[NodeLabel("Gets the enemy's class type")]
public class EnemyClassNode : SkillNode
{
    [Output]
    public StringValue ClassName;

    [Output]
    public BoolValue IsInfantry;

    [Output]
    public BoolValue IsCavalry;

    [Output]
    public BoolValue IsFlying;

    [Output]
    public BoolValue IsArmored;

    [Output]
    public BoolValue IsDragon;

    [Output]
    public BoolValue IsBeast;

    public override object GetValue(NodePort port)
    {
        var skillGraph = graph as SkillGraph;
        if (skillGraph == null || !Application.isPlaying)
        {
            // Return defaults in editor mode
            return port.fieldName switch
            {
                "ClassName" => new StringValue { value = "Infantry" },
                "IsInfantry" => new BoolValue { value = true },
                "IsCavalry" => new BoolValue { value = false },
                "IsFlying" => new BoolValue { value = false },
                "IsArmored" => new BoolValue { value = false },
                "IsDragon" => new BoolValue { value = false },
                "IsBeast" => new BoolValue { value = false },
                _ => null,
            };
        }

        var context = GetContextFromGraph(skillGraph);
        var enemy = ConditionHelpers.GetCharacterFromContext(
            context,
            ConditionHelpers.CharacterSource.Enemy
        );

        if (enemy == null)
        {
            Debug.LogWarning("EnemyClass: Could not retrieve enemy from context");
            return port.fieldName switch
            {
                "ClassName" => new StringValue { value = "" },
                _ => new BoolValue { value = false },
            };
        }

        // TODO: Implement class type retrieval when character class system is added
        // Future implementation: var classData = enemy.GetClass();
        // Then return classData.ClassName and check classData.ClassType enum
        // Example: return new BoolValue { value = classData.ClassType == ClassType.Infantry };

        return port.fieldName switch
        {
            "ClassName" => new StringValue { value = "" },
            _ => new BoolValue { value = false },
        };
    }
}
