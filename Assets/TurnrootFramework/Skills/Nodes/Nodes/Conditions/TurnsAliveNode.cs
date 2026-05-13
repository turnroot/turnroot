using Turnroot.Utilities;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    /// <summary>
    /// Retrieves the number of turns the unit has survived in the current battle.
    /// </summary>
    [CreateNodeMenu("Conditions/Counters/Turns Alive")]
    [NodeLabel("Gets the number of turns the unit has survived")]
    public class TurnsAliveNode : SkillNode
    {
        [Output]
        public FloatValue TurnCount;

        [Output]
        public BoolValue FirstTurn;

        public override object GetValue(NodePort port)
        {
            var skillGraph = graph as SkillGraph;
            if (skillGraph == null || !Application.isPlaying)
            {
                return port.fieldName switch
                {
                    "TurnCount" => new FloatValue { value = 1f },
                    "FirstTurn" => new BoolValue { value = false },
                    _ => null,
                };
            }

            var context = GetContextFromGraph(skillGraph);
            var character = ConditionHelpers.GetCharacterFromContext(
                context,
                ConditionHelpers.CharacterSource.Unit
            );

            if (character == null)
            {
                "TurnsAlive: Could not retrieve unit from context".LogWarning();
                return port.fieldName switch
                {
                    "TurnCount" => new FloatValue { value = 1f },
                    "FirstTurn" => new BoolValue { value = true },
                    _ => null,
                };
            }

            // Get turns alive from character instance
            int turnsAlive = character.TurnsAliveThisBattle;
            // If TurnsAliveThisBattle is 0, treat as first turn (turn 1)
            if (turnsAlive == 0)
            {
                turnsAlive = 1;
            }

            return port.fieldName switch
            {
                "TurnCount" => new FloatValue { value = turnsAlive },
                "FirstTurn" => new BoolValue { value = turnsAlive == 1 },
                _ => null,
            };
        }
    }
}
