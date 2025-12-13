using Turnroot.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    /// <summary>
    /// Base class for stat condition nodes (UnitStatNode, EnemyStatNode).
    /// Provides shared stat output functionality.
    /// </summary>
    public abstract class StatNodeBase : SkillNode
    {
        [Tooltip("The stat to retrieve")]
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

        /// <summary>
        /// The character source to retrieve stats from (Unit or Enemy).
        /// </summary>
        protected abstract ConditionHelpers.CharacterSource CharacterSource { get; }

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
                        CharacterSource,
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
                        CharacterSource,
                        selectedStat,
                        test
                    ),
                },
                "percentage" => new FloatValue
                {
                    value = ConditionHelpers.GetStatPercentage(
                        skillGraph,
                        this,
                        CharacterSource,
                        selectedStat,
                        100f
                    ),
                },
                "bonus" => new FloatValue
                {
                    value = ConditionHelpers.GetStatBonus(
                        skillGraph,
                        this,
                        CharacterSource,
                        selectedStat,
                        isBoundedStat
                    ),
                },
                "bonusActive" => new BoolValue
                {
                    value = ConditionHelpers.GetStatBonusActive(
                        skillGraph,
                        this,
                        CharacterSource,
                        selectedStat,
                        isBoundedStat
                    ),
                },
                _ => null,
            };
        }
    }
}
