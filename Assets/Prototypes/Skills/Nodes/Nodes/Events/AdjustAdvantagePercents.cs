using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Events/Adjust Advantage Percents")]
[NodeLabel("Adjust Advantage Percents")]
public class AdjustAdvantagePercents : SkillNode
{
    [Input]
    public ExecutionFlow In;

    [Output]
    public ExecutionFlow Out;

    [Tooltip("The percent to increase advantage by")]
    [Range(0, 100)]
    public float AddAdvantagePercent;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "Out")
        {
            ExecutionFlow outFlow = new();
            // TODO: Implement runtime adjustment of advantage percents
            return outFlow;
        }
        return null;
    }
}
