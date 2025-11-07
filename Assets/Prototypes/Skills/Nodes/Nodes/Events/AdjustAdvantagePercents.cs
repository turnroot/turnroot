using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Events/Neutral/Adjust Advantage Percents")]
[NodeLabel("Adjust Advantage Percents")]
public class AdjustAdvantagePercents : SkillNode
{
    [Input]
    public ExecutionFlow In;

    [Tooltip("The percent to increase advantage by")]
    [Range(0, 100)]
    public float AddAdvantagePercent;

    public override void Execute(SkillExecutionContext context)
    {
        if (context == null)
        {
            Debug.LogWarning("AdjustAdvantagePercents: No context provided");
            return;
        }

        // TODO: Implement logic to adjust advantage percents based on context
        Debug.Log(
            $"AdjustAdvantagePercents: Would adjust advantage percents by {AddAdvantagePercent}% based on context"
        );
    }
}
