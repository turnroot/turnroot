using Assets.Prototypes.Characters;
using Assets.Prototypes.Characters.Stats;
using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Unit Stat")]
[NodeLabel("Gets the current (and if the stat has a max value, the max) stat value of a unit")]
public class UnitStat : SkillNode
{
    [Tooltip("The stat to retrieve from the unit")]
    public string selectedStat = "Health";
    public bool isBoundedStat = true;

    [Output]
    public FloatValue value;

    [Output]
    public FloatValue maxValue;

    [Output]
    public FloatValue percentage;

    [Output]
    public FloatValue bonus;

    [Output]
    public BoolValue bonusActive;

    [Tooltip("Test value used in editor mode")]
    public float test = 100f;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "value")
        {
            FloatValue statValue = new();

            // In editor mode, return the test value
            if (!Application.isPlaying)
            {
                statValue.value = test;
            }
            else
            {
                // At runtime, get actual stat from the UnitInstance
                statValue.value = GetRuntimeStatValue();
            }

            return statValue;
        }
        else if (port.fieldName == "maxValue")
        {
            FloatValue statMaxValue = new();

            // In editor mode, return the test value
            if (!Application.isPlaying)
            {
                statMaxValue.value = test;
            }
            else
            {
                // At runtime, get actual stat max from the UnitInstance
                if (graph is SkillGraph skillGraph)
                {
                    var contextFromGraph = GetContextFromGraph(skillGraph);
                    if (contextFromGraph != null && contextFromGraph.UnitInstance != null)
                    {
                        var characterInstance = contextFromGraph.UnitInstance;
                        if (characterInstance != null)
                        {
                            if (isBoundedStat)
                            {
                                // Try to parse as BoundedStatType
                                if (
                                    System.Enum.TryParse<BoundedStatType>(
                                        selectedStat,
                                        out var boundedType
                                    )
                                )
                                {
                                    var stat = characterInstance.GetBoundedStat(boundedType);
                                    if (stat != null)
                                    {
                                        statMaxValue.value = stat.Max;
                                        return statMaxValue;
                                    }
                                }
                            }
                        }
                    }
                }

                // Fallback to test value if context or stat not available
                Debug.LogWarning(
                    $"UnitStat: Unable to retrieve runtime max value for {selectedStat}, returning test value."
                );
                statMaxValue.value = test;
            }

            return statMaxValue;
        }
        else if (port.fieldName == "percentage")
        {
            FloatValue statPercentage = new();

            // In editor mode, return 100%
            if (!Application.isPlaying)
            {
                statPercentage.value = 100f;
            }
            else
            {
                // At runtime, get actual stat percentage from the UnitInstance
                if (graph is SkillGraph skillGraph)
                {
                    var contextFromGraph = GetContextFromGraph(skillGraph);
                    if (contextFromGraph != null && contextFromGraph.UnitInstance != null)
                    {
                        var characterInstance = contextFromGraph.UnitInstance;
                        if (characterInstance != null)
                        {
                            if (isBoundedStat)
                            {
                                // Try to parse as BoundedStatType
                                if (
                                    System.Enum.TryParse<BoundedStatType>(
                                        selectedStat,
                                        out var boundedType
                                    )
                                )
                                {
                                    var stat = characterInstance.GetBoundedStat(boundedType);
                                    if (stat != null)
                                    {
                                        statPercentage.value = stat.Ratio * 100f;
                                        return statPercentage;
                                    }
                                }
                            }
                        }
                    }
                }

                // Fallback to 100% if context or stat not available
                Debug.LogWarning(
                    $"UnitStat: Unable to retrieve runtime percentage for {selectedStat}, returning 100%."
                );
                statPercentage.value = 100f;
            }

            return statPercentage;
        }
        else if (port.fieldName == "bonus")
        {
            FloatValue bonusValue = new();

            // In editor mode, return 0
            if (!Application.isPlaying)
            {
                bonusValue.value = 0f;
            }
            else
            {
                // At runtime, get actual bonus from the UnitInstance
                if (graph is SkillGraph skillGraph)
                {
                    var contextFromGraph = GetContextFromGraph(skillGraph);
                    if (contextFromGraph != null && contextFromGraph.UnitInstance != null)
                    {
                        var characterInstance = contextFromGraph.UnitInstance;
                        if (characterInstance != null)
                        {
                            if (isBoundedStat)
                            {
                                if (
                                    System.Enum.TryParse<BoundedStatType>(
                                        selectedStat,
                                        out var boundedType
                                    )
                                )
                                {
                                    var stat = characterInstance.GetBoundedStat(boundedType);
                                    if (stat != null)
                                    {
                                        bonusValue.value = stat.Bonus;
                                        return bonusValue;
                                    }
                                }
                            }
                            else
                            {
                                if (
                                    System.Enum.TryParse<UnboundedStatType>(
                                        selectedStat,
                                        out var unboundedType
                                    )
                                )
                                {
                                    var stat = characterInstance.GetUnboundedStat(unboundedType);
                                    if (stat != null)
                                    {
                                        bonusValue.value = stat.Bonus;
                                        return bonusValue;
                                    }
                                }
                            }
                        }
                    }
                }

                bonusValue.value = 0f;
            }

            return bonusValue;
        }
        else if (port.fieldName == "bonusActive")
        {
            BoolValue bonusActiveValue = new();

            // In editor mode, return false
            if (!Application.isPlaying)
            {
                bonusActiveValue.value = false;
            }
            else
            {
                // At runtime, check if bonus is non-zero
                if (graph is SkillGraph skillGraph)
                {
                    var contextFromGraph = GetContextFromGraph(skillGraph);
                    if (contextFromGraph != null && contextFromGraph.UnitInstance != null)
                    {
                        var characterInstance = contextFromGraph.UnitInstance;
                        if (characterInstance != null)
                        {
                            if (isBoundedStat)
                            {
                                if (
                                    System.Enum.TryParse<BoundedStatType>(
                                        selectedStat,
                                        out var boundedType
                                    )
                                )
                                {
                                    var stat = characterInstance.GetBoundedStat(boundedType);
                                    if (stat != null)
                                    {
                                        bonusActiveValue.value = stat.Bonus != 0;
                                        return bonusActiveValue;
                                    }
                                }
                            }
                            else
                            {
                                if (
                                    System.Enum.TryParse<UnboundedStatType>(
                                        selectedStat,
                                        out var unboundedType
                                    )
                                )
                                {
                                    var stat = characterInstance.GetUnboundedStat(unboundedType);
                                    if (stat != null)
                                    {
                                        bonusActiveValue.value = stat.Bonus != 0;
                                        return bonusActiveValue;
                                    }
                                }
                            }
                        }
                    }
                }

                bonusActiveValue.value = false;
            }

            return bonusActiveValue;
        }
        return null;
    }

    private float GetRuntimeStatValue()
    {
        if (graph is SkillGraph skillGraph)
        {
            var contextFromGraph = GetContextFromGraph(skillGraph);
            if (contextFromGraph != null && contextFromGraph.UnitInstance != null)
            {
                var characterInstance = contextFromGraph.UnitInstance;
                if (characterInstance != null)
                {
                    if (isBoundedStat)
                    {
                        // Try to parse as BoundedStatType
                        if (
                            System.Enum.TryParse<BoundedStatType>(selectedStat, out var boundedType)
                        )
                        {
                            var stat = characterInstance.GetBoundedStat(boundedType);
                            if (stat != null)
                            {
                                return stat.Current;
                            }
                        }
                    }
                    else
                    {
                        // Try to parse as UnboundedStatType
                        if (
                            System.Enum.TryParse<UnboundedStatType>(
                                selectedStat,
                                out var unboundedType
                            )
                        )
                        {
                            var stat = characterInstance.GetUnboundedStat(unboundedType);
                            if (stat != null)
                            {
                                return stat.Current;
                            }
                        }
                    }
                }
            }
        }

        // Fallback to test value if context or stat not available
        Debug.LogWarning(
            $"UnitStat: Unable to retrieve runtime value for {selectedStat}, returning test value."
        );
        return test;
    }
}
