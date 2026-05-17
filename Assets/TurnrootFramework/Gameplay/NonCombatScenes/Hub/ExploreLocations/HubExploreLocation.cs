using NaughtyAttributes;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    /// <summary>
    /// A hub sublocation accessible through the Explore submenu rather than from the main hub menu
    /// directly. Supports unlock conditions (date, chapter, character support — all must pass).
    /// </summary>
    public class HubExploreLocation : HubSubLocation
    {
        [BoxGroup("Explore Location")]
        [Tooltip(
            "All conditions must be satisfied for this location to be accessible. "
                + "Leave empty to always be available."
        )]
        public HubExploreUnlockCondition[] UnlockConditions;

        public bool Indoors;

        private Brain.Brain _brain;
        public bool IsLocked
        {
            get
            {
                if (UnlockConditions == null || UnlockConditions.Length == 0)
                {
                    return false;
                }

                var hubManager = FindFirstObjectByType<HubManager>();
                GameDate date = hubManager != null ? hubManager.gameDate : GameDate.Default;

                int chapterNumber = _brain?.saveFileBrain?.ActiveSaveFile.ChapterNumber ?? -1;
                $"[HubDiag] IsLocked({LocationName}): _brain={((_brain == null) ? "NULL" : "set")} chapter={chapterNumber} date={date.year}/{date.month}/{date.day} conditions={UnlockConditions.Length}".LogInfo(
                    "HubExploreLocation.IsLocked"
                );

                foreach (var condition in UnlockConditions)
                {
                    if (!condition.IsUnlocked(_brain, date))
                    {
                        $"[HubDiag] IsLocked({LocationName}): LOCKED by condition type={condition.Type} UnlockAfterChapter={condition.UnlockAfterChapter} UnlockDate={condition.UnlockDate.year}/{condition.UnlockDate.month}/{condition.UnlockDate.day}".LogInfo(
                            "HubExploreLocation.IsLocked"
                        );
                        return true;
                    }
                }

                $"[HubDiag] IsLocked({LocationName}): UNLOCKED (all conditions passed)".LogInfo(
                    "HubExploreLocation.IsLocked"
                );
                return false;
            }
        }

        public override void Initialize(Brain.Brain brain)
        {
            _brain = brain;
            base.Initialize(brain);
        }

        public override bool CanBeVisitedToday() => !IsLocked;

        protected override HubManager.HubInputMode GetSublocationChoiceMode() =>
            HubManager.HubInputMode.ExploreMisc;
    }
}
