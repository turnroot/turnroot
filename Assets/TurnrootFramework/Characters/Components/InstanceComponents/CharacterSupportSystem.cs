using Turnroot.Characters.Components.Support;
using UnityEngine;

namespace Turnroot.Characters
{
    /// <summary>
    /// Handles support relationships between characters.
    /// </summary>
    public partial class CharacterInstance
    {
        #region Support Relationships

        /// <summary>
        /// Get support relationship with a specific character.
        /// </summary>
        public SupportRelationshipInstance GetSupportRelationship(CharacterData character) =>
            _supportRelationships.Find(s => s.Character == character);

        /// <summary>
        /// Add a new support relationship from a template.
        /// </summary>
        public void AddSupportRelationship(SupportRelationship template)
        {
            // Validate that the support relationship is not with the same character
            if (template.Character == _characterTemplate)
            {
                Debug.LogWarning(
                    $"Cannot add support relationship with the same character ({template.Character.name})"
                );
                return;
            }

            // Check if relationship already exists
            if (GetSupportRelationship(template.Character) == null)
            {
                _supportRelationships.Add(new SupportRelationshipInstance(template));
            }
        }

        /// <summary>
        /// Increase support level with another character.
        /// </summary>
        internal void IncreaseSupport(CharacterData character, int amount)
        {
            var relationship = GetSupportRelationship(character);
            if (relationship != null)
            {
                relationship.Increase(amount);
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning($"No support relationship found with {character.FullName}");
#endif
                AddSupportRelationship(new SupportRelationship { Character = character });
                GetSupportRelationship(character)?.Increase(amount);
            }
        }

        /// <summary>
        /// Remove support relationship with a character.
        /// </summary>
        public void RemoveSupportRelationship(CharacterData character) =>
            _ = _supportRelationships.RemoveAll(s => s.Character == character);

        #endregion
    }
}
