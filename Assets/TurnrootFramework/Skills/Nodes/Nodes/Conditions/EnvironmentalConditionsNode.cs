using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    /// <summary>
    /// Defines the types of environmental conditions that can be checked in skill nodes.
    /// </summary>
    public enum EnvironmentalCondition
    {
        IsVeryHot,
        IsVeryCold,
        IsNight,
        IsSunset,
        IsDawn,
        IsRaining,
        IsFoggy,
        IsStormy,
        IsWindy,
        HasSunlight,
        IsRocky,
        IsSwampy,
        IsVolcanic,
    }

    /// <summary>
    /// Condition node that checks various environmental conditions such as weather, time of day, and terrain properties.
    /// </summary>
    [CreateNodeMenu("Conditions/Environment/Environmental Conditions")]
    [NodeLabel("Checks environmental conditions")]
    public class EnvironmentalConditionsNode : SkillNode
    {
        [Output]
        public BoolValue Condition;
        public EnvironmentalCondition conditionToCheck;

        public override object GetValue(NodePort port)
        {
            if (port.fieldName == "Condition" && graph is SkillGraph skillGraph)
            {
                BoolValue result = new();
                var contextFromGraph = GetContextFromGraph(skillGraph);
                var envConditions = contextFromGraph?.EnvironmentalConditions;

                if (envConditions != null)
                {
                    switch (conditionToCheck)
                    {
                        case EnvironmentalCondition.IsVeryHot:
                            result.value = envConditions.IsVeryHot;
                            break;
                        case EnvironmentalCondition.IsVeryCold:
                            result.value = envConditions.IsVeryCold;
                            break;
                        case EnvironmentalCondition.IsNight:
                            result.value = envConditions.IsNight;
                            break;
                        case EnvironmentalCondition.IsSunset:
                            result.value = envConditions.IsSunset;
                            break;
                        case EnvironmentalCondition.IsDawn:
                            result.value = envConditions.IsDawn;
                            break;
                        case EnvironmentalCondition.IsRaining:
                            result.value = envConditions.IsRaining;
                            break;
                        case EnvironmentalCondition.IsFoggy:
                            result.value = envConditions.IsFoggy;
                            break;
                        case EnvironmentalCondition.IsStormy:
                            result.value = envConditions.IsStormy;
                            break;
                        case EnvironmentalCondition.IsWindy:
                            result.value = envConditions.IsWindy;
                            break;
                        case EnvironmentalCondition.HasSunlight:
                            result.value = envConditions.HasSunlight;
                            break;
                        case EnvironmentalCondition.IsRocky:
                            result.value = envConditions.IsRocky;
                            break;
                        case EnvironmentalCondition.IsSwampy:
                            result.value = envConditions.IsSwampy;
                            break;
                        case EnvironmentalCondition.IsVolcanic:
                            result.value = envConditions.IsVolcanic;
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
            return null;
        }
    }
}
