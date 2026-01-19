using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    [RequireComponent(typeof(LongTermMemory))]
    public partial class CharactersBrain : BrainComponent
    {
        #region Support System API

        public void IncreaseSupport(
            CharacterInstance character,
            CharacterData targetCharacter,
            int amount
        )
        {
            if (!Validate(character, targetCharacter))
            {
                return;
            }

            character.IncreaseSupport(targetCharacter, amount);
            _brain?.PublishSupportIncreased(character, targetCharacter, amount);
            TurnrootLogger.Log(
                $"Support increased between {character.CharacterTemplate?.DisplayName} and {targetCharacter.DisplayName}"
            );
        }

        public void AddSupportRelationship(
            CharacterInstance character,
            Characters.Components.Support.SupportRelationship template
        )
        {
            if (!Validate(character, template) || template?.Character == null)
            {
                return;
            }

            character.AddSupportRelationship(template);
            var added = character.GetSupportRelationship(template.Character);
            if (added != null)
            {
                _brain?.PublishSupportRelationshipAdded(character, added);
            }
            TurnrootLogger.Log(
                $"Added support relationship for {template.Character.DisplayName} on {character.Id}"
            );
        }

        public void RemoveSupportRelationship(CharacterInstance character, CharacterData target)
        {
            if (!Validate(character, target))
            {
                return;
            }

            character.RemoveSupportRelationship(target);
            _brain?.PublishSupportRelationshipRemoved(character, target);
            TurnrootLogger.Log(
                $"Removed support relationship for {target.DisplayName} on {character.Id}"
            );
        }

        #endregion
    }
}
