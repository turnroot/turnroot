using NaughtyAttributes;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts.UI;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    [RequireComponent(typeof(LongTermMemory))]
    public partial class CharactersBrain : BrainComponent
    {
        public bool CanRecruit(CharacterInstance character)
        {
            var status = false;
            var characterData = character.CharacterTemplate;
            if (characterData == null)
            {
                "CharactersBrain.CanRecruit: Character instance has no template, cannot determine recruitability.".LogWarning(
                    "CharactersBrain"
                );
                status = false;
            }

            if (characterData.WillJoinIfAllyIsAlreadyRecruited)
            {
                var requiredAlly = characterData.SpecificAllyRequiredForRecruitment;
                var roster =
                    _brain.gamewideContextBrain.CreateOrRecallGamewidePersistentPlayerRoster();
                if (roster != null)
                {
                    // Check if the required ally is in the roster
                    var rosterInstance = _gamewideContextBrain?.GetOrCreatePlayerTeamRoster(roster);
                    return rosterInstance?.GetInstanceFor(requiredAlly) != null;
                    ; // If true,  return early, the character is recruitable regardless of other conditions
                }
            }

            if (characterData.AvatarMustHaveMinimumExperienceLevelsToRecruit)
            {
                // check avatar experience ranks against characterData.AvatarMinimumExperienceRanksToRecruit
                var avatar = _gamewideContextBrain?.GetOrCreateAvatarInstance();
                if (avatar == null)
                {
                    status = false;
                }
                else
                {
                    foreach (var required in characterData.AvatarMinimumExperienceRanksToRecruit)
                    {
                        var avatarRank = avatar.ExperienceRanks.Find(r =>
                            r.ExperienceTypeId == required.ExperienceTypeId
                        );
                        if (
                            avatarRank == null
                            || required.Rank.CompareTo(avatarRank.Rank.Value) > 0
                        )
                        {
                            status = false;
                            break;
                        }
                    }
                }
            }

            if (characterData.RecruitRequiresMinSupportLevel)
            {
                // check avatar support relationship with character against characterData.RecruitSupportRelationshipMinRank
                var avatar = _gamewideContextBrain?.GetOrCreateAvatarInstance();
                if (avatar == null)
                {
                    status = false;
                }
                else
                {
                    var supportRel = avatar.GetSupportRelationship(characterData);
                    if (
                        supportRel == null
                        || characterData.RecruitSupportRelationshipMinRank.CompareTo(
                            supportRel.CurrentLevel
                        ) > 0
                    )
                    {
                        status = false;
                    }
                }
            }
            return status;
        }

        public OperationResult Recruit(CharacterInstance character)
        {
            if (!CanRecruit(character))
            {
                return OperationResult.Failure(
                    $"Cannot recruit {character.CharacterTemplate.DisplayName}: requirements not met."
                );
            }

            var roster = _brain.gamewideContextBrain.CreateOrRecallGamewidePersistentPlayerRoster();
            if (roster == null)
            {
                return OperationResult.Failure("Could not access player roster.");
            }

            var rosterInstance = _gamewideContextBrain?.GetPersistentPlayerTeamRosterInstance();
            if (rosterInstance == null)
            {
                return OperationResult.Failure("Could not access player roster instance.");
            }

            roster.AddCharacter(character.CharacterTemplate);
            rosterInstance.AddRuntimePlacement(character.CharacterTemplate);
            rosterInstance.AddInstance(character); // fires OnRosterModified → SavePlayerRoster → LTM
            _brain.gamewideContextBrain.PersistCharacter(character, updateIndex: true);

            return OperationResult.Successful();
        }

        /// <summary>
        /// Triggers the post-recruit UI/audio sequence for a newly recruited character, then
        /// fires <see cref="Brain.OnHubCharacterRecruitCompleted"/> when the sequence is done.
        /// </summary>
        public void PlayRecruitCompleteSequence(CharacterInstance character)
        {
            var celebration = FindFirstObjectByType<RecruitmentCelebration>();
            if (celebration != null)
            {
                celebration.Activate(character);
            }
            else
            {
                _brain.PublishHubCharacterRecruitCompleted(character); // progress immediately if no celebration component
            }
        }
    }
}
