using System;
using System.Collections.Generic;
using NaughtyAttributes;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Gameplay.Objects;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;
using static Turnroot.Gameplay.Brain.GamewideContextBrainHelpers;

namespace Turnroot.Gameplay.Combat
{
    [CreateAssetMenu(
        fileName = "AllGameBattlesTable",
        menuName = "Turnroot/Gameplay/All Game Battles Table"
    )]
    public partial class AllGameBattlesTable : SingletonScriptableObject<AllGameBattlesTable>
    {
        [Serializable]
        public struct BattleEntry
        {
            [HorizontalLine(color: EColor.Gray)]
            [Tooltip("The battle scene. Drag the scene asset here — name matching is automatic.")]
            public SceneReference BattleScene;

            public string BattleName;
            public string BattleDescription;

            [Range(1, 3)]
            public int BattleDifficulty;

            public ObjectItem[] Rewards;
            public int GoldReward;

            [Range(0, 100)]
            public int ExtraExperienceReward;

            public bool RequiredStoryBattle;

            [ShowIf(nameof(RequiredStoryBattle))]
            [Tooltip(
                "How many hub days the player may spend faffing around before this Required Story Battle is forced "
                    + "(End Day is disabled once this limit is reached). "
                    + "Set to 0 for no limit."
            )]
            [Range(0, 30)]
            public int MaxHubDaysBeforeBattle;

            public bool Repeateable;
            public bool ParalogueBattle;

            [ShowIf(nameof(ParalogueBattle))]
            public CharacterData ParalogueCharacter;

            [HorizontalLine(color: EColor.White)]
            [InfoBox("Fill these fields to use the quadrant-based Map Exploration display.")]
            public ExploreStatusSprites MapExplorationSprites;

            [Tooltip("Initial exploration state for the top-left quadrant.")]
            public QuadrantExploredState InitialTopLeft;

            [Tooltip("Initial exploration state for the top-right quadrant.")]
            public QuadrantExploredState InitialTopRight;

            [Tooltip("Initial exploration state for the bottom-left quadrant.")]
            public QuadrantExploredState InitialBottomLeft;

            [Tooltip("Initial exploration state for the bottom-right quadrant.")]
            public QuadrantExploredState InitialBottomRight;

            [HorizontalLine(color: EColor.White)]
            [InfoBox("If not using Map Exploration, put the flat map image here instead.")]
            public Sprite MapSprite;
        }

        // ── Inspector fields ─────────────────────────────────────────────────

        [InfoBox(
            "One entry per battle in the game. Drag the scene asset into BattleScene — "
                + "name matching is automatic. The Scene Flow Editor controls which entries are available to the player."
        )]
        [ReorderableList]
        public List<BattleEntry> Battles = new();

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns the <see cref="ExploredStatus"/> for <paramref name="battleSceneName"/>.
        /// Reads from LTM if a saved value exists; otherwise seeds LTM from this entry's
        /// initial values and returns those. If <paramref name="ltm"/> is null, returns
        /// the initial values without persisting anything.
        /// </summary>
        public ExploredStatus Initialize(string battleSceneName, LongTermMemory ltm)
        {
            if (string.IsNullOrEmpty(battleSceneName))
            {
                return default;
            }

            if (ltm != null)
            {
                var saved = ltm.Recall(LtmKeys.MapExplorationKey(battleSceneName));
                if (!string.IsNullOrEmpty(saved) && TryDecodeStatus(saved, out var loaded))
                {
                    return loaded;
                }
            }

            var initial = GetInitialStatus(battleSceneName);

            if (ltm != null)
            {
                SaveStatusToLtm(battleSceneName, initial, ltm);
            }

            return initial;
        }

        /// <summary>
        /// Returns the initial <see cref="ExploredStatus"/> defined in this table for
        /// <paramref name="battleSceneName"/>, without reading or writing LTM.
        /// Returns an all-<see cref="QuadrantExploredState.NotExplored"/> status if no
        /// entry exists for the given name.
        /// </summary>
        public ExploredStatus GetInitialStatus(string battleSceneName)
        {
            return TryGetBattle(battleSceneName, out var entry)
                ? new ExploredStatus
                {
                    TopLeft = entry.InitialTopLeft,
                    TopRight = entry.InitialTopRight,
                    BottomLeft = entry.InitialBottomLeft,
                    BottomRight = entry.InitialBottomRight,
                }
                : default;
        }

        /// <summary>
        /// Tries to find the entry whose <see cref="BattleEntry.BattleScene"/> name matches
        /// <paramref name="battleSceneName"/> (case-sensitive).
        /// </summary>
        public bool TryGetBattle(string battleSceneName, out BattleEntry entry)
        {
            if (Battles != null)
            {
                foreach (var e in Battles)
                {
                    if (e.BattleScene != null && e.BattleScene.SceneName == battleSceneName)
                    {
                        entry = e;
                        return true;
                    }
                }
            }

            entry = default;
            return false;
        }

        // ── Codec ─────────────────────────────────────────────────────────────
        // Statuses are stored as "TL,TR,BL,BR" where each token is the integer
        // value of the corresponding QuadrantExploredState enum member.

        private static string EncodeStatus(ExploredStatus s) =>
            $"{(int)s.TopLeft},{(int)s.TopRight},{(int)s.BottomLeft},{(int)s.BottomRight}";

        private static bool TryDecodeStatus(string raw, out ExploredStatus status)
        {
            status = default;

            if (string.IsNullOrEmpty(raw))
            {
                return false;
            }

            var parts = raw.Split(',');
            if (parts.Length != 4)
            {
                return false;
            }

            if (
                !int.TryParse(parts[0], out var tl)
                || !int.TryParse(parts[1], out var tr)
                || !int.TryParse(parts[2], out var bl)
                || !int.TryParse(parts[3], out var br)
            )
            {
                return false;
            }

            status.TopLeft = (QuadrantExploredState)tl;
            status.TopRight = (QuadrantExploredState)tr;
            status.BottomLeft = (QuadrantExploredState)bl;
            status.BottomRight = (QuadrantExploredState)br;
            return true;
        }
    }
}
