using Turnroot.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    [CreateNodeMenu("Conditions/Unit/Allies' Badge Is")]
    [NodeLabel("Check adjacent allies' badge")]
    public class AllyBadgeIs : SkillNode
    {
        [Output]
        FloatValue MatchCount;
        public string BadgeText;

        public override object GetValue(NodePort port)
        {
            // Get context from the graph
            var skillGraph = graph as SkillGraph;
            if (skillGraph == null)
            {
                Debug.LogWarning("AllyBadgeIs: Could not get SkillGraph");
                return new FloatValue();
            }

            var context = GetContextFromGraph(skillGraph);
            if (context?.AdjacentUnits == null)
            {
                return new FloatValue();
            }

            var matchCount = 0;
            foreach (var unit in context.AdjacentUnits.GetAdjacentAllies(context))
            {
                string badgeText = unit.CharacterTemplate.BadgeText ?? "";
                if (badgeText.Equals(BadgeText))
                {
                    matchCount++;
                }
            }

            return new FloatValue { value = matchCount };
        }
    }
}
