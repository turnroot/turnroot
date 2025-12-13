using Turnroot.Characters;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    [CreateNodeMenu("Conditions/Species/Species Is")]
    [NodeLabel("Checks if a character's species matches the specified name")]
    public class SpeciesIsNode : SkillNode
    {
        [Tooltip("The species name to check for (e.g., Human, Beast, Dragon, Manakete)")]
        public string speciesName = "";

        [Output]
        public BoolValue UnitMatches;

        [Output]
        public BoolValue EnemyMatches;

        [Output]
        public BoolValue AllyMatches;

        public override object GetValue(NodePort port)
        {
            var skillGraph = graph as SkillGraph;
            if (skillGraph == null || !Application.isPlaying)
            {
                // Return false in editor mode
                return new BoolValue { value = false };
            }

            var context = GetContextFromGraph(skillGraph);
            if (context == null)
            {
                return new BoolValue { value = false };
            }

            // Determine which character to check based on port
            var characterSource = port.fieldName switch
            {
                "UnitMatches" => ConditionHelpers.CharacterSource.Unit,
                "EnemyMatches" => ConditionHelpers.CharacterSource.Enemy,
                "AllyMatches" => ConditionHelpers.CharacterSource.Ally,
                _ => (ConditionHelpers.CharacterSource?)null,
            };

            if (!characterSource.HasValue)
            {
                return new BoolValue { value = false };
            }

            var character = ConditionHelpers.GetCharacterFromContext(context, characterSource.Value);
            if (character == null)
            {
                return new BoolValue { value = false };
            }

            bool matches = CheckSpeciesType(character, speciesName);
            return new BoolValue { value = matches };
        }

        /// <summary>
        /// Checks if a character's species matches the given species name.
        /// </summary>
        private static bool CheckSpeciesType(CharacterInstance character, string targetSpeciesName)
        {
            if (string.IsNullOrEmpty(targetSpeciesName))
            {
                return false;
            }

            var species = character.CharacterTemplate?.Species;
            if (species == null)
            {
                return false;
            }

            // Check the Name property, asset name, and Id for flexibility
            return species.Name?.Equals(targetSpeciesName, System.StringComparison.OrdinalIgnoreCase) == true
                || species.name?.Equals(targetSpeciesName, System.StringComparison.OrdinalIgnoreCase) == true
                || species.Id?.Equals(targetSpeciesName, System.StringComparison.OrdinalIgnoreCase) == true;
        }
    }
}
