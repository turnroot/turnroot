using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
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
        IsSunny,
        IsCloudy,
        IsSnowing,
        IsIndoors,
        IsSmoky,
        IsUnderground,
        IsUnderwater,
        IsRocky,
        IsSwampy,
        IsVolcanic,
    }

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
                        case EnvironmentalCondition.IsSunny:
                            result.value = envConditions.IsSunny;
                            break;
                        case EnvironmentalCondition.IsCloudy:
                            result.value = envConditions.IsCloudy;
                            break;
                        case EnvironmentalCondition.IsSnowing:
                            result.value = envConditions.IsSnowing;
                            break;
                        case EnvironmentalCondition.IsIndoors:
                            result.value = envConditions.IsIndoors;
                            break;
                        case EnvironmentalCondition.IsSmoky:
                            result.value = envConditions.IsSmoky;
                            break;
                        case EnvironmentalCondition.IsUnderground:
                            result.value = envConditions.IsUnderground;
                            break;
                        case EnvironmentalCondition.IsUnderwater:
                            result.value = envConditions.IsUnderwater;
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
