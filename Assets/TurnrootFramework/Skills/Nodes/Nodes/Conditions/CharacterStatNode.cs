using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    /// <summary>
    /// Defines which character to target for stat retrieval.
    /// </summary>
    public enum CharacterTarget
    {
        Unit,
        Enemy,
        Ally,
    }

    /// <summary>
    /// Unified node for retrieving character stats.
    /// Consolidates UnitStatNode and EnemyStatNode into a single configurable node.
    /// Use this node instead of the individual stat nodes for new skill graphs.
    /// </summary>
    [CreateNodeMenu("Conditions/Stats/Character Stat")]
    [NodeLabel("Gets the current (and if bounded, max) stat value of a character")]
    public class CharacterStatNode : SkillNode
    {
        [Tooltip("Which character to get the stat from")]
        public CharacterTarget characterTarget = CharacterTarget.Unit;

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

        /// <summary>
        /// Converts CharacterTarget enum to ConditionHelpers.CharacterSource.
        /// </summary>
        private ConditionHelpers.CharacterSource GetCharacterSource()
        {
            return characterTarget switch
            {
                CharacterTarget.Unit => ConditionHelpers.CharacterSource.Unit,
                CharacterTarget.Enemy => ConditionHelpers.CharacterSource.Enemy,
                CharacterTarget.Ally => ConditionHelpers.CharacterSource.Ally,
                _ => ConditionHelpers.CharacterSource.Unit,
            };
        }

        public override object GetValue(NodePort port)
        {
            var skillGraph = graph as SkillGraph;
            if (skillGraph == null || !Application.isPlaying)
            {
                // Return defaults when not in play mode
                return port.fieldName switch
                {
                    "value" => new FloatValue { value = 0f },
                    "maxValue" => new FloatValue { value = 0f },
                    "percentage" => new FloatValue { value = 0f },
                    "bonus" => new FloatValue { value = 0f },
                    "bonusActive" => new BoolValue { value = false },
                    _ => null,
                };
            }

            var characterSource = GetCharacterSource();

            // Runtime mode - get actual values
            return port.fieldName switch
            {
                "value" => new FloatValue
                {
                    value = ConditionHelpers.GetStatCurrentValue(
                        skillGraph,
                        this,
                        characterSource,
                        selectedStat,
                        isBoundedStat,
                        0f
                    ),
                },
                "maxValue" => new FloatValue
                {
                    value = ConditionHelpers.GetStatMaxValue(
                        skillGraph,
                        this,
                        characterSource,
                        selectedStat,
                        0f
                    ),
                },
                "percentage" => new FloatValue
                {
                    value = ConditionHelpers.GetStatPercentage(
                        skillGraph,
                        this,
                        characterSource,
                        selectedStat,
                        100f
                    ),
                },
                "bonus" => new FloatValue
                {
                    value = ConditionHelpers.GetStatBonus(
                        skillGraph,
                        this,
                        characterSource,
                        selectedStat,
                        isBoundedStat
                    ),
                },
                "bonusActive" => new BoolValue
                {
                    value = ConditionHelpers.GetStatBonusActive(
                        skillGraph,
                        this,
                        characterSource,
                        selectedStat,
                        isBoundedStat
                    ),
                },
                _ => null,
            };
        }
    }
}
