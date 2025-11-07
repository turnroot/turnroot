using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Events/Adjust Advantage Percents")]
[NodeLabel("Adjust Advantage Percents")]
public class AdjustAdvantagePercents : SkillNode
{
    [Input]
    public ExecutionFlow In;



    [Tooltip("The percent to increase advantage by")]
    [Range(0, 100)]
    public float AddAdvantagePercent;
}
