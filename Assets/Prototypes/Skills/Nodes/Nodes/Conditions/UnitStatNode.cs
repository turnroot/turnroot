using Assets.Prototypes.Characters;
using Assets.Prototypes.Characters.Stats;
using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Unit/Unit Stat")]
[NodeLabel("Gets the current (and if the stat has a max value, the max) stat value of a unit")]
public class UnitStatNode : SkillNode
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
        var skillGraph = graph as SkillGraph;
        if (skillGraph == null || !Application.isPlaying)
        {
            // Return test values in editor mode
            return port.fieldName switch
            {
                "value" => new FloatValue { value = test },
                "maxValue" => new FloatValue { value = test },
                "percentage" => new FloatValue { value = 100f },
                "bonus" => new FloatValue { value = 0f },
                "bonusActive" => new BoolValue { value = false },
                _ => null,
            };
        }

        // Runtime mode - get actual values
        return port.fieldName switch
        {
            "value" => new FloatValue
            {
                value = ConditionHelpers.GetStatCurrentValue(
                    skillGraph,
                    this,
                    ConditionHelpers.CharacterSource.Unit,
                    selectedStat,
                    isBoundedStat,
                    test
                ),
            },
            "maxValue" => new FloatValue
            {
                value = ConditionHelpers.GetStatMaxValue(
                    skillGraph,
                    this,
                    ConditionHelpers.CharacterSource.Unit,
                    selectedStat,
                    test
                ),
            },
            "percentage" => new FloatValue
            {
                value = ConditionHelpers.GetStatPercentage(
                    skillGraph,
                    this,
                    ConditionHelpers.CharacterSource.Unit,
                    selectedStat,
                    100f
                ),
            },
            "bonus" => new FloatValue
            {
                value = ConditionHelpers.GetStatBonus(
                    skillGraph,
                    this,
                    ConditionHelpers.CharacterSource.Unit,
                    selectedStat,
                    isBoundedStat
                ),
            },
            "bonusActive" => new BoolValue
            {
                value = ConditionHelpers.GetStatBonusActive(
                    skillGraph,
                    this,
                    ConditionHelpers.CharacterSource.Unit,
                    selectedStat,
                    isBoundedStat
                ),
            },
            _ => null,
        };
    }
}
