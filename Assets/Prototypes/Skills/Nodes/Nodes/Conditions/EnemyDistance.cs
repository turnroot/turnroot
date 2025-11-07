using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Position/Enemy Distance")]
[NodeLabel("Gets the distance to the target enemy")]
public class EnemyDistance : SkillNode
{
    [Output]
    public FloatValue value;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "value")
        {
            FloatValue distanceValue = new() { value = GetDistanceToTargetEnemy() };
            return distanceValue;
        }

        return null;
    }

    private float GetDistanceToTargetEnemy()
    {
        // TODO: Implement runtime retrieval of distance to target enemy
        return 0f;
    }
}
