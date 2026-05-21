using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Gameplay.Objects;
using static Turnroot.Gameplay.Brain.GamewideContextBrainHelpers;

namespace Turnroot.Gameplay.Combat
{
    public partial class AllGameBattlesTable
    {
        // ── Exploration setters (LTM-persisted) ──────────────────────────────

        /// <summary>
        /// Updates a single quadrant's exploration state, saves it back to LTM, and returns
        /// the full updated <see cref="ExploredStatus"/>.
        /// Call this whenever a quadrant is explored at runtime.
        /// </summary>
        public ExploredStatus SetQuadrantState(
            string battleSceneName,
            MapQuadrant quadrant,
            QuadrantExploredState newState,
            LongTermMemory ltm
        )
        {
            var status = Initialize(battleSceneName, ltm);

            switch (quadrant)
            {
                case MapQuadrant.TopLeft:
                    status.TopLeft = newState;
                    break;
                case MapQuadrant.TopRight:
                    status.TopRight = newState;
                    break;
                case MapQuadrant.BottomLeft:
                    status.BottomLeft = newState;
                    break;
                case MapQuadrant.BottomRight:
                    status.BottomRight = newState;
                    break;
            }

            SaveStatusToLtm(battleSceneName, status, ltm);
            return status;
        }

        /// <summary>
        /// Writes an updated <see cref="ExploredStatus"/> for <paramref name="battleSceneName"/>
        /// back to LTM so exploration progress is preserved across sessions.
        /// </summary>
        public void SaveStatusToLtm(
            string battleSceneName,
            ExploredStatus status,
            LongTermMemory ltm
        )
        {
            if (string.IsNullOrEmpty(battleSceneName) || ltm == null)
            {
                return;
            }

            ltm.Remember(LtmKeys.MapExplorationKey(battleSceneName), EncodeStatus(status));
        }

        // ── Entry setters (in-memory, no LTM) ────────────────────────────────
        // These modify the in-memory copy of a BattleEntry for the current session.
        // Because BattleEntry is a struct stored in a List, all mutations require
        // a find-copy-modify-replace cycle — these helpers encapsulate that pattern.

        /// <summary>
        /// Replaces the entire <see cref="BattleEntry"/> for the given scene name in-memory.
        /// Use this when multiple fields need updating at once. Returns false if no matching
        /// entry is found.
        /// </summary>
        public bool UpdateEntry(string battleSceneName, BattleEntry updatedEntry)
        {
            if (Battles == null)
            {
                return false;
            }

            for (int i = 0; i < Battles.Count; i++)
            {
                if (
                    Battles[i].BattleScene != null
                    && Battles[i].BattleScene.SceneName == battleSceneName
                )
                {
                    Battles[i] = updatedEntry;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Updates the <see cref="BattleEntry.Repeateable"/> flag for the given scene in-memory.
        /// Useful when a battle's repeatability changes based on story progress.
        /// Returns false if no matching entry is found.
        /// </summary>
        public bool SetRepeateable(string battleSceneName, bool repeatable)
        {
            if (Battles == null)
            {
                return false;
            }

            for (int i = 0; i < Battles.Count; i++)
            {
                if (
                    Battles[i].BattleScene != null
                    && Battles[i].BattleScene.SceneName == battleSceneName
                )
                {
                    var entry = Battles[i];
                    entry.Repeateable = repeatable;
                    Battles[i] = entry;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Updates the <see cref="BattleEntry.RequiredStoryBattle"/> flag for the given scene
        /// in-memory. Useful when a battle's story-required status changes at runtime.
        /// Returns false if no matching entry is found.
        /// </summary>
        public bool SetRequiredStoryBattle(string battleSceneName, bool required)
        {
            if (Battles == null)
            {
                return false;
            }

            for (int i = 0; i < Battles.Count; i++)
            {
                if (
                    Battles[i].BattleScene != null
                    && Battles[i].BattleScene.SceneName == battleSceneName
                )
                {
                    var entry = Battles[i];
                    entry.RequiredStoryBattle = required;
                    Battles[i] = entry;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Updates the reward data for the given scene in-memory.
        /// Returns false if no matching entry is found.
        /// </summary>
        public bool SetRewards(
            string battleSceneName,
            ObjectItem[] rewards,
            int goldReward,
            int extraExperienceReward
        )
        {
            if (Battles == null)
            {
                return false;
            }

            for (int i = 0; i < Battles.Count; i++)
            {
                if (
                    Battles[i].BattleScene != null
                    && Battles[i].BattleScene.SceneName == battleSceneName
                )
                {
                    var entry = Battles[i];
                    entry.Rewards = rewards;
                    entry.GoldReward = goldReward;
                    entry.ExtraExperienceReward = extraExperienceReward;
                    Battles[i] = entry;
                    return true;
                }
            }

            return false;
        }
    }
}
