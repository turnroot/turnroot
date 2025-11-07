using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Counters/Turns Alive")]
[NodeLabel("Gets the number of turns the unit has survived")]
public class TurnsAlive : SkillNode
{
    [Output]
    FloatValue TurnCount;

    [Output]
    BoolValue FirstTurn;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "TurnCount")
        {
            FloatValue turnCount = new();
            // TODO: Implement runtime retrieval of turns alive count
            // This should track how many turns the unit has been on the battlefield
            return turnCount;
        }
        else if (port.fieldName == "FirstTurn")
        {
            BoolValue firstTurn = new();
            // TODO: Implement runtime check if this is unit's first turn alive
            return firstTurn;
        }
        return null;
    }
}
