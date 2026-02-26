using Turnroot.Characters;
using Turnroot.Utilities;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    /// <summary>
    /// Condition node that checks how many adjacent allied units have a specific badge text.
    /// </summary>
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
                "AllyBadgeIs: Could not get SkillGraph".LogWarning();
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
