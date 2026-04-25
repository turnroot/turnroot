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

        private Brain.Brain _brain;

        /// <summary>True if any unlock condition has not yet been met.</summary>
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

                foreach (var condition in UnlockConditions)
                {
                    if (!condition.IsUnlocked(_brain, date))
                    {
                        return true;
                    }
                }

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
