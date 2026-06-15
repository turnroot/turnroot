using System.Linq;
using NaughtyAttributes;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.GameSettings
{
    public enum AvatarUnlockableType
    {
        HeadAccessory,
        FullOutfit,
        HairStyle,
    }

    public enum AvatarUnlockableCondition
    {
        ClearChapter,
        ClearChapterAndUnitSurvives,
        RecruitUnit,
    }

    public struct AvatarUnlockable
    {
        public string Name;
        public int ChapterUnlockAfter;
        public AvatarUnlockableType Type;
        public AvatarUnlockableCondition Condition;

        [ShowIf("ShowIfClearChapter")]
        public CharacterData UnitThatMustSurvive;

        [ShowIf("ShowIfRecruitUnit")]
        public CharacterData UnitThatMustBeRecruited;

        [ShowIf("ShowIfClearChapter")]
        public int ClearChapter;

        // showif bools
        public readonly bool ShowIfClearChapter =>
            Condition
                is AvatarUnlockableCondition.ClearChapter
                    or AvatarUnlockableCondition.ClearChapterAndUnitSurvives;
        public readonly bool ShowIfUnitSurvives =>
            Condition == AvatarUnlockableCondition.ClearChapterAndUnitSurvives;
        public readonly bool ShowIfRecruitUnit =>
            Condition == AvatarUnlockableCondition.RecruitUnit;

        public readonly bool IsHairStyle() => Type == AvatarUnlockableType.HairStyle;

        public readonly bool IsHeadAccessory() => Type == AvatarUnlockableType.HeadAccessory;

        public readonly bool IsFullOutfit() => Type == AvatarUnlockableType.FullOutfit;

        public readonly bool IsUnlocked(int currentChapter, bool unitSurvived, bool unitRecruited)
        {
            return Condition == AvatarUnlockableCondition.ClearChapter
                    ? currentChapter > ChapterUnlockAfter
                : Condition == AvatarUnlockableCondition.ClearChapterAndUnitSurvives
                    ? currentChapter > ChapterUnlockAfter && unitSurvived
                : Condition == AvatarUnlockableCondition.RecruitUnit && unitRecruited;
        }
    }

    public partial class GameplayGeneralSettings
        : SingletonScriptableObject<GameplayGeneralSettings>
    {
        [BoxGroup("Avatar Settings"), HorizontalLine(color: EColor.Indigo)]
        public Color[] AvatarHairColorChoices;

        [BoxGroup("Avatar Settings")]
        public Color[] AvatarEyeColorChoices;

        [BoxGroup("Avatar Settings")]
        public Color[] AvatarSkinColorChoices;

        [BoxGroup("Avatar Settings")]
        public AvatarUnlockable[] AvatarUnlockables;

        public AvatarUnlockable[] GetAnyAvailableAvatarUnlockables(
            int justFinishedChapter,
            Brain brain
        )
        {
            var roster = brain.gamewideContextBrain.CreateOrRecallGamewidePersistentPlayerRoster();

            var rosterInstance = brain.gamewideContextBrain.GetOrCreatePlayerTeamRoster(roster);
            var returns = new AvatarUnlockable[] { };

            foreach (var unlockable in AvatarUnlockables)
            {
                if (
                    unlockable.Condition == AvatarUnlockableCondition.ClearChapter
                    && unlockable.ChapterUnlockAfter == justFinishedChapter
                )
                {
                    returns = returns.Append(unlockable).ToArray();
                }

                if (unlockable.Condition == AvatarUnlockableCondition.RecruitUnit)
                {
                    var recruited =
                        rosterInstance?.GetInstanceFor(unlockable.UnitThatMustBeRecruited) != null;
                    if (recruited)
                    {
                        returns = returns.Append(unlockable).ToArray();
                    }
                }
                else if (
                    unlockable.Condition == AvatarUnlockableCondition.ClearChapterAndUnitSurvives
                )
                {
                    var unitInstance = rosterInstance?.GetInstanceFor(
                        unlockable.UnitThatMustSurvive
                    );
                    var survived =
                        unitInstance != null && unitInstance.IsDefeatedInCurrentBattle == false;
                    if (justFinishedChapter >= unlockable.ChapterUnlockAfter && survived)
                    {
                        returns = returns.Append(unlockable).ToArray();
                    }
                }
            }
            return returns;
        }
    }
}
