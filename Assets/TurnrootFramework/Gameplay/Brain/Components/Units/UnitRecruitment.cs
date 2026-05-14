using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts.UI;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    private struct NullSafeAvatar()
    {
        public CharacterInstance? Avatar { get; } =
            _gamewideContextBrain?.GetOrCreateAvatarInstance();
        public bool IsValid => Avatar != null;
    }

    [RequireComponent(typeof(LongTermMemory))]
    public partial class CharactersBrain : BrainComponent
    {
        private NullSafeAvatar avatar;

        private bool AvatarHasMinExperienceLevels(CharacterData characterData)
        {
            if (characterData.AvatarMustHaveMinimumExperienceLevelsToRecruit)
            {
                var status = true;
                // check avatar experience ranks against characterData.AvatarMinimumExperienceRanksToRecruit

                foreach (var required in characterData.AvatarMinimumExperienceRanksToRecruit)
                {
                    var avatarRank = avatar.Avatar.ExperienceRanks.Find(r =>
                        r.ExperienceTypeId == required.ExperienceTypeId
                    );
                    if (avatarRank == null || required.Rank.CompareTo(avatarRank.Rank.Value) > 0)
                    {
                        status = false;
                        break;
                    }
                }
            }
            return status;
        }

        private bool CheckMinExperienceOverride(CharacterData characterData)
        {
            var status = AvatarHasMinExperienceLevels(characterData);
            if (status)
            {
                return true;
            }
            else
            {
                if (characterData.SupportCanCompensateForMissingExperienceLevels)
                { // failed the experience check but support relationship can compensate
                    var supportRel = avatar.GetSupportRelationship(characterData);
                    if (
                        supportRel == null
                        || characterData.RecruitCompensationSupportLevel.CompareTo(
                            supportRel.CurrentLevel
                        ) > 0
                    )
                    {
                        status = false;
                    }
                }
                else
                {
                    // failed and can't compensate
                    status = false;
                }
            }

            return status;
        }

        public bool CanRecruit(CharacterInstance character)
        {
            avatar = new NullSafeAvatar();
            if (!avatar.IsValid)
                return false;

            var status = false;

            var characterData = character.CharacterTemplate;

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
                    ; // If true the character is recruitable regardless of other conditions
                }
            }

            CheckMinExperienceOverride(characterData); // check experience next since it can be overridden by support relationship

            if (characterData.RecruitRequiresMinSupportLevel)
            {
                var supportRel = avatar.Avatar.GetSupportRelationship(characterData);
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
