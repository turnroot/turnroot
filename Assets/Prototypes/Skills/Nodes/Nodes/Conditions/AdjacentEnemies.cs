using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Adjacent Enemies")]
[NodeLabel("Gets the current adjacent enemies count")]
public class AdjacentEnemies : SkillNode
{
    [Output]
    public FloatValue value;

    [Output]
    BoolValue adjacentEnemy;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "value")
        {
            FloatValue adjacentEnemiesCount = new();
            // TODO: Implement runtime retrieval of adjacent enemies count
            return adjacentEnemiesCount;
        }
        else if (port.fieldName == "adjacentEnemy")
        {
            BoolValue hasAdjacentEnemy = new();
            // TODO: Implement runtime retrieval of adjacent enemies presence
            return hasAdjacentEnemy;
        }
        return null;
    }
}
