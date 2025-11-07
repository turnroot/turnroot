using System;
using Assets.Prototypes.Characters;
using Assets.Prototypes.Characters.Stats;
using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Enemy Stat")]
[NodeLabel(
    "Gets the current (and if the stat has a max value, the max) stat value of the enemy (if skill applies to multiple enemies, evaluates on the first targeted)"
)]
public class EnemyStat : SkillNode
{
    [Tooltip("The stat to retrieve from the enemy")]
    public string selectedStat = "Health";

    [Tooltip(
        "Whether this is a bounded stat (like Health, Level) or unbounded (like Strength, Defense)"
    )]
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
            statValue.value = !Application.isPlaying ? test : GetRuntimeStatValue();

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
                // At runtime, get actual stat max from the first enemy
                if (
                    graph is SkillGraph skillGraph &&
                    GetContextFromGraph(skillGraph) is var contextFromGraph &&
                    contextFromGraph != null &&
                    contextFromGraph.Targets != null &&
                    contextFromGraph.Targets.Count > 0 &&
                    contextFromGraph.Targets[0] is var characterInstance &&
                    characterInstance != null &&
                    isBoundedStat &&
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

                // Fallback to test value if context or stat not available
                Debug.LogWarning(
                    $"EnemyStat: Unable to retrieve runtime max value for {selectedStat}, returning test value."
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
                // At runtime, get actual stat percentage from the first enemy
                if (graph is SkillGraph skillGraph)
                {
                    var contextFromGraph = GetContextFromGraph(skillGraph);
                    if (contextFromGraph != null && contextFromGraph.Targets != null)
                    {
                        var enemyInstances = contextFromGraph.Targets;
                        if (enemyInstances != null && enemyInstances.Count > 0)
                        {
                            var characterInstance = enemyInstances[0];
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
                }

                // Fallback to 100% if context or stat not available
                Debug.LogWarning(
                    $"EnemyStat: Unable to retrieve runtime percentage for {selectedStat}, returning 100%."
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
                // At runtime, get actual bonus from the first enemy
                if (graph is SkillGraph skillGraph)
                {
                    var contextFromGraph = GetContextFromGraph(skillGraph);
                    if (contextFromGraph != null && contextFromGraph.Targets != null)
                    {
                        var enemyInstances = contextFromGraph.Targets;
                        if (enemyInstances != null && enemyInstances.Count > 0)
                        {
                            var characterInstance = enemyInstances[0];
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
                                        var stat = characterInstance.GetUnboundedStat(
                                            unboundedType
                                        );
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
                    if (contextFromGraph != null && contextFromGraph.Targets != null)
                    {
                        var enemyInstances = contextFromGraph.Targets;
                        if (enemyInstances != null && enemyInstances.Count > 0)
                        {
                            var characterInstance = enemyInstances[0];
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
                                        var stat = characterInstance.GetUnboundedStat(
                                            unboundedType
                                        );
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
            if (contextFromGraph != null && contextFromGraph.Targets != null)
            {
                var enemyInstances = contextFromGraph.Targets;
                if (enemyInstances != null && enemyInstances.Count > 0)
                {
                    // return the stat of the first enemy instance
                    var characterInstance = enemyInstances[0];
                    if (characterInstance != null)
                    {
                        if (isBoundedStat)
                        {
                            // Try to parse as BoundedStatType
                            if (
                                System.Enum.TryParse(
                                    selectedStat,
                                    out BoundedStatType boundedStatType
                                )
                            )
                            {
                                var boundedStat = characterInstance.GetBoundedStat(boundedStatType);
                                if (boundedStat != null)
                                {
                                    return boundedStat.Current;
                                }
                            }
                        }
                        else
                        {
                            // Try to parse as UnboundedStatType
                            if (
                                System.Enum.TryParse(
                                    selectedStat,
                                    out UnboundedStatType unboundedStatType
                                )
                            )
                            {
                                var unboundedStat = characterInstance.GetUnboundedStat(
                                    unboundedStatType
                                );
                                if (unboundedStat != null)
                                {
                                    return unboundedStat.Current;
                                }
                            }
                        }
                    }
                }
            }
        }

        // Fallback to default if context or stat not available
        Debug.LogWarning(
            $"EnemyStat: Unable to retrieve runtime value for {selectedStat}, returning test value."
        );
        return test;
    }
}
