using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

public enum EnvironmentalCondition
{
    IsNight,

    IsRaining,
    IsFoggy,
    IsDesert,
    IsSnowing,
    IsIndoors,
}

[CreateNodeMenu("Conditions/Environmental Conditions")]
[NodeLabel("Checks environmental conditions")]
public class EnvironmentalConditionsNode : SkillNode
{
    [Output]
    public BoolValue Condition;
    public EnvironmentalCondition conditionToCheck;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "Condition")
        {
            if (graph is SkillGraph skillGraph)
            {
                BoolValue result = new BoolValue();
                var contextFromGraph = GetContextFromGraph(skillGraph);
                var envConditions = contextFromGraph?.EnvironmentalConditions;

                if (envConditions != null)
                {
                    switch (conditionToCheck)
                    {
                        case EnvironmentalCondition.IsNight:
                            result.value = envConditions.IsNight;
                            break;
                        case EnvironmentalCondition.IsRaining:
                            result.value = envConditions.IsRaining;
                            break;
                        case EnvironmentalCondition.IsFoggy:
                            result.value = envConditions.IsFoggy;
                            break;
                        case EnvironmentalCondition.IsDesert:
                            result.value = envConditions.IsDesert;
                            break;
                        case EnvironmentalCondition.IsSnowing:
                            result.value = envConditions.IsSnowing;
                            break;
                        case EnvironmentalCondition.IsIndoors:
                            result.value = envConditions.IsIndoors;
                            break;
                        default:
                            result.value = false;
                            break;
                    }
                }
                else
                {
                    result.value = false;
                }

                return result;
            }
        }
        return null;
    }
}
