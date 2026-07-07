using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts.UI;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public enum RecruitmentAttemptOutcome
    {
        Failure,
        NearlySucceeded,
        Success,
    }

    [RequireComponent(typeof(LongTermMemory))]
    public partial class CharactersBrain : BrainComponent
    {
        private struct NullSafeAvatar
        {
            public CharacterInstance Avatar { get; }
            public bool IsValid => Avatar != null;

            public NullSafeAvatar(CharacterInstance avatar)
            {
                Avatar = avatar;
            }
        }

        private NullSafeAvatar avatar;

        private bool AvatarHasMinExperienceLevels(CharacterData characterData)
        {
            if (!characterData.AvatarMustHaveMinimumExperienceLevelsToRecruit)
            {
                return true;
            }

            // check avatar experience ranks against characterData.AvatarMinimumExperienceRanksToRecruit
            foreach (var required in characterData.AvatarMinimumExperienceRanksToRecruit)
            {
                var avatarRank = avatar.Avatar.ExperienceRanks.Find(r =>
                    r.ExperienceTypeId == required.ExperienceTypeId
                );
                if (avatarRank == null || required.Rank.CompareTo(avatarRank.Rank.Value) > 0)
                {
                    return false;
                }
            }
            return true;
        }

        private bool AvatarHasMinExperienceLevels(CharacterData characterData, out bool isNearMiss)
        {
            isNearMiss = false;

            if (!characterData.AvatarMustHaveMinimumExperienceLevelsToRecruit)
            {
                return true;
            }

            var status = true;
            foreach (var required in characterData.AvatarMinimumExperienceRanksToRecruit)
            {
                var avatarRank = avatar.Avatar.ExperienceRanks.Find(r =>
                    r.ExperienceTypeId == required.ExperienceTypeId
                );
                if (avatarRank == null)
                {
                    status = false;
                    continue;
                }

                var difference = required.Rank.CompareTo(avatarRank.Rank.Value);
                if (difference > 0)
                {
                    status = false;
                    if (difference == 1)
                    {
                        isNearMiss = true;
                    }
                }
            }

            return status;
        }

        private bool AvatarMeetsRecruitSupportRequirement(
            CharacterData characterData,
            out bool isNearMiss
        )
        {
            isNearMiss = false;

            if (!characterData.RecruitRequiresMinSupportLevel)
            {
                return true;
            }

            var supportRel = avatar.Avatar.GetSupportRelationship(characterData);
            if (supportRel == null)
            {
                return false;
            }

            var difference = characterData.RecruitSupportRelationshipMinRank.CompareTo(
                supportRel.CurrentLevel
            );
            if (difference > 0)
            {
                isNearMiss = difference == 1;
                return false;
            }

            return true;
        }

        private bool RequiredAllyIsRecruited(CharacterData characterData)
        {
            if (!characterData.WillJoinIfAllyIsAlreadyRecruited)
            {
                return false;
            }

            var requiredAlly = characterData.SpecificAllyRequiredForRecruitment;
            if (requiredAlly == null)
            {
                return false;
            }

            var roster = _brain.gamewideContextBrain.CreateOrRecallGamewidePersistentPlayerRoster();
            if (roster == null)
            {
                return false;
            }

            var rosterInstance = _gamewideContextBrain?.GetOrCreatePlayerTeamRoster(roster);
            return rosterInstance?.GetInstanceFor(requiredAlly) != null;
        }

        private int GetCurrentPlayerRosterAverageLevel()
        {
            var rosterInstance = _gamewideContextBrain?.GetPersistentPlayerTeamRosterInstance();
            var levels = rosterInstance
                ?.Instances?.Where(i => i != null)
                .Select(i => i.CurrentLevel)
                .ToArray();
            return levels == null || levels.Length == 0
                ? 1
                : Mathf.Max(1, Mathf.FloorToInt((float)levels.Average()));
        }

        public RecruitmentAttemptOutcome GetRecruitmentAttemptOutcome(CharacterInstance character)
        {
            avatar = new NullSafeAvatar(_gamewideContextBrain?.GetOrCreateAvatarInstance());
            if (!avatar.IsValid || character?.CharacterTemplate == null)
            {
                return RecruitmentAttemptOutcome.Failure;
            }

            var characterData = character.CharacterTemplate;

            if (RequiredAllyIsRecruited(characterData))
            {
                return RecruitmentAttemptOutcome.Success;
            }

            var experienceNearMiss = false;
            var supportNearMiss = false;

            var experienceMet = AvatarHasMinExperienceLevels(characterData, out experienceNearMiss);
            if (!experienceMet && characterData.SupportCanCompensateForMissingExperienceLevels)
            {
                var supportRel = avatar.Avatar.GetSupportRelationship(characterData);
                if (
                    supportRel != null
                    && characterData.RecruitCompensationSupportLevel.CompareTo(
                        supportRel.CurrentLevel
                    ) <= 0
                )
                {
                    experienceMet = true;
                    experienceNearMiss = false;
                }
            }

            var supportMet = AvatarMeetsRecruitSupportRequirement(
                characterData,
                out supportNearMiss
            );

            return experienceMet && supportMet ? RecruitmentAttemptOutcome.Success
                : experienceNearMiss || supportNearMiss ? RecruitmentAttemptOutcome.NearlySucceeded
                : RecruitmentAttemptOutcome.Failure;
        }

        public bool CanRecruit(CharacterInstance character) =>
            GetRecruitmentAttemptOutcome(character) == RecruitmentAttemptOutcome.Success;

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

            var targetLevel = Mathf.Max(
                character.CurrentLevel,
                GetCurrentPlayerRosterAverageLevel()
            );
            while (character.CurrentLevel < targetLevel)
            {
                var beforeLevel = character.CurrentLevel;
                LevelUpCharacter(character);
                if (character.CurrentLevel <= beforeLevel)
                {
                    return OperationResult.Failure("Could not normalize recruited unit level.");
                }
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
