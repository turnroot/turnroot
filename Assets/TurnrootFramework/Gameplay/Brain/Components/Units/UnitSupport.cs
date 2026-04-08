using Turnroot.Characters;
using Turnroot.Characters.Stats;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Manages character support relationships, including increasing support levels and handling support-related operations.
    /// </summary>
    [RequireComponent(typeof(LongTermMemory))]
    public partial class CharactersBrain : BrainComponent
    {
        #region Support System API

        public void IncreaseSupport(
            CharacterInstance character,
            CharacterData targetCharacter,
            float amount
        )
        {
            if (!Validate(character, targetCharacter))
            {
                return;
            }

            character.IncreaseSupport(targetCharacter, amount);
            Brain.PublishSupportIncreased(character, targetCharacter, amount);

            $"Support increased between {character.CharacterTemplate?.DisplayName} and {targetCharacter.DisplayName}".LogInfo();
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
                Brain.PublishSupportRelationshipAdded(character, added);
            }

            $"Added support relationship for {template.Character.DisplayName} on {character.Id}".LogInfo();
        }

        public void RemoveSupportRelationship(CharacterInstance character, CharacterData target)
        {
            if (!Validate(character, target))
            {
                return;
            }

            character.RemoveSupportRelationship(target);
            Brain.PublishSupportRelationshipRemoved(character, target);

            $"Removed support relationship for {target.DisplayName} on {character.Id}".LogInfo();
        }

        #endregion

        #region Hub Support Handlers

        internal void HandleHubCharacterInteracted(CharacterInstance visitedCharacter)
        {
            AwardHubSupportPointsAvatarPairing(
                visitedCharacter,
                GameplayGeneralSettings.Instance?.HubInteractionSupportPoints ?? 0f
            );
        }

        internal void HandleHubCharacterTalked(CharacterInstance visitedCharacter)
        {
            AwardHubSupportPointsAvatarPairing(
                visitedCharacter,
                GameplayGeneralSettings.Instance?.HubInteractionTalkSupportPoints ?? 0f
            );
        }

        public void AwardHubSupportPointsAvatarPairing(
            CharacterInstance visitedCharacter,
            float basePoints
        )
        {
            if (visitedCharacter?.CharacterTemplate == null || basePoints == 0f)
            {
                return;
            }

            var avatar = _gamewideContextBrain?.GetOrCreateAvatarInstance();
            if (avatar?.CharacterTemplate == null)
            {
                return;
            }

            var settings = GameplayGeneralSettings.Instance;
            var charmA = avatar.GetUnboundedStat(UnboundedStatType.Charm)?.Get() ?? 0f;
            var charmB = visitedCharacter.GetUnboundedStat(UnboundedStatType.Charm)?.Get() ?? 0f;
            var charmMultiplier = 1f + ((charmA + charmB) / 25f);

            var speed = settings.SupportGrowthSpeed;
            SupportRelationshipTable.SupportPairing? pairing =
                SupportRelationshipTable.Instance.TryGetPairing(
                    avatar.CharacterTemplate,
                    visitedCharacter.CharacterTemplate,
                    out var foundPairing
                )
                    ? foundPairing
                    : null;
            if (pairing != null)
            {
                speed *=
                    pairing.Value.SupportGainMultiplier > 0f
                        ? pairing.Value.SupportGainMultiplier
                        : 1f;
                var finalPoints = basePoints * charmMultiplier * speed;
                $"Support gain multiplier from pairing: {pairing.Value.SupportGainMultiplier}".LogInfo();
                $"Final support points awarded: {finalPoints}".LogInfo();
                IncreaseSupport(avatar, visitedCharacter.CharacterTemplate, finalPoints);
            }
            else
            {
                $"Warning: No support pairing found between {avatar.CharacterTemplate.DisplayName} and {visitedCharacter.CharacterTemplate.DisplayName}. Using default support gain multiplier.".LogWarning();
                var finalPoints = basePoints * charmMultiplier * speed;
                $"Final support points awarded: {finalPoints}".LogInfo();

                IncreaseSupport(avatar, visitedCharacter.CharacterTemplate, finalPoints);
            }
        }

        #endregion
    }
}
