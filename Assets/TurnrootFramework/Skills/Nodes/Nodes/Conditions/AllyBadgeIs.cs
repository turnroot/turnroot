using Turnroot.Characters;
using Turnroot.Utilities;
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
            if (context?.Participants?.AdjacentUnits == null)
            {
                return new FloatValue();
            }

            var matchCount = 0;
            var adjacentAllies = ListPool<CharacterInstance>.Get();
            context.Participants.AdjacentUnits.GetAdjacentAlliesNonAlloc(context, adjacentAllies);

            foreach (var unit in adjacentAllies)
            {
                string badgeText = unit.CharacterTemplate.BadgeText ?? "";
                if (badgeText.Equals(BadgeText))
                {
                    matchCount++;
                }
            }

            ListPool<CharacterInstance>.Return(adjacentAllies);
            return new FloatValue { value = matchCount };
        }
    }
}
