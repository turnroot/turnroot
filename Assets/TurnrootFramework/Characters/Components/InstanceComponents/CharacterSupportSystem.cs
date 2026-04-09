using Turnroot.Characters.Components.Support;
using Turnroot.Utilities;

namespace Turnroot.Characters
{
    /// <summary>
    /// Handles support relationships between characters.
    /// </summary>
    public partial class CharacterInstance
    {
        #region Support Relationships
        public SupportRelationshipInstance GetSupportRelationship(CharacterData character) =>
            _supportRelationships.Find(s => s.Character == character);

        public OperationResult AddSupportRelationship(SupportRelationship template)
        {
            bool ok = ValidationHelper.ValidateNotNull(
                "CharacterInstance.AddSupportRelationship",
                out var missing,
                (template, nameof(template)),
                (template?.Character, "template.Character")
            );

            if (!ok)
            {
                return OperationResult.Failure(
                    $"AddSupportRelationship failed: missing {string.Join(", ", missing)}"
                );
            }

            // Validate that the support relationship is not with the same character
            if (template.Character == _characterTemplate)
            {
                return OperationResult.Failure(
                    $"Cannot add support relationship with the same character ({template.Character.name})"
                );
            }

            // Check if relationship already exists
            if (GetSupportRelationship(template.Character) == null)
            {
                _supportRelationships.Add(new SupportRelationshipInstance(template));
            }

            return OperationResult.Successful();
        }

        internal OperationResult IncreaseSupport(CharacterData character, float amount)
        {
            bool ok = ValidationHelper.ValidateNotNull(
                "CharacterInstance.IncreaseSupport",
                out var missing,
                (character, nameof(character))
            );

            if (!ok)
            {
                return OperationResult.Failure(
                    $"IncreaseSupport failed: missing {string.Join(", ", missing)}"
                );
            }

            var relationship = GetSupportRelationship(character);
            if (relationship != null)
            {
                relationship.Increase(amount);
                return OperationResult.Successful();
            }

            $"Support relationship with {character.name} does not exist. Creating new relationship.".LogInfo();
            var res = AddSupportRelationship(new SupportRelationship { Character = character });
            if (!res.Success)
            {
                return res;
            }
            GetSupportRelationship(character)?.Increase(amount);
            return OperationResult.Successful();
        }

        public OperationResult RemoveSupportRelationship(CharacterData character)
        {
            bool ok = ValidationHelper.ValidateNotNull(
                "CharacterInstance.RemoveSupportRelationship",
                out var missing,
                (character, nameof(character))
            );

            if (!ok)
            {
                return OperationResult.Failure(
                    $"RemoveSupportRelationship failed: missing {string.Join(", ", missing)}"
                );
            }

            _ = _supportRelationships.RemoveAll(s => s.Character == character);
            return OperationResult.Successful();
        }

        #endregion
    }
}
