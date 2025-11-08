using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Counters/Turns Alive")]
[NodeLabel("Gets the number of turns the unit has survived")]
public class TurnsAliveNode : SkillNode
{
    [Output]
    public FloatValue TurnCount;

    [Output]
    public BoolValue FirstTurn;

    public override object GetValue(NodePort port)
    {
        var skillGraph = graph as SkillGraph;
        if (skillGraph == null || !Application.isPlaying)
        {
            // Return defaults in editor mode
            return port.fieldName switch
            {
                "TurnCount" => new FloatValue { value = 1f },
                "FirstTurn" => new BoolValue { value = true },
                _ => null,
            };
        }

        var context = GetContextFromGraph(skillGraph);
        var character = ConditionHelpers.GetCharacterFromContext(
            context,
            ConditionHelpers.CharacterSource.Unit
        );

        if (character == null)
        {
            Debug.LogWarning("TurnsAlive: Could not retrieve unit from context");
            return port.fieldName switch
            {
                "TurnCount" => new FloatValue { value = 1f },
                "FirstTurn" => new BoolValue { value = true },
                _ => null,
            };
        }

        // TODO: Implement actual turns alive tracking when battle system is added
        // Future implementation:
        // int turnsAlive = character.TurnsAliveCount;
        // return port.fieldName switch
        // {
        //     "TurnCount" => new FloatValue { value = turnsAlive },
        //     "FirstTurn" => new BoolValue { value = turnsAlive == 1 },
        //     _ => null,
        // };

        return port.fieldName switch
        {
            "TurnCount" => new FloatValue { value = 1f },
            "FirstTurn" => new BoolValue { value = true },
            _ => null,
        };
    }
}
