using Turnroot.Utilities;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    /// <summary>
    /// Retrieves the current turn number in the battle.
    /// </summary>
    [CreateNodeMenu("Conditions/Counters/Turn Count")]
    [NodeLabel("Gets the current turn count")]
    public class TurnCountNode : SkillNode
    {
        [Output]
        private FloatValue value;

        public override object GetValue(NodePort port)
        {
            if (port.fieldName != "value")
            {
                return null;
            }

            var skillGraph = graph as SkillGraph;
            if (skillGraph == null || !Application.isPlaying)
            {
                return new FloatValue { value = 1f }; // Default to turn 1 in editor
            }

            var context = GetContextFromGraph(skillGraph);
            if (context == null)
            {
                "TurnCount: Could not retrieve context from graph".LogWarning();
                return new FloatValue { value = 1f };
            }

            int turnNumber = context.Brain?.battleBrain?.CurrentTurnNumber ?? 1;
            return new FloatValue { value = turnNumber };
        }
    }
}
